namespace RoslynPad.Editor;

partial class CodeTextEditor
{
    static CodeTextEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CodeTextEditor), new FrameworkPropertyMetadata(typeof(CodeTextEditor)));
    }

    partial void Initialize()
    {
        MouseHover += OnMouseHover;
        MouseHoverStopped += OnMouseHoverStopped;
        PreviewKeyDown += OnPreviewKeyDown;

        ToolTipService.SetInitialShowDelay(this, 0);
        SearchReplacePanel = SearchReplacePanel.Install(this);
    }

    public SearchReplacePanel SearchReplacePanel { get => field!; private set; }

    partial void InitializeToolTip()
    {
        if (_toolTip != null)
        {
            _toolTip.Closed += (o, a) => _toolTip = null;
            ToolTipService.SetInitialShowDelay(_toolTip, 0);
            _toolTip.PlacementTarget = this; // required for property inheritance
        }
    }
}
