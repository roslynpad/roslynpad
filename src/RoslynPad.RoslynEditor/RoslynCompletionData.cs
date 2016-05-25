using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.RoslynEditor
{
    internal sealed class RoslynCompletionData : ICompletionDataEx, INotifyPropertyChanged
    {
        private readonly CompletionItem _item;
        private readonly SnippetManager _snippetManager;
        private object _description;

        public RoslynCompletionData(CompletionItem item, SnippetManager snippetManager)
        {
            _item = item;
            _snippetManager = snippetManager;
            Text = item.DisplayText;
            Content = item.DisplayText;
            if (item.Glyph != null)
            {
                Image = item.Glyph.Value.ToImageSource();
            }
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            if (_item.Glyph == Glyph.Snippet && CompleteSnippet(textArea, completionSegment, e))
            {
                return;
            }
            var change = _item.Rules.GetTextChange(_item);
            var text = change?.NewText ?? _item.DisplayText; // workaround for keywords
            textArea.Document.Replace(completionSegment, text);
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
                    _description = _item.GetDescriptionAsync().Result.ToTextBlock();
                }
                return _description;
            }
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public double Priority { get; private set; }

        public bool IsSelected => _item.Preselect;

        public string SortText => _item.SortText;

        // avoids WPF PropertyDescriptor binding leaks
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }
}