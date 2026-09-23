#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

/// <summary>The services a minimap draws from.</summary>
internal sealed record MinimapServices(
    IViewClassifierAggregatorService Classifiers,
    IClassificationFormatMapService ClassificationFormatMaps,
    IEditorFormatMapService EditorFormatMaps,
    IViewTagAggregatorFactoryService TagAggregators);

/// <summary>
/// The minimap on the right of the text, before the vertical scroll bar (<see cref="MinimapOptions"/>). It shows while
/// <see cref="MinimapOptions.AlignmentId"/> is <see cref="MinimapAlignment.Right"/>, and takes the room beside the view
/// that the right margins otherwise float over.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(MinimapMarginNames.Minimap)]
[MarginContainer(PredefinedMarginNames.Right)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
[Order(Before = PredefinedMarginNames.VerticalScrollBar)]
public sealed class MinimapMarginProvider : IWpfTextViewMarginProvider
{
    private readonly MinimapServices _services;

    [ImportingConstructor]
    public MinimapMarginProvider(
        IViewClassifierAggregatorService classifiers,
        IClassificationFormatMapService classificationFormatMaps,
        IEditorFormatMapService editorFormatMaps,
        IViewTagAggregatorFactoryService tagAggregators)
    {
        _services = new MinimapServices(classifiers, classificationFormatMaps, editorFormatMaps, tagAggregators);
    }

    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new MinimapMargin(wpfTextViewHost, MinimapAlignment.Right, _services);
    }
}

/// <summary>
/// The minimap on the left of the text, outside the other left margins (<see cref="MinimapOptions"/>). It shows while
/// <see cref="MinimapOptions.AlignmentId"/> is <see cref="MinimapAlignment.Left"/>.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(MinimapMarginNames.LeftMinimap)]
[MarginContainer(PredefinedMarginNames.Left)]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
[Order(Before = PredefinedMarginNames.Glyph)]
public sealed class LeftMinimapMarginProvider : IWpfTextViewMarginProvider
{
    private readonly MinimapServices _services;

    [ImportingConstructor]
    public LeftMinimapMarginProvider(
        IViewClassifierAggregatorService classifiers,
        IClassificationFormatMapService classificationFormatMaps,
        IEditorFormatMapService editorFormatMaps,
        IViewTagAggregatorFactoryService tagAggregators)
    {
        _services = new MinimapServices(classifiers, classificationFormatMaps, editorFormatMaps, tagAggregators);
    }

    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new MinimapMargin(wpfTextViewHost, MinimapAlignment.Left, _services);
    }
}

/// <summary>
/// The minimap: the visual snapshot's lines drawn small, one row per line, with marks for errors, markers, find matches
/// and the selection, and a band where the viewport stands. The picture is rasterized off the render pass into a bitmap
/// and drawn again when the text, its classification, its marks or the scroll position that shifts it change; the band
/// is drawn over it on every frame.
/// </summary>
internal sealed class MinimapMargin : Control, IWpfTextViewMargin, IReservingMargin
{
    /// <summary>The room between the minimap's edges and its picture.</summary>
    internal const double Inset = 3.0;

    /// <summary>How wide the inner edge that resizes the minimap is.</summary>
    internal const double Grip = 4.0;

    private const double DragThreshold = 3.0;

    /// <summary>How far the pointer may drift while it rests for the preview.</summary>
    private const double PreviewRestTolerance = 4.0;
    private const double TextOpacity = 0.8;
    private const double ErrorOpacity = 0.7;
    private const double ChangeOpacity = 0.45;
    private const double MinLabelSize = 5.0;

    /// <summary>How long one pass may build ink before it draws what it has and goes on in the next.</summary>
    private const long InkBudgetMilliseconds = 25;

    private const int HideDelayMilliseconds = 300;
    private const int PreviewWheelLines = 3;
    private const int MaxPreviewCharacters = 500;

    private static Cursor? s_resizeCursor;

    private readonly IWpfTextViewHost _host;
    private readonly IWpfTextView _view;
    private readonly MinimapAlignment _side;
    private readonly MinimapServices _services;
    private readonly IEditorFormatMap _formatMap;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly MinimapCanvas _canvas = new();
    private readonly List<(Point Origin, FormattedText Text)> _labels = [];
    private readonly List<Mark> _marks = [];
    private MinimapPalette _palette;
    private MinimapLayout _layout;
    private MinimapInkSource? _ink;
    private ITagAggregator<IErrorTag>? _errorTags;
    private ITagAggregator<ITextMarkerTag>? _markerTags;
    private LineChangeTracker? _changes;
    private FindReplacePanel? _findPanel;
    private MinimapGlyphMasks? _glyphs;
    private MinimapPreview? _preview;
    private WriteableBitmap? _bitmap;
    private double _bitmapScale = 1.0;
    private IBrush? _textBrush;
    private VerticalScrollBarMarginProvider.VerticalScrollBarMargin? _scrollBar;
    private TopLevel? _topLevel;
    private DispatcherTimer? _hoverTimer;
    private bool _hoverTimerShows;
    private DispatcherTimer? _previewTimer;
    private Point _previewAnchor;
    private double _width;
    private bool _shown;
    private bool _reserves;
    private bool _inRange;
    private bool _hoverShown;
    private bool _rasterPending;
    private bool _pointerOver;
    private bool _bandHover;
    private bool _gripHover;
    private bool _isDisposed;
    private DragKind _drag;
    private double _dragOffset;
    private Point _pressPosition;
    private KeyModifiers _pressModifiers;
    private double _resizeStartX;
    private double _resizeStartWidth;
    private double _dragWidth = double.NaN;
    private Point _lastPointer;

    public MinimapMargin(IWpfTextViewHost host, MinimapAlignment side, MinimapServices services)
    {
        _host = host;
        _view = host.TextView;
        _side = side;
        _services = services;
        _formatMap = services.EditorFormatMaps.GetEditorFormatMap(_view);
        _classificationFormatMap = services.ClassificationFormatMaps.GetClassificationFormatMap(_view);
        _palette = MinimapPalette.Read(_formatMap, _view.Options);
        ClipToBounds = true;

        _view.Options.OptionChanged += OnOptionChanged;
        _view.LayoutChanged += OnLayoutChanged;
        _view.BackgroundBrushChanged += OnBackgroundBrushChanged;
        _view.Closed += OnViewClosed;
        _formatMap.FormatMappingChanged += OnFormatMappingChanged;
        UpdateState();
    }

