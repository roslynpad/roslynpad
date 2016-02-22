using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using RoslynPad.Formatting;
using RoslynPad.Roslyn.Completion;

namespace RoslynPad.Editor
{
    internal sealed class AvalonEditCompletionData : ICompletionDataEx
    {
        private readonly CompletionItem _item;
        private object _description;

        public AvalonEditCompletionData(CompletionItem item)
        {
            _item = item;
            Text = item.DisplayText;
            Content = item.DisplayText;
            if (item.Glyph != null)
            {
                Image = Application.Current.TryFindResource(item.Glyph.Value) as ImageSource;
            }
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e)
        {
            var change = _item.Rules.GetTextChange(_item);
            if (change == null) return;
            textArea.Document.Replace(completionSegment, change.Value.NewText);
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
    }
}