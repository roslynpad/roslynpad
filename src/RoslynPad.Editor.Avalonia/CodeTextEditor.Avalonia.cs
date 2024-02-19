namespace RoslynPad.Editor;

public partial class CodeTextEditor
{
    protected override Type StyleKeyOverride => typeof(TextEditor);

    partial void Initialize()
    {
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
}
