using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;
using RoslynPad.Roslyn.SignatureHelp;

namespace RoslynPad.Gtk
{
    internal class RoslynCompletionTextEditorExtension : CompletionTextEditorExtension
    {
        private readonly RoslynHost _host;
        private readonly DocumentId _documentId;

        public RoslynCompletionTextEditorExtension(RoslynHost host, DocumentId documentId)
        {
            _host = host;
            _documentId = documentId;
        }

        public override string CompletionLanguage => "C#";

        public override bool KeyPress(KeyDescriptor descriptor)
        {
            var b = base.KeyPress(descriptor);
            if ((descriptor.KeyChar == ',' || descriptor.KeyChar == ')') && CanRunParameterCompletionCommand())
            {
                RunParameterCompletionCommand();
            }
            return b;
        }

        public override async Task<ICompletionDataList> HandleBackspaceOrDeleteCodeCompletionAsync(CodeCompletionContext completionContext, SpecialKey key, char triggerCharacter, CancellationToken token = default(CancellationToken))
        {
            if (!char.IsLetterOrDigit(triggerCharacter) && triggerCharacter != '_')
                return null;

            if (key == SpecialKey.BackSpace || key == SpecialKey.Delete)
            {
                var ch = completionContext.TriggerOffset > 0
                    ? Editor.GetCharAt(completionContext.TriggerOffset - 1)
                    : '\0';
                var ch2 = completionContext.TriggerOffset < Editor.Length
                    ? Editor.GetCharAt(completionContext.TriggerOffset)
                    : '\0';
                if (!IsIdentifierPart(ch) && !IsIdentifierPart(ch2))
                    return null;
            }
            var result = await HandleCodeCompletion(completionContext, CompletionTrigger.CreateDeletionTrigger(triggerCharacter), 0, token).ConfigureAwait(false);
            if (result == null)
                return null;
            result.AutoCompleteUniqueMatch = false;
            result.AutoCompleteEmptyMatch = false;
            return result;
        }

        private static bool IsIdentifierPart(char ch)
        {
            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        public override async Task<ICompletionDataList> HandleCodeCompletionAsync(CodeCompletionContext completionContext, char completionChar,
            CancellationToken token = default(CancellationToken))
        {
            int triggerWordLength = 0;
            if (char.IsLetterOrDigit(completionChar) || completionChar == '_')
            {
                if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit(Editor.GetCharAt(completionContext.TriggerOffset - 2)))
                    return null;
                triggerWordLength = 1;
            }
            return await HandleCodeCompletion(completionContext, CompletionTrigger.CreateInsertionTrigger(completionChar), triggerWordLength, token);
        }

        public override async Task<ICompletionDataList> CodeCompletionCommand(CodeCompletionContext completionContext)
        {
            return await HandleCodeCompletion(completionContext, CompletionTrigger.Default, 0, default(CancellationToken));
        }

        public override Task<ParameterHintingResult> ParameterCompletionCommand(CodeCompletionContext completionContext)
        {
            char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt(completionContext.TriggerOffset - 1) : '\0';
            return HandleParameterCompletionCommand(completionContext, ch, true);
        }

        public override Task<ParameterHintingResult> HandleParameterCompletionAsync(CodeCompletionContext completionContext, char completionChar, CancellationToken token = default(CancellationToken))
        {
            return HandleParameterCompletionCommand(completionContext, completionChar, false, token);
        }

        private async Task<ParameterHintingResult> HandleParameterCompletionCommand(CodeCompletionContext completionContext, char completionChar, bool force, CancellationToken token = default(CancellationToken))
        {
            if (!force && completionChar != '(' && completionChar != '<' && completionChar != '[' && completionChar != ',')
                return null;
            if (Editor.EditMode != EditMode.Edit)
                return null;

            var document = _host.GetDocument(_documentId);
            var signatureHelp = _host.GetService<ISignatureHelpProvider>();
            var items = await signatureHelp.GetItemsAsync(document, completionContext.TriggerOffset,
                new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.TypeCharCommand, completionChar), token);

            if (items == null) return ParameterHintingResult.Empty;

            var hintingData = items.Items.Select(x => (ParameterHintingData)new RoslynParameterHintingData(x, items)).ToList();
            return new RoslynParameterHintingResult(hintingData, items, completionContext.TriggerOffset);
        }

        private class RoslynParameterHintingResult : ParameterHintingResult
        {
            public SignatureHelpItems Items { get; }

            public RoslynParameterHintingResult(List<ParameterHintingData> data, SignatureHelpItems items, int startOffset) : base(data, startOffset)
            {
                Items = items;
            }
        }

        public override Task<int> GuessBestMethodOverload(ParameterHintingResult provider, int currentOverload, CancellationToken token)
        {
            return Task.FromResult(
                (provider as RoslynParameterHintingResult)?.Items.SelectedItemIndex ?? currentOverload);
        }

