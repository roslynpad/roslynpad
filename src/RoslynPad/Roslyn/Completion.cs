using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn
{
    internal static class CompletionService
    {
        private static readonly Type _innerType = Type.GetType("Microsoft.CodeAnalysis.Completion.CompletionService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        private static readonly Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>>
            _getCompletionListAsync = CreateGetCompletionListAsyncFunc();

        private static Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>> CreateGetCompletionListAsyncFunc()
        {
            var innerMethod = _innerType.GetMethod("GetCompletionListAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var innerMethodsParam = innerMethod.GetParameters();
            var param = new[]
            {
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(int)),
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(OptionSet)),
                Expression.Parameter(typeof(IEnumerable<object>)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<Document, int, object, OptionSet, IEnumerable<object>, CancellationToken, Task<object>>>(
                Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast))
                    .MakeGenericMethod(innerMethod.ReturnType.GetGenericArguments()[0], typeof(object)),
                    Expression.Call(innerMethod,
                        param[0], param[1], Expression.Convert(param[2], innerMethodsParam[2].ParameterType),
                        param[3], Expression.Convert(param[4], innerMethodsParam[4].ParameterType), param[5])), param).Compile();
        }

        private static readonly Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>>
           _isCompletionTriggerCharacterAsync = CreateIsCompletionTriggerCharacterAsync();

        private static Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>> CreateIsCompletionTriggerCharacterAsync()
        {
            var innerMethod = _innerType.GetMethod("IsCompletionTriggerCharacterAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var innerMethodsParam = innerMethod.GetParameters();
            var param = new[]
            {
                Expression.Parameter(typeof(Document)),
                Expression.Parameter(typeof(int)),
                Expression.Parameter(typeof(IEnumerable<object>)),
                Expression.Parameter(typeof(CancellationToken))
            };
            return Expression.Lambda<Func<Document, int, IEnumerable<object>, CancellationToken, Task<bool>>>(
                Expression.Call(innerMethod,
                        param[0], param[1], Expression.Convert(param[2], innerMethodsParam[2].ParameterType),
                        param[3]),
                param).Compile();
        }


        private static readonly Func<Document, object> _getCompletionRules = CreateGetCompletionRules();

        private static Func<Document, object> CreateGetCompletionRules()
        {
            var innerMethod = _innerType.GetMethod("GetCompletionRules", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var param = new[]
            {
                Expression.Parameter(typeof(Document))
            };
            return Expression.Lambda<Func<Document, object>>(
                Expression.Convert(Expression.Call(innerMethod, param[0]), typeof(object)),
                param).Compile();
        }

        public static CompletionRules GetCompletionRules(Document document)
        {
            var rules = _getCompletionRules(document);
            return new CompletionRules(rules);
        }

        public static async Task<CompletionList> GetCompletionListAsync(Document document, int position,
            CompletionTriggerInfo triggerInfo, OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var list = await _getCompletionListAsync(document, position, triggerInfo.Inner, options, null,
                cancellationToken).ConfigureAwait(false);
            return list == null ? null : new CompletionList(list);
        }

        public static Task<bool> IsCompletionTriggerCharacterAsync(Document document, int characterPosition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return _isCompletionTriggerCharacterAsync(document, characterPosition, null, cancellationToken);
        }
    }

    public enum CompletionTriggerReason
    {
        InvokeCompletionCommand,
        TypeCharCommand,
        BackspaceOrDeleteCommand,
        Snippets
    }

    public struct CompletionTriggerInfo
    {
        public CompletionTriggerReason TriggerReason { get; }

        public char? TriggerCharacter { get; }

        public object Inner { get; set; }

        private static readonly Func<int, char?, object> _completionTriggerInfoCtor =
            CreateCompletionTriggerInfoFunc();

        private static Func<int, char?, object> CreateCompletionTriggerInfoFunc()
        {
            var type = Type.GetType("Microsoft.CodeAnalysis.Completion.CompletionTriggerInfo, Microsoft.CodeAnalysis.Features", throwOnError: true);
            var param = new[]
            {
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (char?)),
            };
            var constructor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            return Expression.Lambda<Func<int, char?, object>>(
                Expression.Convert(Expression.New(constructor, Expression.Convert(param[0], constructor.GetParameters()[0].ParameterType), param[1]), typeof(object)),
                param).Compile();
        }

        private CompletionTriggerInfo(CompletionTriggerReason triggerReason, char? triggerCharacter)
        {
            TriggerReason = triggerReason;
            TriggerCharacter = triggerCharacter;
            Inner = _completionTriggerInfoCtor((int)triggerReason, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateTypeCharTriggerInfo(char triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.TypeCharCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateInvokeCompletionTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.InvokeCompletionCommand, null);
        }

        public static CompletionTriggerInfo CreateBackspaceTriggerInfo(char? triggerCharacter)
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.BackspaceOrDeleteCommand, triggerCharacter);
        }

        public static CompletionTriggerInfo CreateSnippetTriggerInfo()
        {
            return new CompletionTriggerInfo(CompletionTriggerReason.Snippets, null);
        }
    }

    public class CompletionList
    {
        public bool IsExclusive { get; }

        public ImmutableArray<CompletionItem> Items { get; }

        public CompletionList(object inner)
        {
            IsExclusive = inner.GetPropertyValue<bool>(nameof(IsExclusive));
            Items = ImmutableArray.CreateRange(inner.GetPropertyValue<IEnumerable<object>>(nameof(Items))
                .Select(x => new CompletionItem(x)));
        }
    }

    public class CompletionRules
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly object _inner;

        public CompletionRules(object inner)
        {
            _inner = inner;
        }
    }

    public class CompletionItemRules
    {
        private readonly object _inner;

        public CompletionItemRules(object inner)
        {
            _inner = inner;
        }

        public virtual TextChange? GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return (TextChange?)_inner.GetType().GetMethod(nameof(GetTextChange)).Invoke(_inner, new[] { selectedItem.Inner, ch, textTypedSoFar });
        }
    }

    [DebuggerDisplay("{DisplayText}")]
    public class CompletionItem : IComparable<CompletionItem>
    {
        public object Inner { get; set; }

        public Glyph? Glyph { get; }

        public string DisplayText { get; }

        public string FilterText { get; }

        public string SortText { get; }

        public bool Preselect { get; }

        public TextSpan FilterSpan { get; }

        public bool IsBuilder { get; }

        public CompletionItemRules Rules { get; }

        public bool ShowsWarningIcon { get; }

        public bool ShouldFormatOnCommit { get; }

        public CompletionItem(object inner)
        {
            Inner = inner;
            Glyph = (Glyph)inner.GetPropertyValue<int>(nameof(Glyph));
            DisplayText = inner.GetPropertyValue<string>(nameof(DisplayText));
            FilterText = inner.GetPropertyValue<string>(nameof(FilterText));
            SortText = inner.GetPropertyValue<string>(nameof(SortText));
            Preselect = inner.GetPropertyValue<bool>(nameof(Preselect));
            Rules = new CompletionItemRules(inner.GetPropertyValue<object>(nameof(Rules)));
            FilterSpan = inner.GetPropertyValue<TextSpan>(nameof(FilterSpan));
            IsBuilder = inner.GetPropertyValue<bool>(nameof(IsBuilder));
            ShowsWarningIcon = inner.GetPropertyValue<bool>(nameof(ShowsWarningIcon));
            ShouldFormatOnCommit = inner.GetPropertyValue<bool>(nameof(ShouldFormatOnCommit));
        }

        public virtual Task<ImmutableArray<SymbolDisplayPart>> GetDescriptionAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return (Task<ImmutableArray<SymbolDisplayPart>>)Inner.GetType().GetMethod(nameof(GetDescriptionAsync))
                .Invoke(Inner, new object[] { cancellationToken });
        }

        public int CompareTo(CompletionItem other)
        {
            var result = StringComparer.OrdinalIgnoreCase.Compare(SortText, other.SortText);
            if (result == 0)
            {
                result = StringComparer.OrdinalIgnoreCase.Compare(DisplayText, other.DisplayText);
            }
            return result;
        }
    }

    public enum Glyph
    {
        Assembly,
        BasicFile,
        BasicProject,
        ClassPublic,
        ClassProtected,
        ClassPrivate,
        ClassInternal,
        CSharpFile,
        CSharpProject,
        ConstantPublic,
        ConstantProtected,
        ConstantPrivate,
        ConstantInternal,
        DelegatePublic,
        DelegateProtected,
        DelegatePrivate,
        DelegateInternal,
        EnumPublic,
        EnumProtected,
        EnumPrivate,
        EnumInternal,
        EnumMember,
        Error,
        EventPublic,
        EventProtected,
        EventPrivate,
        EventInternal,
        ExtensionMethodPublic,
        ExtensionMethodProtected,
        ExtensionMethodPrivate,
        ExtensionMethodInternal,
        FieldPublic,
        FieldProtected,
        FieldPrivate,
        FieldInternal,
        InterfacePublic,
        InterfaceProtected,
        InterfacePrivate,
        InterfaceInternal,
        Intrinsic,
        Keyword,
        Label,
        Local,
        Namespace,
        MethodPublic,
        MethodProtected,
        MethodPrivate,
        MethodInternal,
        ModulePublic,
        ModuleProtected,
        ModulePrivate,
        ModuleInternal,
        OpenFolder,
        Operator,
        Parameter,
        PropertyPublic,
        PropertyProtected,
        PropertyPrivate,
        PropertyInternal,
        RangeVariable,
        Reference,
        StructurePublic,
        StructureProtected,
        StructurePrivate,
        StructureInternal,
        TypeParameter,
        Snippet,
        CompletionWarning
    }
}