    public event EventHandler? ReservesSpaceChanged;

    private enum DragKind
    {
        None,
        Band,
        PendingJump,
        Resize,
    }

    public Control VisualElement => this;

    public double MarginSize => _shown ? Bounds.Width : 0.0;

    public bool Enabled => _shown;

    public bool ReservesSpace => _reserves;

    /// <summary>Whether the picture has content: the document is within the line-count limits.</summary>
    internal bool DrawsDocument => _shown && _inRange;

    internal MinimapLayout Layout => _layout;

    internal MinimapCanvas Picture => _canvas;

    internal MinimapPreview? Preview => _preview;

    internal MinimapInkSource? Ink => _ink;

    private bool IsHoverMode
        => _side == MinimapAlignment.Right
           && _view.Options.GetOptionValue(MinimapOptions.ShowOnScrollBarHoverId)
           && !_view.Options.GetOptionValue(MinimapOptions.HideOriginalScrollBarId);

    private static Cursor ResizeCursor => s_resizeCursor ??= new Cursor(StandardCursorType.SizeWestEast);

    public ITextViewMargin? GetTextViewMargin(string marginName)
        => string.Equals(marginName, _side == MinimapAlignment.Right ? MinimapMarginNames.Minimap : MinimapMarginNames.LeftMinimap, StringComparison.OrdinalIgnoreCase)
            ? this
            : null;

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _view.Options.OptionChanged -= OnOptionChanged;
        _view.LayoutChanged -= OnLayoutChanged;
        _view.BackgroundBrushChanged -= OnBackgroundBrushChanged;
        _view.Closed -= OnViewClosed;
        _formatMap.FormatMappingChanged -= OnFormatMappingChanged;
        Release();
        if (_scrollBar is { } scrollBar)
        {
            scrollBar.SetReplaced(this, false);
            scrollBar.PointerEntered -= OnScrollBarPointerEntered;
            scrollBar.PointerExited -= OnScrollBarPointerExited;
        }

        if (_topLevel is { } topLevel)
        {
            topLevel.ScalingChanged -= OnScalingChanged;
            _topLevel = null;
        }

