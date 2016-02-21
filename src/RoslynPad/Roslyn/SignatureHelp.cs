using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn
{
    internal class SignatureHelperProvider : ISignatureHelpProvider
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Editor.ISignatureHelpProvider, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);

        private readonly object _inner;

        internal SignatureHelperProvider(object inner)
        {
            _inner = inner;
        }

        private static readonly Func<object, char, bool> _isTriggerCharacter = CreateIsTriggerCharacter();

        private static Func<object, char, bool> CreateIsTriggerCharacter()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (char)),
            };
            return Expression.Lambda<Func<object, char, bool>>(
                Expression.Call(Expression.Convert(param[0], InterfaceType), InterfaceType.GetMethod(nameof(IsTriggerCharacter)), param[1]),
                param).Compile();
        }

        private static readonly Func<object, char, bool> _isRetriggerCharacter = CreateIsRetriggerCharacter();

        private static Func<object, char, bool> CreateIsRetriggerCharacter()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (char)),
            };
            return Expression.Lambda<Func<object, char, bool>>(
                Expression.Call(Expression.Convert(param[0], InterfaceType), InterfaceType.GetMethod(nameof(IsRetriggerCharacter)), param[1]),
                param).Compile();
        }

        private static readonly Func<object, Document, int, object, CancellationToken, Task<object>> _getItemsAsync = CreateGetItemsAsync();
        
        private static Func<object, Document, int, object, CancellationToken, Task<object>> CreateGetItemsAsync()
        {
            var param = new[]
            {
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (Document)),
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (object)),
                Expression.Parameter(typeof (CancellationToken)),
            };
            var methodInfo = InterfaceType.GetMethod(nameof(GetItemsAsync));
            return Expression.Lambda<Func<object, Document, int, object, CancellationToken, Task<object>>>(
                Expression.Call(typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast), BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0], typeof(object)),
                    Expression.Call(Expression.Convert(param[0], InterfaceType), methodInfo, param[1], param[2], 
                        Expression.Convert(param[3], methodInfo.GetParameters()[2].ParameterType), param[4])),
                param).Compile();
        }

        public bool IsTriggerCharacter(char ch)
        {
            return _isTriggerCharacter(_inner, ch);
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return _isRetriggerCharacter(_inner, ch);
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            var result = await _getItemsAsync(_inner, document, position, triggerInfo.Inner, cancellationToken).ConfigureAwait(false);

            return result == null ? null : new SignatureHelpItems(result);
        }
    }

    public interface ISignatureHelpProvider
    {
        bool IsTriggerCharacter(char ch);

        bool IsRetriggerCharacter(char ch);

        Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);
    }

    public class SignatureHelpItem
    {
        public bool IsVariadic { get; }

        public ImmutableArray<SymbolDisplayPart> PrefixDisplayParts { get; }

        public ImmutableArray<SymbolDisplayPart> SuffixDisplayParts { get; }

        public ImmutableArray<SymbolDisplayPart> SeparatorDisplayParts { get; }

        public ImmutableArray<SignatureHelpParameter> Parameters { get; }

        public ImmutableArray<SymbolDisplayPart> DescriptionParts { get; }

        public Func<CancellationToken, IEnumerable<SymbolDisplayPart>> DocumentationFactory { get; }

        internal SignatureHelpItem(object inner)
        {
            IsVariadic = inner.GetPropertyValue<bool>(nameof(IsVariadic));
            PrefixDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(PrefixDisplayParts));
            SuffixDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(SuffixDisplayParts));
            SeparatorDisplayParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(SeparatorDisplayParts));
            Parameters = ImmutableArray.CreateRange(inner.GetPropertyValue<IEnumerable<object>>(nameof(Parameters)).Select(source => new SignatureHelpParameter(source)));
            DescriptionParts = inner.GetPropertyValue<ImmutableArray<SymbolDisplayPart>>(nameof(DescriptionParts));
            IsVariadic = inner.GetPropertyValue<bool>(nameof(IsVariadic));
            DocumentationFactory = inner.GetPropertyValue<Func<CancellationToken, IEnumerable<SymbolDisplayPart>>>(nameof(DocumentationFactory));
        }
    }

    public class SignatureHelpItems
    {
        public IList<SignatureHelpItem> Items { get; }

        public TextSpan ApplicableSpan { get; }

        public int ArgumentIndex { get; }

        public int ArgumentCount { get; }

        public string ArgumentName { get; }

        public int? SelectedItemIndex { get; }

        internal SignatureHelpItems(object inner)
        {
            Items = inner.GetPropertyValue<IEnumerable<object>>(nameof(Items)).Select(x => new SignatureHelpItem(x)).ToArray();
            ApplicableSpan = inner.GetPropertyValue<TextSpan>(nameof(ApplicableSpan));
            ArgumentIndex = inner.GetPropertyValue<int>(nameof(ArgumentIndex));
            ArgumentCount = inner.GetPropertyValue<int>(nameof(ArgumentCount));
            ArgumentName = inner.GetPropertyValue<string>(nameof(ArgumentName));
            SelectedItemIndex = inner.GetPropertyValue<int?>(nameof(SelectedItemIndex));
        }
    }

    public class SignatureHelpParameter
    {
        public string Name { get; }

        public Func<CancellationToken, IEnumerable<SymbolDisplayPart>> DocumentationFactory { get; }

        public IList<SymbolDisplayPart> PrefixDisplayParts { get; }

        public IList<SymbolDisplayPart> SuffixDisplayParts { get; }

        public IList<SymbolDisplayPart> DisplayParts { get; }

        public bool IsOptional { get; }

        public IList<SymbolDisplayPart> SelectedDisplayParts { get; }

        internal SignatureHelpParameter(object inner)
        {
            Name = inner.GetPropertyValue<string>(nameof(Name));
            DocumentationFactory = inner.GetPropertyValue<Func<CancellationToken, IEnumerable<SymbolDisplayPart>>>(nameof(DocumentationFactory));
            PrefixDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(PrefixDisplayParts));
            SuffixDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(SuffixDisplayParts));
            DisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(DisplayParts));
            IsOptional = inner.GetPropertyValue<bool>(nameof(IsOptional));
            SelectedDisplayParts = inner.GetPropertyValue<IList<SymbolDisplayPart>>(nameof(SelectedDisplayParts));
        }
    }

    public struct SignatureHelpTriggerInfo
    {
        internal object Inner { get; set; }

        public SignatureHelpTriggerReason TriggerReason { get; }

        public char? TriggerCharacter { get; }

        private static readonly Func<int, char?, object> _signatureHelpTriggerInfoCtor =
            CreateSignatureHelpTriggerInfoFunc();

        private static Func<int, char?, object> CreateSignatureHelpTriggerInfoFunc()
        {
            var type = Type.GetType("Microsoft.CodeAnalysis.Editor.SignatureHelpTriggerInfo, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);
            var param = new[]
            {
                Expression.Parameter(typeof (int)),
                Expression.Parameter(typeof (char?)),
            };
            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First();
            return Expression.Lambda<Func<int, char?, object>>(
                Expression.Convert(Expression.New(constructor, Expression.Convert(param[0], constructor.GetParameters()[0].ParameterType), param[1]), typeof(object)),
                param).Compile();
        }

        public SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
        {
            TriggerReason = triggerReason;
            TriggerCharacter = triggerCharacter;
            Inner = _signatureHelpTriggerInfoCtor((int)triggerReason, triggerCharacter);
        }
    }

    public enum SignatureHelpTriggerReason
    {
        InvokeSignatureHelpCommand,
        TypeCharCommand,
        RetriggerCommand
    }
}