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

        public SignatureHelperProvider(object inner)
        {
            _inner = inner;
        }

        public bool IsTriggerCharacter(char ch)
        {
            return (bool)InterfaceType.GetMethod(nameof(IsTriggerCharacter)).Invoke(_inner, new object[] { ch });
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return (bool)InterfaceType.GetMethod(nameof(IsRetriggerCharacter)).Invoke(_inner, new object[] { ch });
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            var methodInfo = InterfaceType.GetMethod(nameof(GetItemsAsync));
            var task = methodInfo.Invoke(_inner, new[] { document, position, triggerInfo.Inner, cancellationToken });

            var result = await ((Task<object>)typeof(Utilities.TaskExtensions).GetMethod(nameof(Utilities.TaskExtensions.Cast), BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()[0], typeof(object))
                .Invoke(null, new[] { task })).ConfigureAwait(false);

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

        public SignatureHelpItem(object inner)
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

        public SignatureHelpItems(object inner)
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

        public SignatureHelpParameter(object inner)
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
        public SignatureHelpTriggerReason TriggerReason { get; }

        public char? TriggerCharacter { get; }

        public object Inner { get; set; }

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

        internal SignatureHelpTriggerInfo(SignatureHelpTriggerReason triggerReason, char? triggerCharacter = null)
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