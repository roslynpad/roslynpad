using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using System;
using System.Linq;

namespace RoslynPad.Editor
{
    public partial class CodeTextEditor : IStyleable
    {
        Type IStyleable.StyleKey => typeof(TextEditor);

        partial void Initialize()
        {
            var lineMargin = new LineNumberMargin { Margin = new Thickness(0, 0, 10, 0) };
            lineMargin[~TextBlock.ForegroundProperty] = this[~LineNumbersForegroundProperty];
            TextArea.LeftMargins.Insert(0, lineMargin);

            PointerHover += OnMouseHover;
            PointerHoverStopped += OnMouseHoverStopped;
        }

        partial void InitializeToolTip()
        {
            if (_toolTip != null)
            {
                ToolTip.SetShowDelay(this, 0);
                ToolTip.SetTip(this, _toolTip);
                _toolTip.GetPropertyChangedObservable(ToolTip.IsOpenProperty).Subscribe(c =>
                {
                    if (!(bool)c.NewValue)
                        _toolTip = null;
                });
            }
        }

        partial void AfterToolTipOpen()
        {
            if (_toolTip != null)
            {
                _toolTip.InvalidateVisual();
            }
        }

        partial class CustomCompletionWindow
        {
            private static readonly System.Reflection.PropertyInfo LogicalChildrenProperty = typeof(StyledElement).GetProperty("LogicalChildren", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            partial void Initialize()
            {
                CompletionList.ListBox.BorderThickness = new Thickness(1);
                CompletionList.ListBox.PointerPressed +=
                    (o, e) => _isSoftSelectionActive = false;

                // HACK alert - this is due to an Avalonia bug that assumes the parent of a PopupRoot must be a Popup (in our case it's a Window)
                var toolTip = LogicalChildren.OfType<Avalonia.Controls.Primitives.Popup>().First();
                LogicalChildren.Remove(toolTip);
                var logicalChildren = (Avalonia.Collections.IAvaloniaList<Avalonia.LogicalTree.ILogical>)LogicalChildrenProperty.GetValue(TextArea);
                logicalChildren.Add(toolTip);
            }

            protected override void DetachEvents()
            {
                // TODO: temporary workaround until SetParent(null) is removed
                var selected = CompletionList.SelectedItem;
                base.DetachEvents();
                CompletionList.SelectedItem = selected;
            }
        }
    }
}
