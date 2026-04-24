namespace RoslynPad.Editor;

public partial class CodeTextEditor
{
    protected override Type StyleKeyOverride => typeof(TextEditor);

    partial void Initialize()
    {
        PointerHover += OnMouseHover;
        PointerExited += OnPointerExited;
        KeyDownEvent.AddClassHandler<CodeTextEditor>(OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        ToolTip.SetTip(this, null);
        _toolTip = null;
    }

    partial void InitializeToolTip()
    {
        if (_toolTip == null)
        {
            return;
        }

        ToolTip.SetShowDelay(this, 0);
        ToolTip.SetTip(this, _toolTip);
    }

    partial void AfterToolTipOpen()
    {
        _toolTip?.InvalidateVisual();
    }
}