        public override Task<int> GetCurrentParameterIndex(ParameterHintingResult provider, int startOffset, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(
               (provider as RoslynParameterHintingResult)?.Items.ArgumentIndex ?? 0);
        }

        private async Task<CompletionDataList> HandleCodeCompletion(CodeCompletionContext completionContext, CompletionTrigger trigger, int triggerWordLength, CancellationToken token)
        {
            var document = _host.GetDocument(_documentId);

            var completions = await CompletionService.GetService(document)
                .GetCompletionsAsync(document, completionContext.TriggerOffset,
                    trigger,
                    cancellationToken: token)
                .ConfigureAwait(false);

            var list = new CompletionDataList(
                completions?.Items.Select(x => (CompletionData)new RoslynCompletionData(document, x)) ?? Array.Empty<CompletionData>())
            {
                TriggerWordLength = triggerWordLength
            };
            //list.AutoCompleteEmptyMatch = completionResult.AutoCompleteEmptyMatch;
            // list.AutoCompleteEmptyMatchOnCurlyBrace = completionResult.AutoCompleteEmptyMatchOnCurlyBracket;
            //list.AutoSelect = completionResult.AutoSelect;
            //list.DefaultCompletionString = completionResult.DefaultCompletionString;
            // list.CloseOnSquareBrackets = completionResult.CloseOnSquareBrackets;
            if (Equals(trigger, CompletionTrigger.Default))
            {
                list.AutoCompleteUniqueMatch = true;
            }
            return list;

        }

        private class RoslynCompletionData : CompletionData
        {
            private readonly Document _document;
            private readonly CompletionItem _item;

            public RoslynCompletionData(Document document, CompletionItem item)
            {
                _document = document;
                _item = item;
            }

            public override string DisplayText
            {
                get { return _item.DisplayText; }
                set { }
            }

            public override IconId Icon
            {
                get
                {
                    var glyph = _item.GetGlyph();
                    return glyph != null ? new IconId(glyph.Value.GetGlyphIcon()) : IconId.Null;
                }
                set { }
            }

            public override void InsertCompletionText(CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
            {
                var changes = CompletionService.GetService(_document).GetChangeAsync(_document, _item).Result; // TODO: async
                CompletionText = changes.TextChange.NewText;
                base.InsertCompletionText(window, ref ka, descriptor);
            }

            public override async Task<TooltipInformation> CreateTooltipInformation(bool smartWrap, CancellationToken cancelToken)
            {
                var tooltip = new TooltipInformation();
                var description = await CompletionService.GetService(_document).GetDescriptionAsync(_document, _item, cancelToken).ConfigureAwait(true);
                tooltip.SignatureMarkup += description.TaggedParts.ToPangoMarkup();
                return tooltip;
            }
        }

        private class RoslynParameterHintingData : ParameterHintingData
        {
            private readonly SignatureHelpItem _item;
            private readonly SignatureHelpItems _signatureHelp;

            public RoslynParameterHintingData(SignatureHelpItem item, SignatureHelpItems signatureHelp) : base(item)
            {
                _item = item;
                _signatureHelp = signatureHelp;
            }

            public override string GetParameterName(int parameter) => _item.Parameters[parameter].Name;

            public override int ParameterCount => _item.Parameters.Length;

            public override bool IsParameterListAllowed => true;

            public override Task<TooltipInformation> CreateTooltipInformation(TextEditor editor, DocumentContext ctx, int currentParameter, bool smartWrap,
                CancellationToken cancelToken)
            {
                var tooltip = new TooltipInformation();
                BuildText(tooltip);
                return Task.FromResult(tooltip);
            }

            private void BuildText(TooltipInformation tooltip)
            {
                tooltip.SignatureMarkup += _item.PrefixDisplayParts.ToPangoMarkup();

                tooltip.SummaryMarkup += _item.DocumentationFactory(CancellationToken.None).ToPangoMarkup();

                if (!_item.Parameters.IsDefaultOrEmpty)
                {
                    for (var index = 0; index < _item.Parameters.Length; index++)
                    {
                        var param = _item.Parameters[index];
                        AddParameterSignatureHelp(index, param, tooltip);
                    }
                }

                tooltip.SignatureMarkup += _item.SuffixDisplayParts.ToPangoMarkup();
            }

            private void AddParameterSignatureHelp(int index, SignatureHelpParameter param, TooltipInformation tooltip)
            {
                var isSelected = _signatureHelp.ArgumentIndex == index;
                tooltip.SignatureMarkup += param.DisplayParts.ToPangoMarkup(bold: isSelected);
                if (index != _item.Parameters.Length - 1)
                {
                    tooltip.SignatureMarkup += _item.SeparatorDisplayParts.ToPangoMarkup();
                }
                if (isSelected)
                {
                    var text = param.DocumentationFactory(CancellationToken.None).ToPangoMarkup();
                    if (!string.IsNullOrEmpty(text))
                    {
                        tooltip.SummaryMarkup += param.Name + ": " + text;
                    }
                }
            }
        }
    }
}