        _hoverTimer?.Stop();
        _previewTimer?.Stop();
        _preview?.Dispose();
        _preview = null;
    }

    /// <summary>Draws the picture now rather than when the dispatcher is idle; for tests.</summary>
    internal void RasterizeForTest() => RasterizeNow();

    protected override Size MeasureOverride(Size availableSize) => new(_width, 0.0);

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_view.InLayout)
        {
            ScheduleRaster();
        }
        else
        {
            RasterizeNow();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateScrollBar();
        if (TopLevel.GetTopLevel(this) is { } topLevel && !ReferenceEquals(topLevel, _topLevel))
        {
            if (_topLevel is { } previous)
            {
                previous.ScalingChanged -= OnScalingChanged;
            }

            _topLevel = topLevel;
            topLevel.ScalingChanged += OnScalingChanged;
        }

        ScheduleRaster();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_topLevel is { } topLevel)
        {
            topLevel.ScalingChanged -= OnScalingChanged;
            _topLevel = null;
        }

        ClosePreview();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!_shown || _isDisposed)
        {
            return;
        }

        // Strictly read-only: the picture was drawn off the render pass, and nothing here lays out or invalidates. The
        // ground is at least transparent, so the whole margin takes the pointer, drawn or not; floating over the text it
        // is the view's.
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(_palette.Background ?? (_reserves ? Brushes.Transparent : _view.Background), bounds);

        if (_bitmap is { } bitmap)
        {
            var size = bitmap.PixelSize;
            context.DrawImage(
                bitmap,
                new Rect(0.0, 0.0, size.Width, size.Height),
                new Rect(0.0, 0.0, size.Width / _bitmapScale, size.Height / _bitmapScale));
        }

        foreach (var (origin, text) in _labels)
        {
            context.DrawText(text, origin);
        }

        if (_inRange)
        {
            var band = new Rect(0.0, _layout.BandTop, bounds.Width, Math.Max(_layout.BandHeight, 2.0)).Intersect(bounds);
            if (band.Height > 0.0)
            {
                var fill = _drag == DragKind.Band ? _palette.ViewportActive : _bandHover ? _palette.ViewportHover : _palette.Viewport;
                context.FillRectangle(fill, band);
                double thickness = _view.Options.GetOptionValue(MinimapOptions.ViewportBorderThicknessId);
                if (thickness > 0.0)
                {
                    context.DrawRectangle(null, new Pen(_palette.ViewportBorder, thickness), band.Deflate(thickness / 2.0));
                }
            }
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _pointerOver = true;
        if (IsHoverMode)
        {
            _hoverTimer?.Stop();
        }

        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _pointerOver = _bandHover = _gripHover = false;
        Cursor = null;
        ClosePreview();
        if (IsHoverMode)
        {
            StartHoverTimer(show: false, HideDelayMilliseconds);
        }

        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!_shown || !point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        var position = point.Position;
        var options = _view.Options;
        ClosePreview();
        e.Handled = true;
        if (IsOnGrip(position))
        {
            _drag = DragKind.Resize;
            _resizeStartX = e.GetPosition(null).X;
            _resizeStartWidth = _dragWidth = Bounds.Width;
            e.Pointer.Capture(this);
            return;
        }

        if (!_inRange)
        {
            return;
        }

        if (_layout.BandContains(position.Y))
        {
            BeginBandDrag(position.Y - _layout.BandTop, e.Pointer);
            return;
        }

        switch (options.GetOptionValue(MinimapOptions.JumpTriggerId))
        {
            case MinimapJumpTrigger.MouseDown:
                Jump(position, e.KeyModifiers);
                BeginBandDrag(_layout.BandContains(position.Y) ? position.Y - _layout.BandTop : _layout.BandHeight / 2.0, e.Pointer);
                break;
            case MinimapJumpTrigger.MouseUp:
                _drag = DragKind.PendingJump;
                _pressPosition = position;
                _pressModifiers = e.KeyModifiers;
                e.Pointer.Capture(this);
                break;
            case MinimapJumpTrigger.None:
            default:
                break;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_shown)
        {
            return;
        }

        var position = e.GetPosition(this);
        _lastPointer = position;
        switch (_drag)
        {
            case DragKind.Resize:
                // The width stays the margin's own until the drag ends: an option written on every move would have
                // every listener of the view's options answer every move.
                double dx = e.GetPosition(null).X - _resizeStartX;
                double width = Math.Clamp(_side == MinimapAlignment.Right ? _resizeStartWidth - dx : _resizeStartWidth + dx, MinimapWidth.Narrowest, MinimapWidth.Widest);
                if (width != _dragWidth)
                {
                    _dragWidth = _width = width;
                    InvalidateMeasure();
                }

                return;
            case DragKind.Band:
                ScrollToFirstLine(_layout.FirstLineForBandTop(position.Y - _dragOffset));
                return;
            case DragKind.PendingJump:
                if (Math.Abs(position.X - _pressPosition.X) > DragThreshold || Math.Abs(position.Y - _pressPosition.Y) > DragThreshold)
                {
                    _drag = DragKind.Band;
                    _dragOffset = _layout.BandHeight / 2.0;
                    ScrollToFirstLine(_layout.FirstLineForBandTop(position.Y - _dragOffset));
                    InvalidateVisual();
                }

                return;
            case DragKind.None:
            default:
                UpdateHover(position);
                return;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_drag == DragKind.PendingJump)
        {
            Jump(_pressPosition, _pressModifiers);
        }
        else if (_drag == DragKind.Resize)
        {
            CommitWidth();
        }

        if (_drag != DragKind.None)
        {
            _drag = DragKind.None;
            e.Pointer.Capture(null);
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_drag == DragKind.Resize)
        {
            CommitWidth();
        }

        if (_drag != DragKind.None)
        {
            _drag = DragKind.None;
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || _view.IsClosed)
        {
            return;
        }

        if (_preview is { IsOpen: true } preview && e.Delta.Y != 0.0 && _view.Options.GetOptionValue(MinimapOptions.WheelMovesPreviewId))
        {
            FillPreview(preview.CenterLine + (e.Delta.Y > 0.0 ? -PreviewWheelLines : PreviewWheelLines));
            PlacePreview(e.GetPosition(this).Y);
            e.Handled = true;
            return;
        }

        // The minimap scrolls the view under the wheel, as the scroll bar does.
        if (_view is WpfTextView view)
        {
            view.HandleMouseWheel(e);
        }
    }

    private void UpdateState()
    {
        if (_isDisposed)
        {
            return;
        }

        var options = _view.Options;
        int lineCount = _view.VisualSnapshot.LineCount;
        int maxLines = options.GetOptionValue(MinimapOptions.MaxLineCountId);
        _inRange = lineCount >= options.GetOptionValue(MinimapOptions.MinLineCountId) && (maxLines == 0 || lineCount <= maxLines);
        bool hoverMode = IsHoverMode;
        bool shown = options.GetOptionValue(MinimapOptions.EnabledId)
            && options.GetOptionValue(MinimapOptions.AlignmentId) == _side
            && (_inRange || options.GetOptionValue(MinimapOptions.EmptyOutOfRangeId))
            && (!hoverMode || _hoverShown);
        bool reserves = shown && !hoverMode;
        if (!hoverMode)
        {
            _hoverShown = false;
        }

        if (shown != _shown)
        {
            _shown = shown;
            IsVisible = shown;
            if (shown)
            {
                Acquire();
            }
            else
            {
                Release();
            }
        }

        if (reserves != _reserves)
        {
            _reserves = reserves;
            ReservesSpaceChanged?.Invoke(this, EventArgs.Empty);
        }

        if (shown)
        {
            _ink?.Configure(
                options.GetOptionValue(DefaultOptions.TabSizeOptionId),
                options.GetOptionValue(MinimapOptions.SyntaxHighlightId),
                options.GetOptionValue(MinimapOptions.ShowMarkersId),
                options.GetOptionValue(MinimapOptions.MarkersPatternId));
            SetErrorTags(options.GetOptionValue(MinimapOptions.ErrorHighlightId));
            SetMarkerTags(options.GetOptionValue(MinimapOptions.MarkupHighlightId));
            SetChangeMarks(options.GetOptionValue(MinimapOptions.ChangeHighlightId));
        }

        UpdateScrollBar();
        double width = !shown ? 0.0 : double.IsNaN(_dragWidth) ? options.GetOptionValue(MinimapOptions.WidthId) : _dragWidth;
        if (width != _width)
        {
            _width = width;
            InvalidateMeasure();
        }

        ScheduleRaster();
    }

    private void Acquire()
    {
        _ink = new MinimapInkSource(_view, _services.Classifiers.GetClassifier(_view), _classificationFormatMap);
        _ink.Invalidated += OnSourceChanged;
        _view.Selection.SelectionChanged += OnSourceChanged;
        if (FindReplacePanel.Get(_view) is { } panel)
        {
            _findPanel = panel;
            panel.MatchesChanged += OnSourceChanged;
        }
    }

    private void Release()
    {
        if (_ink is { } ink)
        {
            ink.Invalidated -= OnSourceChanged;
            ink.Dispose();
            _ink = null;
        }

        _view.Selection.SelectionChanged -= OnSourceChanged;
        if (_findPanel is { } panel)
        {
            panel.MatchesChanged -= OnSourceChanged;
            _findPanel = null;
        }

        SetErrorTags(false);
        SetMarkerTags(false);
        SetChangeMarks(false);
        ClosePreview();
        _bitmap?.Dispose();
        _bitmap = null;
        _labels.Clear();
        _drag = DragKind.None;
    }

    private void SetErrorTags(bool on)
    {
        if (on && _errorTags is null)
        {
            _errorTags = _services.TagAggregators.CreateTagAggregator<IErrorTag>(_view);
            _errorTags.BatchedTagsChanged += OnTagsChanged;
        }
        else if (!on && _errorTags is { } tags)
        {
            tags.BatchedTagsChanged -= OnTagsChanged;
            tags.Dispose();
            _errorTags = null;
        }
    }

    private void SetMarkerTags(bool on)
    {
        if (on && _markerTags is null)
        {
            _markerTags = _services.TagAggregators.CreateTagAggregator<ITextMarkerTag>(_view);
            _markerTags.BatchedTagsChanged += OnTagsChanged;
        }
        else if (!on && _markerTags is { } tags)
        {
            tags.BatchedTagsChanged -= OnTagsChanged;
            tags.Dispose();
            _markerTags = null;
        }
    }

    private void SetChangeMarks(bool on)
    {
        if (on && _changes is null)
        {
            _changes = LineChangeTracker.For(_view.TextBuffer);
            _changes.Changed += OnSourceChanged;
        }
        else if (!on && _changes is { } changes)
        {
            changes.Changed -= OnSourceChanged;
            _changes = null;
        }
    }

    /// <summary>
    /// Steps the vertical scroll bar aside while the minimap stands in for it, and listens to it for the pointer when the
    /// minimap shows on hovering it. The scroll bar is found once both exist: the minimap is created before it.
    /// </summary>
    private void UpdateScrollBar()
    {
        if (_scrollBar is null
            && _host.GetTextViewMargin(PredefinedMarginNames.VerticalScrollBar) is VerticalScrollBarMarginProvider.VerticalScrollBarMargin scrollBar)
        {
            _scrollBar = scrollBar;
            if (_side == MinimapAlignment.Right)
            {
                scrollBar.PointerEntered += OnScrollBarPointerEntered;
                scrollBar.PointerExited += OnScrollBarPointerExited;
            }
        }

        _scrollBar?.SetReplaced(this, _reserves && _inRange && _view.Options.GetOptionValue(MinimapOptions.HideOriginalScrollBarId));
    }

    private void ScheduleRaster()
    {
        if (_rasterPending || !_shown || _isDisposed)
        {
            return;
        }

        _rasterPending = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_rasterPending)
                {
                    RasterizeNow();
                }
            },
            DispatcherPriority.Background);
    }

    private MinimapLayout ComputeLayout()
    {
        var visual = _view.VisualSnapshot;
        double lineHeight = _view.LineHeight;
        double firstLine = 0.0;
        double viewportLines = lineHeight > 0.0 ? _view.ViewportHeight / lineHeight : 1.0;
        if (_view is ITextView2 view2 && view2.TryGetTextViewLines(out var lines) && lines.Count > 0)
        {
            var top = lines.FirstVisibleLine;
            firstLine = VisualLineOf(top.Start, visual);
            if (top.IsFirstTextViewLineForSnapshotLine && top.IsLastTextViewLineForSnapshotLine && top.Height > 0.0)
            {
                firstLine += Math.Clamp((_view.ViewportTop - top.Top) / top.Height, 0.0, 1.0);
            }

            // Wrapped, a line takes several rows of the view: the band spans the lines the view shows rather than its
            // rows, and the room below a document that ends above the viewport's bottom as the lines it would hold.
            if ((_view.Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0)
            {
                var bottom = lines.LastVisibleLine;
                double room = lineHeight > 0.0 ? Math.Max(0.0, _view.ViewportBottom - bottom.Bottom) / lineHeight : 0.0;
                viewportLines = Math.Max(1.0, VisualLineOf(bottom.Start, visual) - Math.Floor(firstLine) + 1.0 + room);
            }
        }

        return new MinimapLayout(
            visual.LineCount,
            firstLine,
            viewportLines,
            Bounds.Height,
            _view.Options.GetOptionValue(MinimapOptions.PixelsPerLineId),
            _view.Options.GetOptionValue(MinimapOptions.SizingId));
    }

    private int VisualLineOf(SnapshotPoint editPoint, ITextSnapshot visual)
        => _view.TextViewModel.GetNearestPointInVisualSnapshot(editPoint, visual, PointTrackingMode.Negative).GetContainingLine().LineNumber;

    private void RasterizeNow()
    {
        _rasterPending = false;

        // Nothing is drawn before the view's first layout, and nothing here may bring that layout forward: the first
        // layout that is published raises LayoutChanged, which draws.
        if (!_shown || _isDisposed || _view.IsClosed || _ink is null || _view is not ITextView2 view2 || !view2.TryGetTextViewLines(out _))
        {
            return;
        }

        _layout = ComputeLayout();
        _labels.Clear();
        _textBrush = _classificationFormatMap.DefaultTextProperties.ForegroundBrush;
        var options = _view.Options;
        double scale = options.GetOptionValue(MinimapOptions.HiDpiId) ? TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0 : 1.0;
        int width = (int)Math.Ceiling(Bounds.Width * scale);
        int height = (int)Math.Ceiling(Bounds.Height * scale);
        if (width <= 0 || height <= 0)
        {
            InvalidateVisual();
            return;
        }

        _canvas.Reset(width, height);
        bool pending = false;
        if (_inRange)
        {
            var visual = _view.VisualSnapshot;
            int first = _layout.FirstDrawnLine;
            int last = _layout.LastDrawnLine;
            DrawMarks(visual, first, last, scale);
            pending = DrawText(visual, first, last, scale);
        }

        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96.0 * scale, 96.0 * scale), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        _bitmapScale = scale;
        _canvas.CopyTo(_bitmap);
        InvalidateVisual();
        if (pending)
        {
            ScheduleRaster();
        }
    }

    /// <summary>Draws the text of the lines <paramref name="first"/> to <paramref name="last"/>; true when some ink is still to be built.</summary>
    private bool DrawText(ITextSnapshot visual, int first, int last, double scale)
    {
        var options = _view.Options;
        double pitch = _layout.Pitch * scale;
        double column = options.GetOptionValue(MinimapOptions.PixelsPerLineId) / 2.0 * scale;
        double left = Inset * scale;

        // A character is its glyph's miniature where a line has two device pixels or more; below that only its presence
        // shows, and a run of them is one bar, lines apart by a gap where there is room for one.
        var glyphs = options.GetOptionValue(MinimapOptions.RenderStyleId) == MinimapRenderStyle.Accurate && pitch >= 2.0 ? Glyphs() : null;
        double barHeight = glyphs is null && pitch >= 2.0 ? pitch * 2.0 / 3.0 : pitch;

        double labelSize = options.GetOptionValue(MinimapOptions.MarkersScaleId) * _layout.Pitch;
        var labelTypeface = new Typeface(_classificationFormatMap.DefaultTextProperties.Typeface.FontFamily, FontStyle.Normal, FontWeight.Bold);

        var clock = Stopwatch.StartNew();
        bool pending = false;
        for (int line = first; line <= last; line++)
        {
            var ink = _ink!.Get(visual, line, build: clock.ElapsedMilliseconds < InkBudgetMilliseconds);
            if (ink is null)
            {
                pending = true;
                continue;
            }

            double top = _layout.YOf(line) * scale;
            foreach (var run in ink.Runs)
            {
                double x = left + (run.Start * column);
                if (x >= _canvas.Width)
                {
                    break;
                }

                if (glyphs is null)
                {
                    _canvas.Fill(x, top, left + (run.End * column), top + barHeight, run.Color, TextOpacity);
                    continue;
                }

                for (int c = run.Start; c < run.End; c++)
                {
                    DrawGlyph(glyphs, ink.Columns[c], left + (c * column), top, column, barHeight, run.Color);
                }
            }

            if (labelSize >= MinLabelSize && ink.MarkerLabel is { } label)
            {
                var brush = _palette.Marker ?? (ink.Runs.Length > 0 ? new SolidColorBrush(ink.Runs[0].Color) : _textBrush) ?? Brushes.Gray;
                var text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, labelTypeface, labelSize, brush);
                _labels.Add((new Point(Inset, _layout.YOf(line)), text));
            }
        }

        return pending;
    }

    private void DrawGlyph(MinimapGlyphMasks glyphs, char c, double left, double top, double width, double height, Color color)
    {
        if (glyphs.Get(c) is not { } mask)
        {
            _canvas.Fill(left, top, left + width, top + height, color, TextOpacity);
            return;
        }

        double cellWidth = width / MinimapGlyphMasks.Columns;
        double cellHeight = height / MinimapGlyphMasks.Rows;
        for (int row = 0; row < MinimapGlyphMasks.Rows; row++)
        {
            for (int column = 0; column < MinimapGlyphMasks.Columns; column++)
            {
                float density = mask[(row * MinimapGlyphMasks.Columns) + column];
                if (density > 0f)
                {
                    double x = left + (column * cellWidth);
                    double y = top + (row * cellHeight);
                    _canvas.Fill(x, y, x + cellWidth, y + cellHeight, color, density);
                }
            }
        }
    }

    private MinimapGlyphMasks Glyphs()
    {
        var typeface = _classificationFormatMap.DefaultTextProperties.Typeface;
        return _glyphs is { } glyphs && glyphs.Typeface == typeface ? glyphs : _glyphs = new MinimapGlyphMasks(typeface);
    }

    /// <summary>Draws the marks of the lines <paramref name="first"/> to <paramref name="last"/>: markup first, errors over it.</summary>
    private void DrawMarks(ITextSnapshot visual, int first, int last, double scale)
    {
        var options = _view.Options;
        var range = new SnapshotSpan(visual.GetLineFromLineNumber(first).Start, visual.GetLineFromLineNumber(last).EndIncludingLineBreak);
        _marks.Clear();

        // The changes lie under everything else: they say where the text moved, the other marks what is in it.
        if (_changes is { Snapshot: { } changed } changes)
        {
            AddChangeMarks(visual, range, changed, changes.Kinds);
        }

        if (options.GetOptionValue(MinimapOptions.MarkupHighlightId))
        {
            bool fullLine = options.GetOptionValue(MinimapOptions.MarkupFullLineId);
            if (MinimapInkSource.ColorOf(_palette.Selection) is { } selection)
            {
                foreach (var span in _view.Selection.SelectedSpans)
                {
                    AddEditMarks(visual, range, span, selection, 1.0, fullLine);
                }
            }

            if (_findPanel?.Matches is { Count: > 0 } matches && MinimapInkSource.ColorOf(_palette.FindMatch) is { } find)
            {
                foreach (var span in matches)
                {
                    AddEditMarks(visual, range, span, find, 1.0, fullLine);
                }
            }

            if (_markerTags is { } markers)
            {
                foreach (var tag in markers.GetTags(range))
                {
                    if (MinimapPalette.MarkerColor(_formatMap, tag.Tag.Type) is { } color)
                    {
                        foreach (var span in tag.Span.GetSpans(visual))
                        {
                            AddMarks(visual, range, span, color, 1.0, fullLine);
                        }
                    }
                }
            }
        }

        if (options.GetOptionValue(MinimapOptions.ErrorHighlightId) && _errorTags is { } errors)
        {
            bool fullLine = options.GetOptionValue(MinimapOptions.ErrorFullLineId);
            foreach (var tag in errors.GetTags(range))
            {
                var color = MinimapPalette.ErrorColor(_formatMap, tag.Tag.ErrorType ?? string.Empty);
                foreach (var span in tag.Span.GetSpans(visual))
                {
                    AddMarks(visual, range, span, color, ErrorOpacity, fullLine);
                }
            }
        }

        double pitch = _layout.Pitch * scale;
        double column = options.GetOptionValue(MinimapOptions.PixelsPerLineId) / 2.0 * scale;
        double left = Inset * scale;
        foreach (var mark in _marks)
        {
            double top = _layout.YOf(mark.Line) * scale;
            double x0 = mark.EndColumn < 0 ? 0.0 : left + (mark.StartColumn * column);
            double x1 = mark.EndColumn < 0 ? _canvas.Width : left + (mark.EndColumn * column);
            _canvas.Fill(x0, top, x1, top + Math.Max(pitch, 1.0), mark.Color, mark.Opacity);
        }
    }

    /// <summary>Adds a full-width mark for every run of added or changed lines of <paramref name="changed"/>.</summary>
    private void AddChangeMarks(ITextSnapshot visual, SnapshotSpan range, ITextSnapshot changed, IReadOnlyList<LineChangeKind> kinds)
    {
        var added = MinimapInkSource.ColorOf(_palette.Added);
        var modified = MinimapInkSource.ColorOf(_palette.Modified);
        int count = Math.Min(kinds.Count, changed.LineCount);
        int line = 0;
        while (line < count)
        {
            var kind = kinds[line];
            int end = line + 1;
            while (end < count && kinds[end] == kind)
            {
                end++;
            }

            if (kind != LineChangeKind.None && (kind == LineChangeKind.Added ? added : modified) is { } color)
            {
                var span = new SnapshotSpan(changed.GetLineFromLineNumber(line).Start, changed.GetLineFromLineNumber(end - 1).EndIncludingLineBreak);
                AddEditMarks(visual, range, span, color, ChangeOpacity, fullLine: true);
            }

            line = end;
        }
    }

    private void AddEditMarks(ITextSnapshot visual, SnapshotSpan range, SnapshotSpan editSpan, Color color, double opacity, bool fullLine)
    {
        if (editSpan.IsEmpty || !ReferenceEquals(editSpan.Snapshot.TextBuffer, _view.TextBuffer))
        {
            return;
        }

        var current = editSpan.TranslateTo(_view.TextBuffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);
        foreach (var span in _view.BufferGraph.MapUpToSnapshot(current, SpanTrackingMode.EdgeInclusive, visual))
        {
            AddMarks(visual, range, span, color, opacity, fullLine);
        }
    }

    /// <summary>Adds the marks a span of the visual snapshot makes on the lines of <paramref name="range"/>.</summary>
    private void AddMarks(ITextSnapshot visual, SnapshotSpan range, SnapshotSpan span, Color color, double opacity, bool fullLine)
    {
        if (span.IsEmpty || span.End < range.Start || span.Start > range.End || _ink is null)
        {
            return;
        }

        int startLine = visual.GetLineNumberFromPosition(span.Start);
        int endLine = visual.GetLineNumberFromPosition(span.End);

        // A span ending where a line starts leaves that line unmarked.
        if (endLine > startLine && visual.GetLineFromLineNumber(endLine).Start == span.End)
        {
            endLine--;
        }

        int firstLine = visual.GetLineNumberFromPosition(range.Start);
        int lastLine = visual.GetLineNumberFromPosition(range.End);
        for (int number = Math.Max(firstLine, startLine); number <= Math.Min(lastLine, endLine); number++)
        {
            if (fullLine)
            {
                _marks.Add(new Mark(number, 0, -1, color, opacity));
                continue;
            }

            var line = visual.GetLineFromLineNumber(number);
            string text = line.Length > MinimapInkSource.MaxColumns ? visual.GetText(line.Start, MinimapInkSource.MaxColumns) : line.GetText();
            int start = number == startLine ? span.Start - line.Start : 0;
            int end = number == endLine ? Math.Min(span.End - line.Start, line.Length) : line.Length;
            int startColumn = _ink.ColumnOf(text, start);
            _marks.Add(new Mark(number, startColumn, Math.Max(startColumn + 1, _ink.ColumnOf(text, end)), color, opacity));
        }
    }

    private void Jump(Point position, KeyModifiers modifiers)
    {
        var visual = _view.VisualSnapshot;
        var visualLine = visual.GetLineFromLineNumber(Math.Min(_layout.LineAt(position.Y), visual.LineCount - 1));
        var options = _view.Options;
        if (options.GetOptionValue(MinimapOptions.ClickTargetId) == MinimapClickTarget.MousePosition)
        {
            ScrollToFirstLine(_layout.FirstLineForBandTop(position.Y - (_layout.BandHeight / 2.0)));
        }
        else
        {
            _view.DisplayTextLineContainingBufferPosition(
                EditPoint(visualLine.Start),
                Math.Max(0.0, (_view.ViewportHeight - _view.LineHeight) / 2.0),
                ViewRelativePosition.Top);
        }

        if (!options.GetOptionValue(MinimapOptions.MoveOnlyId))
        {
            MoveCaret(visualLine, position.X, (modifiers & KeyModifiers.Shift) != 0);
        }
    }

    private void MoveCaret(ITextSnapshotLine visualLine, double x, bool extend)
    {
        double column = _view.Options.GetOptionValue(MinimapOptions.PixelsPerLineId) / 2.0;
        int columnIndex = Math.Max(0, (int)Math.Floor((x - Inset) / column));
        int offset = Math.Min(_ink?.IndexOfColumn(visualLine.GetText(), columnIndex) ?? 0, visualLine.Length);
        var target = new VirtualSnapshotPoint(EditPoint(visualLine.Start + offset));
        var anchor = extend
            ? _view.Selection.IsEmpty ? _view.Caret.Position.VirtualBufferPosition : _view.Selection.AnchorPoint
            : target;
        if (_view is WpfTextView view)
        {
            // Without scrolling: the jump already put the line in view.
            view.EditorOperations.SelectAndMoveCaret(anchor, target, TextSelectionMode.Stream, scrollOptions: null);
        }
        else
        {
            _view.Selection.Select(anchor, target);
            _view.Caret.MoveTo(target);
        }

        _view.VisualElement.Focus();
    }

    /// <summary>Writes the width a drag of the inner edge ended at to the view's options.</summary>
    private void CommitWidth()
    {
        if (!double.IsNaN(_dragWidth))
        {
            double width = _dragWidth;
            _dragWidth = double.NaN;
            _view.Options.SetOptionValue(MinimapOptions.WidthId, width);
        }
    }

    private void BeginBandDrag(double offset, IPointer pointer)
    {
        _drag = DragKind.Band;
        _dragOffset = Math.Clamp(offset, 0.0, Math.Max(0.0, _layout.BandHeight));
        pointer.Capture(this);
        InvalidateVisual();
    }

    private void ScrollToFirstLine(double line)
    {
        var visual = _view.VisualSnapshot;
        int number = Math.Clamp((int)Math.Round(line), 0, visual.LineCount - 1);
        _view.DisplayTextLineContainingBufferPosition(EditPoint(visual.GetLineFromLineNumber(number).Start), 0.0, ViewRelativePosition.Top);
    }

    private SnapshotPoint EditPoint(SnapshotPoint visualPoint)
        => _view.BufferGraph.MapDownToSnapshot(visualPoint, PointTrackingMode.Negative, _view.TextSnapshot, PositionAffinity.Successor)
           ?? new SnapshotPoint(_view.TextSnapshot, 0);

    private bool IsOnGrip(Point position)
        => !_view.Options.GetOptionValue(MinimapOptions.LockWidthId)
           && (_side == MinimapAlignment.Right ? position.X < Grip : position.X >= Bounds.Width - Grip);

    private void UpdateHover(Point position)
    {
        bool grip = IsOnGrip(position);
        bool band = _inRange && !grip && _layout.BandContains(position.Y);
        if (grip != _gripHover || band != _bandHover)
        {
            _gripHover = grip;
            _bandHover = band;
            Cursor = grip ? ResizeCursor : null;
            InvalidateVisual();
        }

        // The band stands for what the view shows already, and below the document's end there is nothing to show: the
        // preview opens only over the picture off the band, once the pointer rests.
        if (!_inRange || grip || band || !IsOverDocument(position.Y) || !_view.Options.GetOptionValue(MinimapOptions.ShowPreviewId))
        {
            ClosePreview();
        }
        else if (_preview is { IsOpen: true } preview)
        {
            int line = _layout.LineAt(position.Y);
            if (preview.CenterLine != line)
            {
                FillPreview(line);
            }

            PlacePreview(position.Y);
        }
        else
        {
            SchedulePreview(position);
        }
    }

    /// <summary>Opens the preview once the pointer has rested near <paramref name="position"/> for the preview delay.</summary>
    private void SchedulePreview(Point position)
    {
        int delay = _view.Options.GetOptionValue(MinimapOptions.PreviewDelayId);
        if (delay <= 0)
        {
            OpenPreview(position);
            return;
        }

        if (_previewTimer is { IsEnabled: true }
            && Math.Abs(position.X - _previewAnchor.X) <= PreviewRestTolerance
            && Math.Abs(position.Y - _previewAnchor.Y) <= PreviewRestTolerance)
        {
            return;
        }

        if (_previewTimer is null)
        {
            _previewTimer = new DispatcherTimer();
            _previewTimer.Tick += OnPreviewTimerTick;
        }

        _previewAnchor = position;
        _previewTimer.Stop();
        _previewTimer.Interval = TimeSpan.FromMilliseconds(delay);
        _previewTimer.Start();
    }

    private void OpenPreview(Point position)
    {
        FillPreview(_layout.LineAt(position.Y));
        PlacePreview(position.Y);
    }

    private void ClosePreview()
    {
        _previewTimer?.Stop();
        _preview?.Hide();
    }

    private void OnPreviewTimerTick(object? sender, EventArgs e)
    {
        _previewTimer?.Stop();
        var position = _lastPointer;
        if (_shown && _inRange && _pointerOver && _drag == DragKind.None && !IsOnGrip(position) && !_layout.BandContains(position.Y)
            && IsOverDocument(position.Y) && _view.Options.GetOptionValue(MinimapOptions.ShowPreviewId))
        {
            OpenPreview(position);
        }
    }

    /// <summary>Whether <paramref name="y"/> is over the document's picture rather than below its last line.</summary>
    private bool IsOverDocument(double y) => y < _layout.YOf(_layout.LineCount);

    private void FillPreview(int centerLine)
    {
        var visual = _view.VisualSnapshot;
        int count = _view.Options.GetOptionValue(MinimapOptions.PreviewLineCountId);
        centerLine = Math.Clamp(centerLine, 0, visual.LineCount - 1);
        int first = Math.Clamp(centerLine - (count / 2), 0, Math.Max(0, visual.LineCount - count));
        int last = Math.Min(visual.LineCount - 1, first + count - 1);
        var lines = new List<PreviewLine>(last - first + 1);
        for (int number = first; number <= last; number++)
        {
            lines.Add(PreviewLineOf(visual.GetLineFromLineNumber(number)));
        }

        // Set as the editor sets its text, zoom included, with the numbers and the text where the editor's own stand.
        var properties = _classificationFormatMap.DefaultTextProperties;
        var popup = PopupBrushes.Read(_formatMap);
        double zoom = _view.ZoomLevel / 100.0;
        var page = PreviewPage();
        _preview ??= new MinimapPreview();
        _preview.SetContent(
            centerLine,
            lines,
            centerLine - first,
            new PreviewStyle(
                properties.Typeface,
                properties.FontRenderingEmSize * zoom,
                _view.LineHeight * zoom,
                properties.ForegroundBrush ?? popup.Foreground,
                LineNumberMarginProvider.NumberBrush,
                MinimapInkSource.ColorOf(_view.Background) is { A: > 0 } ? _view.Background : popup.Background,
                popup.BorderBrush,
                _palette.ViewportHover,
                NumbersRight: LineNumbersEnd(zoom) is { } numbersEnd ? numbersEnd - page.Left : 0.0,
                TextLeft: LeftOf(_view.VisualElement) - page.Left));
    }

    /// <summary>
    /// The part of the editor the preview covers, in this margin's coordinates: the host's height, and its width on the
    /// text's side of the minimap, gutter included, up to the right margins that float over the text there.
    /// </summary>
    private Rect PreviewPage()
    {
        var hostControl = _host.HostControl;
        var hostTopLeft = hostControl.TranslatePoint(default, this) ?? default;
        double left;
        double right;
        if (_side == MinimapAlignment.Right)
        {
            left = hostTopLeft.X;
            right = -MinimapPreview.Gap;
        }
        else
        {
            left = Bounds.Width + MinimapPreview.Gap;
            right = _host.GetTextViewMargin(PredefinedMarginNames.Right) is { } rightMargins
                ? LeftOf(rightMargins.VisualElement)
                : hostTopLeft.X + hostControl.Bounds.Width;
        }

        return new Rect(left, hostTopLeft.Y, Math.Max(0.0, right - left), hostControl.Bounds.Height);
    }

    /// <summary>Where the editor's line numbers end, in this margin's coordinates; null while it shows none.</summary>
    private double? LineNumbersEnd(double zoom)
        => _host.GetTextViewMargin(PredefinedMarginNames.LineNumber) is { Enabled: true } numbers
           && numbers.VisualElement.TranslatePoint(new Point(numbers.VisualElement.Bounds.Width, 0.0), this) is { } end
            ? end.X - (LineNumberMarginProvider.NumberInset * zoom)
            : null;

    private double LeftOf(Visual visual) => visual.TranslatePoint(default, this)?.X ?? 0.0;

    private PreviewLine PreviewLineOf(ITextSnapshotLine line)
    {
        string text = line.Length > MaxPreviewCharacters ? line.Snapshot.GetText(line.Start, MaxPreviewCharacters) : line.GetText();
        int number = EditPoint(line.Start).GetContainingLine().LineNumber + 1;
        var runs = new List<(string, IBrush?)>();
        if (_ink is not { } ink)
        {
            return new PreviewLine(number, runs);
        }

        // Tabs are expanded here: a text block's own tab stops are not the editor's.
        int column = 0;
        var builder = new StringBuilder();
        foreach (var span in ink.Classify(line, text.Length))
        {
            builder.Clear();
            for (int i = span.Start; i < span.End; i++)
            {
                if (text[i] == '\t')
                {
                    int width = ink.TabSize - (column % ink.TabSize);
                    builder.Append(' ', width);
                    column += width;
                }
                else
                {
                    builder.Append(text[i]);
                    column++;
                }
            }

            runs.Add((builder.ToString(), span.Brush));
        }

        return new PreviewLine(number, runs);
    }

    private void PlacePreview(double y) => _preview?.Place(this, PreviewPage(), y);

    private void StartHoverTimer(bool show, int delayMilliseconds)
    {
        _hoverTimer?.Stop();
        if (show && delayMilliseconds <= 0)
        {
            SetHoverShown(true);
            return;
        }

        if (_hoverTimer is null)
        {
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Tick += OnHoverTimerTick;
        }

        _hoverTimerShows = show;
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, delayMilliseconds));
        _hoverTimer.Start();
    }

    private void SetHoverShown(bool shown)
    {
        if (_hoverShown != shown)
        {
            _hoverShown = shown;
            UpdateState();
        }
    }

    private void OnHoverTimerTick(object? sender, EventArgs e)
    {
        _hoverTimer?.Stop();
        if (_hoverTimerShows)
        {
            SetHoverShown(true);
        }
        else if (!IsPointerOver && _scrollBar?.IsPointerOver != true && _drag == DragKind.None)
        {
            SetHoverShown(false);
        }
    }

    private void OnScrollBarPointerEntered(object? sender, PointerEventArgs e)
    {
        if (IsHoverMode && _view.Options.GetOptionValue(MinimapOptions.EnabledId))
        {
            StartHoverTimer(show: true, _view.Options.GetOptionValue(MinimapOptions.ScrollBarHoverDelayId));
        }
    }

    private void OnScrollBarPointerExited(object? sender, PointerEventArgs e)
    {
        if (IsHoverMode)
        {
            StartHoverTimer(show: false, HideDelayMilliseconds);
        }
    }

    private void OnOptionChanged(object? sender, EditorOptionChangedEventArgs e)
    {
        if (e.OptionId.StartsWith("Minimap/", StringComparison.Ordinal)
            || string.Equals(e.OptionId, DefaultOptions.TabSizeOptionName, StringComparison.Ordinal))
        {
            _palette = MinimapPalette.Read(_formatMap, _view.Options);
            UpdateState();
        }
    }

    private void OnLayoutChanged(object? sender, TextViewLayoutChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        if (e.OldSnapshot != e.NewSnapshot)
        {
            // The line count may have crossed a limit.
            UpdateState();
        }
        else if (_scrollBar is null)
        {
            // A host that is laid out before it is attached meets its scroll bar here first.
            UpdateScrollBar();
        }

        if (!_shown)
        {
            return;
        }

        var layout = ComputeLayout();
        if (_bitmap is null
            || layout.LineCount != _layout.LineCount
            || Math.Abs(layout.Pitch - _layout.Pitch) > 1e-9
            || Math.Abs(layout.Offset - _layout.Offset) > 0.01
            || Math.Abs(layout.Height - _layout.Height) > 0.01)
        {
            // The picture itself moved.
            RasterizeNow();
        }
        else
        {
            _layout = layout;
            InvalidateVisual();
        }

        // An open preview follows the text under the pointer, and closes when the band has come under it.
        if (_preview is { IsOpen: true } && _pointerOver && _drag == DragKind.None)
        {
            UpdateHover(_lastPointer);
        }
    }

    private void OnSourceChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ScheduleRaster);
            return;
        }

        ScheduleRaster();
    }

    private void OnTagsChanged(object? sender, BatchedTagsChangedEventArgs e) => OnSourceChanged(sender, e);

    private void OnFormatMappingChanged(object? sender, FormatItemsEventArgs e)
    {
        _palette = MinimapPalette.Read(_formatMap, _view.Options);
        ScheduleRaster();
        InvalidateVisual();
    }

    private void OnBackgroundBrushChanged(object? sender, BackgroundBrushChangedEventArgs e) => InvalidateVisual();

    private void OnScalingChanged(object? sender, EventArgs e) => ScheduleRaster();

    private void OnViewClosed(object? sender, EventArgs e) => Dispose();

    /// <summary>A mark on a line over the columns <c>[StartColumn, EndColumn)</c>; an <see cref="EndColumn"/> below 0 spans the minimap's width.</summary>
    private readonly record struct Mark(int Line, int StartColumn, int EndColumn, Color Color, double Opacity);
}
