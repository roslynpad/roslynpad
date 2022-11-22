using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using System;

namespace RoslynPad.Editor;

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
        if (_toolTip == null)
        {
            return;
        }

        ToolTip.SetShowDelay(this, 0);
        ToolTip.SetTip(this, _toolTip);
        _toolTip.GetPropertyChangedObservable(ToolTip.IsOpenProperty).Subscribe(c =>
        {
            if (c.NewValue as bool? != true)
            {
                _toolTip = null;
            }
        });
    }

    partial void AfterToolTipOpen()
    {
        _toolTip?.InvalidateVisual();
    }

    partial class CustomCompletionWindow
    {
        partial void Initialize()
        {
            CompletionList.ListBox.BorderThickness = new Thickness(1);
            CompletionList.ListBox.PointerPressed += (o, e) => _isSoftSelectionActive = false;
        }
    }
}
