namespace RoslynPad.Editor;

public sealed class ToolTipRequestEventArgs : RoutedEventArgs
{
    public ToolTipRequestEventArgs()
    {
        RoutedEvent = CodeTextEditor.ToolTipRequestEvent;
    }

    public bool InDocument { get; set; }

    public TextLocation LogicalPosition { get; set; }

    public int Position { get; set; }

    public object? ContentToShow { get; set; }

    public void SetToolTip(object content)
    {
        Handled = true;
        ContentToShow = content ?? throw new ArgumentNullException(nameof(content));
    }
}
