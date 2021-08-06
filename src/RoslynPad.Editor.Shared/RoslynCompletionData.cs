using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if AVALONIA
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Avalonia.Media;
using CommonTextEventArgs = Avalonia.Input.TextInputEventArgs;
using CommonImage = Avalonia.Media.Imaging.IBitmap;
#else
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using CommonTextEventArgs = System.Windows.Input.TextCompositionEventArgs;
using CommonImage = System.Windows.Media.ImageSource;
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Editor
{
    internal sealed class RoslynCompletionData : ICompletionDataEx, INotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly CompletionItem _item;
        private readonly char? _completionChar;
        private readonly SnippetManager _snippetManager;
        private readonly Glyph _glyph;
        private readonly Lazy<Task> _descriptionTask;
        private Decorator? _description;

        public RoslynCompletionData(Document document, CompletionItem item, char? completionChar, SnippetManager snippetManager)
        {
            _document = document;
            _item = item;
            _completionChar = completionChar;
            _snippetManager = snippetManager;
            Text = item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix;
            Content = Text;
            _glyph = item.GetGlyph();
            _descriptionTask = new Lazy<Task>(RetrieveDescription);
        }

        public async void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            if (_glyph == Glyph.Snippet && CompleteSnippet(textArea, completionSegment, e))
            {
                return;
            }

            var changes = await CompletionService.GetService(_document)
                .GetChangeAsync(_document, _item, null).ConfigureAwait(true);

            var textChange = changes.TextChange;
            var document = textArea.Document;
            using (document.RunUpdate())
            {
                // we may need to remove a few typed chars since the Roslyn document isn't updated
                // while the completion window is open
                if (completionSegment.EndOffset > textChange.Span.End)
                {
                    document.Replace(
                        new TextSegment { StartOffset = textChange.Span.End, EndOffset = completionSegment.EndOffset },
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
            var txea = e as CommonTextEventArgs;
            if (txea != null && txea.Text?.Length > 0)
                completionChar = txea.Text[0];
            else if (e is KeyEventArgs kea && kea.Key == Key.Tab)
                completionChar = '\t';

            if (completionChar == '\t')
            {
                var snippet = _snippetManager.FindSnippet(_item.DisplayText);
                if (snippet != null)
                {
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
            }

            return false;
        }

#if AVALONIA
        public CommonImage? Image => null;
        public IImage? Drawing => _glyph.ToImageSource();
#else
        public CommonImage? Image => _glyph.ToImageSource();
#endif

        public string Text { get; }

        public object Content { get; }

        public object Description
        {
            get
            {
                if (_description == null)
                {
                    _description = new Decorator();
#if AVALONIA
                    _description.Initialized += (o, e) => { var task = _descriptionTask.Value; };
#else
                    _description.Loaded += (o, e) => { var task = _descriptionTask.Value; };
#endif
                }

                return _description;
            }
        }

        private async Task RetrieveDescription()
        {
            if (_description != null)
            {
                var description = await Task.Run(() => CompletionService.GetService(_document).GetDescriptionAsync(_document, _item)).ConfigureAwait(true);
                _description.Child = description.TaggedParts.ToTextBlock();
            }
        }

        public double Priority { get; private set; }

        public bool IsSelected => _item.Rules.MatchPriority == MatchPriority.Preselect;

        public string SortText => _item.SortText;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
