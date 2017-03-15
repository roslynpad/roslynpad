using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using RoslynPad.Annotations;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Editor.Windows
{
    internal sealed class RoslynCompletionData : ICompletionDataEx, INotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly CompletionItem _item;
        private readonly char? _completionChar;
        private readonly SnippetManager _snippetManager;
        private readonly Glyph _glyph;
        private object _description;

        public RoslynCompletionData(Document document, CompletionItem item, char? completionChar, SnippetManager snippetManager)
        {
            _document = document;
            _item = item;
            _completionChar = completionChar;
            _snippetManager = snippetManager;
            Text = item.DisplayText;
            Content = item.DisplayText;
            _glyph = item.GetGlyph();
            Image = _glyph.ToImageSource();
        }

        public async void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            if (_glyph == Glyph.Snippet && CompleteSnippet(textArea, completionSegment, e))
            {
                return;
            }

            var changes = await CompletionService.GetService(_document)
                .GetChangeAsync(_document, _item, _completionChar).ConfigureAwait(true);

            var textChange = changes.TextChange;
            var document = textArea.Document;
            using (document.RunUpdate())
            {
                // we may need to remove a few typed chars since the Roslyn document isn't updated
                // while the completion window is open
                if (completionSegment.EndOffset > textChange.Span.End)
                {
                    document.Replace(
                        new TextSegment {StartOffset = textChange.Span.End, EndOffset = completionSegment.EndOffset},
                        string.Empty);
                }

                document.Replace(textChange.Span.Start, textChange.Span.Length,
                    new StringTextSource(textChange.NewText));
            }

            if (changes.NewPosition != null)
            {
                textArea.Caret.Offset = changes.NewPosition.Value;
            }
        }

        private bool CompleteSnippet(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            char? completionChar = null;
            var txea = e as TextCompositionEventArgs;
            var kea = e as KeyEventArgs;
            if (txea != null && txea.Text.Length > 0)
                completionChar = txea.Text[0];
            else if (kea != null && kea.Key == Key.Tab)
                completionChar = '\t';

            if (completionChar == '\t')
            {
                var snippet = _snippetManager.FindSnippet(_item.DisplayText);
                Debug.Assert(snippet != null, "snippet != null");
                var editorSnippet = snippet.CreateAvalonEditSnippet();
                using (textArea.Document.RunUpdate())
                {
                    textArea.Document.Remove(completionSegment.Offset, completionSegment.Length);
                    editorSnippet.Insert(textArea);
                }
                if (txea != null)
                {
                    txea.Handled = true;
                }
                return true;
            }
            return false;
        }

        public ImageSource Image { get; }

        public string Text { get; }

        public object Content { get; }

        public object Description
        {
            get
            {
                if (_description == null)
                {
                    RetrieveDescription();
                }
                return _description;
            }
        }

        private async void RetrieveDescription()
        {
            var description = await CompletionService.GetService(_document).GetDescriptionAsync(_document, _item).ConfigureAwait(true);
            _description = description.TaggedParts.ToTextBlock();
            OnPropertyChanged(nameof(Description));
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public double Priority { get; private set; }

        public bool IsSelected => _item.Rules.MatchPriority == MatchPriority.Preselect;

        public string SortText => _item.SortText;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}