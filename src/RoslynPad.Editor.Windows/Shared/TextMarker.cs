namespace RoslynPad.Editor;

public sealed class TextMarker : TextSegment
{
    private readonly TextMarkerService _service;

    public TextMarker(TextMarkerService service, int startOffset, int length)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        StartOffset = startOffset;
        Length = length;
    }

    public event EventHandler? Deleted;

    public bool IsDeleted => !IsConnectedToCollection;

    public void Delete()
    {
        _service.Remove(this);
    }

    internal void OnDeleted()
    {
        Deleted?.Invoke(this, EventArgs.Empty);
    }

    private void Redraw()
    {
        _service.Redraw(this);
    }

    public Color? BackgroundColor
    {
        get; set
        {
            if (!EqualityComparer<Color?>.Default.Equals(field, value))
            {
                field = value;
                Redraw();
            }
        }
    }

    public Color? ForegroundColor
    {
        get; set
        {
            if (!EqualityComparer<Color?>.Default.Equals(field, value))
            {
                field = value;
                Redraw();
            }
        }
    }

    public FontWeight? FontWeight
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                Redraw();
            }
        }
    }

    public FontStyle? FontStyle
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                Redraw();
            }
        }
    }

    public object? Tag { get; set; }

    public int Priority { get; set; }

    public Color MarkerColor
    {
        get; set
        {
            if (!EqualityComparer<Color>.Default.Equals(field, value))
            {
                field = value;
                Redraw();
            }
        }
    }

    public object? ToolTip { get; set; }
}
