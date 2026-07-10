#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Formatting.Implementation;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The Avalonia text view. Rendering is viewport-only: each layout formats just the snapshot
/// lines needed to fill the viewport, positions their visuals on the text layer,
/// and publishes the result as an <see cref="IWpfTextViewLineCollection"/>. All layout is a
/// pure function of (snapshot, viewport, format map, options); the view holds no mutable
/// document state.
/// </summary>
internal sealed class WpfTextView : Panel, IWpfTextView, ITextView2
{
    private readonly TextEditorFactoryService _factory;
    private readonly ITextViewModel _viewModel;
    private readonly ITextViewRoleSet _roles;
    private readonly IEditorOptions _options;
    private readonly IBufferGraph _bufferGraph;
    private IClassifier? _classifier;
    private readonly IClassificationFormatMap _classificationFormatMap;
    private readonly IEditorFormatMap _editorFormatMap;
    private readonly PropertyCollection _properties = new();
    private readonly Canvas _textLayer = new() { ClipToBounds = true };
    private readonly Dictionary<string, AdornmentLayer> _adornmentLayers = new(StringComparer.OrdinalIgnoreCase);
    private readonly TextCaret _caret;
    private readonly TextSelection _selection;
    private readonly ViewScroller _viewScroller;
    private readonly SelectionLayer _selectionLayer;
    private readonly CaretLayer _caretLayer;
    private readonly Queue<Action> _postLayoutActions = new();
    private IMultiSelectionBroker? _multiSelectionBroker;
    private Microsoft.VisualStudio.Text.Operations.IEditorOperations? _editorOperations;
    private EditorTextInputMethodClient? _textInputMethodClient;
    private ITextAndAdornmentSequencer? _sequencer;
    private ILineTransformSource? _lineTransformSource;
    private bool _lineTransformSourceCreated;

    private FormattedLineSource? _lineSource;
    private double _lineSourceWrapWidth = -1.0;
    private WpfTextViewLineCollection? _lineCollection;
    private FormattedLineSource? _lineCollectionSource;
    private Dictionary<int, List<FormattedLine>>? _reusableRows;
    private readonly HashSet<FormattedLine> _reusedRows = [];
    private readonly Dictionary<FormattedLine, double> _reusedOldTops = [];

    // Spans (on the rendered snapshot) whose classification changed since the last layout.
    // Rows crossing them are excluded from the line cache so the next layout reformats them
    // with fresh classification. Only meaningful while the line source is unchanged; an edit
    // invalidates the whole source anyway.
    private readonly List<Span> _classificationDamage = [];
    private ITextSnapshot _textSnapshot;
    private readonly Border _backgroundLayer = new() { Background = Brushes.Transparent };
    private double _viewportLeft;
    private double _viewportWidth;
    private double _viewportHeight;
    private double _maxTextRightCoordinate;
    private double _zoomLevel = 100.0;
    private SnapshotPoint _anchorPosition;
    private double _anchorDistance;
    private bool _inLayout;
    private bool _isClosed;
    private bool _layoutQueued;
    private ITrackingSpan? _provisionalTextHighlight;
    private readonly SpaceReservationStack _spaceReservationStack;

    public WpfTextView(
        TextEditorFactoryService factory,
        ITextViewModel viewModel,
        ITextViewRoleSet roles,
        IEditorOptionsFactoryService optionsFactory,
        IEditorOptions parentOptions,
        IClassifier? classifier,
        IClassificationFormatMap classificationFormatMap,
        IEditorFormatMap editorFormatMap,
        IBufferGraph bufferGraph)
    {
        _factory = factory;
        _viewModel = viewModel;
        _roles = roles;

        // The view's options are scoped to the view itself (TextView/* options are invalid
        // in buffer scope) and chain to the caller-provided parent.
        var options = optionsFactory.GetOptions(this);
        options.Parent = parentOptions;
        _options = options;
        _bufferGraph = bufferGraph;
        _classificationFormatMap = classificationFormatMap;
        _editorFormatMap = editorFormatMap;
        _textSnapshot = viewModel.EditBuffer.CurrentSnapshot;
        _anchorPosition = new SnapshotPoint(_textSnapshot, 0);
        _classifier = classifier;

        // Base layer order per VS: selection renders below the text, the caret above it;
        // adornment layers slot in above the text layer (before the caret layer).
        _selectionLayer = new SelectionLayer(this);
        _caretLayer = new CaretLayer(this);

        // The background is a layer child (not the panel's Background, which paints over
        // Bounds — the slot in logical coordinates, no longer lined up with the slot on
        // screen under zoom): like every layer it arranges to the logical viewport, which
        // the render transform maps exactly onto the slot. Its default transparent brush
        // also keeps the view hit-testable (a background-less panel is invisible to
        // hit-testing) even when the host never styles it.
        Children.Add(_backgroundLayer);
        Children.Add(_selectionLayer);
        Children.Add(_textLayer);
        Children.Add(_caretLayer);
        // No ClipToBounds here: the clip would apply in logical (pre-zoom) coordinates,
        // cutting the view short of its slot when zoomed out. The host wraps the view in
        // a clipping decorator that operates in screen coordinates.
        Focusable = true;

        _caret = new TextCaret(this, bufferGraph);
        _selection = new TextSelection(this);
        _viewScroller = new ViewScroller(this);
        _spaceReservationStack = new SpaceReservationStack(this);

        viewModel.EditBuffer.Changed += OnTextBufferChanged;
        if (!ReferenceEquals(viewModel.VisualBuffer, viewModel.EditBuffer))
        {
            // Elision changes (collapse/expand) reshape the visual buffer without an
            // edit-buffer change; they invalidate the layout through the same path.
            viewModel.VisualBuffer.Changed += OnVisualBufferChanged;
        }

        classificationFormatMap.ClassificationFormatMappingChanged += OnFormatMappingChanged;
        editorFormatMap.FormatMappingChanged += OnEditorFormatMappingChanged;
        _caretLayer.UpdateBrushes(editorFormatMap);
        options.OptionChanged += OnOptionChanged;
        ApplyZoomLevel(options.GetOptionValue(DefaultTextViewOptions.ZoomLevelId));

        TextInputMethodClientRequested += (_, e) =>
        {
            if (!_isClosed)
            {
                e.Client = _textInputMethodClient ??= new EditorTextInputMethodClient(this);
            }
        };

        GotFocus += (_, _) =>
        {
            if (_multiSelectionBroker is { ActivationTracksFocus: true } broker)
            {
                broker.AreSelectionsActive = true;
            }

            _caretLayer.OnViewUpdated();
            GotAggregateFocus?.Invoke(this, EventArgs.Empty);
        };
        LostFocus += (_, _) =>
        {
            if (_multiSelectionBroker is { ActivationTracksFocus: true } broker)
            {
                broker.AreSelectionsActive = false;
            }

            _caretLayer.OnViewUpdated();
            LostAggregateFocus?.Invoke(this, EventArgs.Empty);
        };
    }

    #region Events

    public event EventHandler<TextViewLayoutChangedEventArgs>? LayoutChanged;

    public event EventHandler? ViewportLeftChanged;

    public event EventHandler? ViewportHeightChanged;

    public event EventHandler? ViewportWidthChanged;

    public event EventHandler? Closed;

    public event EventHandler? LostAggregateFocus;

    public event EventHandler? GotAggregateFocus;

    public event EventHandler<BackgroundBrushChangedEventArgs>? BackgroundBrushChanged;

    public event EventHandler<ZoomLevelChangedEventArgs>? ZoomLevelChanged;

    // Hover per the contract: each handler declares its own delay through
    // [MouseHover(delay)] (default 150ms); when the pointer rests, handlers fire in
    // delay order as their time elapses, and any movement restarts the cycle.
    public event EventHandler<MouseHoverEventArgs> MouseHover
    {
        add
        {
            ArgumentNullException.ThrowIfNull(value);
            int delay = value.Method.GetCustomAttribute<MouseHoverAttribute>()?.Delay ?? DefaultHoverDelayMilliseconds;
            _mouseHoverHandlers.Add((value, TimeSpan.FromMilliseconds(delay)));
        }
        remove
        {
            int index = _mouseHoverHandlers.FindIndex(entry => entry.Handler.Equals(value));
            if (index >= 0)
            {
                _mouseHoverHandlers.RemoveAt(index);
            }
        }
    }

    private const int DefaultHoverDelayMilliseconds = 150;
    private readonly List<(EventHandler<MouseHoverEventArgs> Handler, TimeSpan Delay)> _mouseHoverHandlers = [];
    private DispatcherTimer? _hoverTimer;
    private (EventHandler<MouseHoverEventArgs> Handler, TimeSpan Delay)[]? _hoverCycle;
    private int _hoverCycleIndex;
    private TimeSpan _hoverElapsed;
    private Point _hoverPoint = new(double.NegativeInfinity, double.NegativeInfinity);

    private void RestartHoverCycle(Point point)
    {
        _hoverPoint = point;
        _hoverTimer?.Stop();
        _hoverElapsed = TimeSpan.Zero;
        _hoverCycleIndex = 0;
        _hoverCycle = _mouseHoverHandlers.Count == 0
            ? null
            : [.. _mouseHoverHandlers.OrderBy(static entry => entry.Delay)];
        if (_hoverCycle is not null)
        {
            ArmHoverTimer();
        }
    }

    private void CancelHoverCycle()
    {
        _hoverTimer?.Stop();
        _hoverCycle = null;
        _hoverPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
    }

    private void ArmHoverTimer()
    {
        if (_hoverTimer is null)
        {
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Tick += OnHoverTimerTick;
        }

        var due = _hoverCycle![_hoverCycleIndex].Delay - _hoverElapsed;
        _hoverTimer.Interval = due > TimeSpan.FromMilliseconds(1) ? due : TimeSpan.FromMilliseconds(1);
        _hoverTimer.Start();
    }

    private void OnHoverTimerTick(object? sender, EventArgs e)
    {
        _hoverTimer!.Stop();
        if (_isClosed || _hoverCycle is null || _hoverCycleIndex >= _hoverCycle.Length)
        {
            return;
        }

        if (GetHoverPositionFromViewPoint(_hoverPoint) is not { } position)
        {
            _hoverCycle = null;
            return;
        }

        _hoverElapsed = _hoverCycle[_hoverCycleIndex].Delay;
        var args = new MouseHoverEventArgs(
            this,
            position.Position,
            _bufferGraph.CreateMappingPoint(position, PointTrackingMode.Positive));
        while (_hoverCycleIndex < _hoverCycle.Length && _hoverCycle[_hoverCycleIndex].Delay <= _hoverElapsed)
        {
            _hoverCycle[_hoverCycleIndex].Handler(this, args);
            _hoverCycleIndex++;
        }

        if (_hoverCycle is not null && _hoverCycleIndex < _hoverCycle.Length)
        {
            ArmHoverTimer();
        }
    }

    #endregion

    #region Buffers and models

    public ITextBuffer TextBuffer => _viewModel.EditBuffer;

    public ITextSnapshot TextSnapshot => _textSnapshot;

    public ITextSnapshot VisualSnapshot => _viewModel.VisualBuffer.CurrentSnapshot;

    public ITextViewModel TextViewModel => _viewModel;

    public ITextDataModel TextDataModel => _viewModel.DataModel;

    public IBufferGraph BufferGraph => _bufferGraph;

    public ITextViewRoleSet Roles => _roles;

    public IEditorOptions Options => _options;

    public PropertyCollection Properties => _properties;

    #endregion

    #region View state

    public bool IsClosed => _isClosed;

    public bool InLayout => _inLayout;

    internal TextEditorFactoryService Factory => _factory;

    public ITextCaret Caret => _caret;

    public ITextSelection Selection => _selection;

    public IViewScroller ViewScroller => _viewScroller;

    public bool IsMouseOverViewOrAdornments => IsPointerOver || _spaceReservationStack.IsMouseOver;

    public bool HasAggregateFocus => IsFocused || _spaceReservationStack.HasAggregateFocus;

    public ITrackingSpan? ProvisionalTextHighlight
    {
        get => _provisionalTextHighlight;
        set => _provisionalTextHighlight = value;
    }

    #endregion

    #region ITextView2

    public bool InOuterLayout => _inLayout;

    public event EventHandler? MaxTextRightCoordinateChanged;

    public IMultiSelectionBroker MultiSelectionBroker
    {
        get
        {
            if (_multiSelectionBroker is null)
            {
                var broker = _factory.CreateMultiSelectionBroker(this);

                // Broker construction forces the initial layout (the selection transformer
                // captures its preferred x-coordinate from the view lines), and a
                // LayoutChanged handler that reads Caret.Position re-enters this getter.
                // Keep the reentrant instance — the caret and selection shims subscribed
                // to it — matching VS's GetOrCreateSingletonProperty semantics.
                if (_multiSelectionBroker is null)
                {
                    _multiSelectionBroker = broker;

                    // The layers redraw on selection changes; subscribing here (rather than
                    // in their Render methods) keeps broker creation out of the render pass.
                    _multiSelectionBroker.MultiSelectionSessionChanged += (_, _) =>
                    {
                        _selectionLayer.InvalidateVisual();
                        _caretLayer.OnViewUpdated();
                    };
                }
            }

            return _multiSelectionBroker;
        }
    }

    /// <summary>The broker if it has been created; render paths must not create it.</summary>
    internal IMultiSelectionBroker? ExistingBroker => _multiSelectionBroker;

    internal Microsoft.VisualStudio.Text.Operations.IEditorOperations EditorOperations
        => _editorOperations ??= _factory.GetEditorOperations(this);

    internal SelectionLayer SelectionLayerControl => _selectionLayer;

    internal CaretLayer CaretLayerControl => _caretLayer;

    public void QueuePostLayoutAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_inLayout)
        {
            _postLayoutActions.Enqueue(action);
        }
        else
        {
            action();
        }
    }

    public bool TryGetTextViewLines(out ITextViewLineCollection textViewLines)
    {
        if (!_inLayout && !_isClosed && _lineCollection is { } lines)
        {
            textViewLines = lines;
            return true;
        }

        textViewLines = null!;
        return false;
    }

    public bool TryGetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition, out ITextViewLine textViewLine)
    {
        if (!_inLayout && !_isClosed && _lineCollection is not null)
        {
            textViewLine = GetTextViewLineContainingBufferPosition(bufferPosition);
            return textViewLine is not null;
        }

        textViewLine = null!;
        return false;
    }

    public double LineHeight => EnsureLineSource().LineHeight;

    public double MaxTextRightCoordinate => _maxTextRightCoordinate;

    public double ViewportTop => 0.0;

    public double ViewportBottom => ViewportTop + ViewportHeight;

    public double ViewportRight => ViewportLeft + ViewportWidth;

    public double ViewportWidth => _viewportWidth;

    public double ViewportHeight => _viewportHeight;

    public double ViewportLeft
    {
        get => _viewportLeft;
        set
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            double maxLeft = (EnsureLineSource().WordWrapWidth > 0.0)
                ? 0.0
                : Math.Max(0.0, _maxTextRightCoordinate + EnsureLineSource().ColumnWidth - _viewportWidth);
            double newLeft = Math.Clamp(value, 0.0, maxLeft);
            if (newLeft != _viewportLeft)
            {
                _viewportLeft = newLeft;
                PositionLineVisuals();
                foreach (var layer in _adornmentLayers.Values)
                {
                    layer.OnLayoutChanged();
                }

                ViewportLeftChanged?.Invoke(this, EventArgs.Empty);
                _spaceReservationStack.QueueRefresh();
            }
        }
    }

    public new IBrush Background
    {
        get => _backgroundLayer.Background ?? Brushes.Transparent;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!Equals(_backgroundLayer.Background, value))
            {
                _backgroundLayer.Background = value;
                BackgroundBrushChanged?.Invoke(this, new BackgroundBrushChangedEventArgs(value));
            }
        }
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        // The option is the source of truth (the change handler applies the zoom), so
        // host-persisted zoom and direct assignment take the same path.
        set => _options.SetOptionValue(DefaultTextViewOptions.ZoomLevelId, value);
    }

    /// <summary>
    /// Zoom is a render transform over the view: geometry
    /// queries stay in the logical text-rendering coordinate system per the LineTransform
    /// contract, and no reformat happens on zoom). The viewport is the arranged size in
    /// logical units, so it shrinks as the zoom grows.
    /// </summary>
    private void ApplyZoomLevel(double level)
    {
        double newLevel = Math.Clamp(
            level,
            _options.GetOptionValue(DefaultTextViewOptions.MinZoomLevelId),
            _options.GetOptionValue(DefaultTextViewOptions.MaxZoomLevelId));
        if (newLevel == _zoomLevel || !_roles.Contains(PredefinedTextViewRoles.Zoomable))
        {
            return;
        }

        _zoomLevel = newLevel;
        double scale = newLevel / 100.0;
        RenderTransformOrigin = new RelativePoint(0.0, 0.0, RelativeUnit.Relative);
        RenderTransform = scale == 1.0 ? null : new ScaleTransform(scale, scale);

        // Unarranged views (explicit-viewport hosting) keep their viewport; arranged
        // views re-derive it from the slot at the new scale, and their children
        // re-arrange to the new logical size.
        if (Bounds.Width > 0.0 && Bounds.Height > 0.0)
        {
            SetViewportSize(Bounds.Width / scale, Bounds.Height / scale);
            InvalidateArrange();
        }

        ZoomLevelChanged?.Invoke(this, new ZoomLevelChangedEventArgs(newLevel));
        _spaceReservationStack.QueueRefresh();
    }

    public IFormattedLineSource FormattedLineSource => EnsureLineSource();

    public ILineTransformSource? LineTransformSource => _lineTransformSource;

    public Control VisualElement => this;

    #endregion

    #region Lines

    public IWpfTextViewLineCollection TextViewLines
    {
        get
        {
            if (_inLayout)
            {
                throw new InvalidOperationException("The view is in the middle of a layout.");
            }

            EnsureInitialLayout();
            return _lineCollection!;
        }
    }

    ITextViewLineCollection ITextView.TextViewLines => TextViewLines;

    public IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
    {
        ThrowIfClosed();
        ValidateBufferPosition(bufferPosition);
        EnsureInitialLayout();
        if (_lineCollection!.GetTextViewLineContainingBufferPosition(bufferPosition) is { } line)
        {
            return line;
        }

        // Not in the rendered collection: format it transiently (top coordinates unset).
        var source = EnsureLineSource();
        var snapshotLine = _viewModel
            .GetNearestPointInVisualSnapshot(bufferPosition, source.TopTextSnapshot, PointTrackingMode.Negative)
            .GetContainingLine();
        var rows = source.FormatLineInVisualBuffer(snapshotLine);
        foreach (var row in rows)
        {
            if (row.ContainsBufferPosition(bufferPosition))
            {
                return (IWpfTextViewLine)row;
            }
        }

        return (IWpfTextViewLine)rows[^1];
    }

    ITextViewLine ITextView.GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        => GetTextViewLineContainingBufferPosition(bufferPosition);

    public SnapshotSpan GetTextElementSpan(SnapshotPoint point)
    {
        ValidateBufferPosition(point);
        return GetTextViewLineContainingBufferPosition(point).GetTextElementSpan(point);
    }

    #endregion

    #region Layout

    public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo)
        => DisplayTextLineContainingBufferPosition(bufferPosition, verticalDistance, relativeTo, null, null);

    public void DisplayTextLineContainingBufferPosition(
        SnapshotPoint bufferPosition,
        double verticalDistance,
        ViewRelativePosition relativeTo,
        double? viewportWidthOverride,
        double? viewportHeightOverride)
    {
        ThrowIfClosed();
        if (relativeTo != ViewRelativePosition.Top && relativeTo != ViewRelativePosition.Bottom)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeTo));
        }

        ValidateBufferPosition(bufferPosition);

        if (viewportWidthOverride is { } width)
        {
            if (double.IsNaN(width))
            {
                throw new ArgumentOutOfRangeException(nameof(viewportWidthOverride));
            }

            _viewportWidth = width;
        }

        if (viewportHeightOverride is { } height)
        {
            if (double.IsNaN(height))
            {
                throw new ArgumentOutOfRangeException(nameof(viewportHeightOverride));
            }

            _viewportHeight = height;
        }

        PerformLayout(bufferPosition, verticalDistance, relativeTo);
    }

    private void PerformLayout(SnapshotPoint anchorPosition, double verticalDistance, ViewRelativePosition relativeTo)
    {
        if (_inLayout)
        {
            throw new InvalidOperationException("A layout is already in progress.");
        }

        // Provider creation may touch view state, so it happens before the layout proper
        // (and never from a render path).
        if (!_lineTransformSourceCreated)
        {
            _lineTransformSourceCreated = true;
            _lineTransformSource = _factory.CreateLineTransformSource(this);
        }

        var oldState = new ViewState(this);
        var oldCollection = _lineCollection;
        List<FormattedLine> lines;

        _inLayout = true;
        try
        {
            // The rendered snapshot is pinned for the whole layout.
            _textSnapshot = TextBuffer.CurrentSnapshot;
            anchorPosition = anchorPosition.TranslateTo(_textSnapshot, PointTrackingMode.Negative);
            var source = EnsureLineSource();

            // The line cache: rows of the published collection are reusable
            // while the line source is unchanged (the source is a pure function of
            // snapshot × format map × options × wrap width, so identity is the cache key).
            // Steady-state scrolling then translates lines instead of reformatting them.
            _reusedRows.Clear();
            _reusedOldTops.Clear();
            _reusableRows = oldCollection is not null && ReferenceEquals(source, _lineCollectionSource)
                ? oldCollection.Lines
                    .Where(row => !_classificationDamage.Any(damage => row.ExtentIncludingLineBreak.IntersectsWith(damage)))
                    .GroupBy(static row => row.ParagraphStart)
                    .ToDictionary(static group => group.Key, static group => group.ToList())
                : null;
            _classificationDamage.Clear();

            lines = BuildLines(source, anchorPosition, verticalDistance, relativeTo);

            double oldMaxTextRight = _maxTextRightCoordinate;
            var visibleArea = new Rect(_viewportLeft, ViewportTop, _viewportWidth, _viewportHeight);
            foreach (var line in lines)
            {
                line.SetVisibleArea(visibleArea);
                _maxTextRightCoordinate = Math.Max(_maxTextRightCoordinate, line.TextRight);
            }

            if (_maxTextRightCoordinate != oldMaxTextRight)
            {
                QueuePostLayoutAction(() => MaxTextRightCoordinateChanged?.Invoke(this, EventArgs.Empty));
            }

            _anchorPosition = lines[0].Start;
            _anchorDistance = lines[0].Top - ViewportTop;

            // Publish the new collection, then retire the old one (reused rows live on).
            _lineCollection = new WpfTextViewLineCollection(this, lines);
            _lineCollectionSource = source;
            oldCollection?.Invalidate();
            if (oldCollection is not null)
            {
                foreach (var oldLine in oldCollection.Lines)
                {
                    if (oldLine is FormattedLine formatted && _reusedRows.Contains(formatted))
                    {
                        continue;
                    }

                    oldLine.RemoveVisual();
                    oldLine.Dispose();
                }
            }

            _reusableRows = null;

            _textLayer.Children.Clear();
            foreach (var line in lines)
            {
                _textLayer.Children.Add((Control)line.GetOrCreateVisual());
            }

            PositionLineVisuals();
        }
        finally
        {
            _inLayout = false;
        }

        foreach (var layer in _adornmentLayers.Values)
        {
            layer.OnLayoutChanged();
        }

        // Classify per the contract: reused rows that moved are translations; only fresh
        // rows are new-or-reformatted.
        var newOrReformatted = new List<ITextViewLine>();
        var translated = new List<ITextViewLine>();
        foreach (var line in lines)
        {
            if (_reusedRows.Contains(line))
            {
                double oldTop = _reusedOldTops[line];
                if (line.Top == oldTop)
                {
                    line.SetChange(TextViewLineChange.None);
                    line.SetDeltaY(0.0);
                }
                else
                {
                    line.SetChange(TextViewLineChange.Translated);
                    line.SetDeltaY(line.Top - oldTop);
                    translated.Add(line);
                }
            }
            else
            {
                line.SetChange(TextViewLineChange.NewOrReformatted);
                newOrReformatted.Add(line);
            }
        }

        var newState = new ViewState(this);
        LayoutChanged?.Invoke(this, new TextViewLayoutChangedEventArgs(
            oldState,
            newState,
            newOrReformatted,
            translated));

        _selectionLayer.InvalidateVisual();
        _caretLayer.OnViewUpdated();

        // Popups reposition against the new layout (asynchronously, per the contract).
        _spaceReservationStack.QueueRefresh();

        while (_postLayoutActions.Count > 0)
        {
            _postLayoutActions.Dequeue()();
        }
    }

    /// <summary>
    /// Formats lines around the anchor until the viewport is filled, then shifts the result
    /// to prevent gaps at the buffer's start or end (the contract's repositioning rule).
    /// </summary>
    private List<FormattedLine> BuildLines(FormattedLineSource source, SnapshotPoint anchorPosition, double verticalDistance, ViewRelativePosition relativeTo)
    {
        var lines = new List<FormattedLine>();

        // The layout walks visual-buffer lines (under elision they join across collapsed
        // regions); the anchor arrives in edit coordinates and maps to the nearest
        // visible point.
        var visualSnapshot = source.TopTextSnapshot;
        var anchorLine = _viewModel
            .GetNearestPointInVisualSnapshot(anchorPosition, visualSnapshot, PointTrackingMode.Negative)
            .GetContainingLine();
        var anchorRows = FormatSnapshotLine(source, anchorLine);
        var anchorRow = anchorRows.FirstOrDefault(row => row.ContainsBufferPosition(anchorPosition)) ?? anchorRows[^1];

        // Transforms apply before any height is measured; the anchor's nominal position
        // is the best yPosition estimate available at this point.
        double anchorY = relativeTo == ViewRelativePosition.Top
            ? ViewportTop + verticalDistance
            : ViewportTop + _viewportHeight - verticalDistance;
        foreach (var row in anchorRows)
        {
            ApplyLineTransform(row, anchorY, relativeTo);
        }

        double anchorTop = relativeTo == ViewRelativePosition.Top
            ? ViewportTop + verticalDistance
            : ViewportTop + _viewportHeight - verticalDistance - anchorRow.Height;

        // Place the anchor snapshot line's rows.
        double top = anchorTop;
        for (int i = anchorRows.IndexOf(anchorRow) - 1; i >= 0; i--)
        {
            top -= anchorRows[i].Height;
        }

        foreach (var row in anchorRows)
        {
            row.SetTop(top);
            top += row.Height;
            lines.Add(row);
        }

        FillUpward(source, lines);
        FillDownward(source, lines);

        // Gap prevention (the repositioning rules): the view never scrolls above the
        // start of the buffer, and scrolling past the end clamps with the last line
        // still visible at the top of the viewport (the VS over-scroll limit).
        double shift = 0.0;
        var first = lines[0];
        var last = lines[^1];
        bool atBufferEnd = last.EndsAtEndOfVisualBuffer && last.IsLastTextViewLineForSnapshotLine;
        if (atBufferEnd && last.Top < ViewportTop)
        {
            shift = ViewportTop - last.Top;
        }

        if (shift != 0.0)
        {
            foreach (var line in lines)
            {
                line.SetTop(line.Top + shift);
            }

            FillUpward(source, lines);
            first = lines[0];
        }

        if (first.VisualRowStart == 0 && first.Top > ViewportTop)
        {
            double downShift = first.Top - ViewportTop;
            foreach (var line in lines)
            {
                line.SetTop(line.Top - downShift);
            }

            FillDownward(source, lines);
        }

        return lines;
    }

    private void FillUpward(FormattedLineSource source, List<FormattedLine> lines)
    {
        while (lines[0].Top > ViewportTop && lines[0].VisualRowStart > 0)
        {
            var previousLine = new SnapshotPoint(source.TopTextSnapshot, lines[0].VisualRowStart - 1).GetContainingLine();
            var rows = FormatSnapshotLine(source, previousLine);
            double top = lines[0].Top;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                ApplyLineTransform(rows[i], top, ViewRelativePosition.Bottom);
                top -= rows[i].Height;
                rows[i].SetTop(top);
            }

            lines.InsertRange(0, rows);
        }
    }

    private void FillDownward(FormattedLineSource source, List<FormattedLine> lines)
    {
        // End-of-buffer is flag-based, not a position comparison: a buffer ending in a
        // line break still has one final (empty) snapshot line to format at Length —
        // without it the end is never in the layout and the over-scroll clamp can't
        // engage.
        while (lines[^1].Bottom < ViewportBottom
            && !(lines[^1].EndsAtEndOfVisualBuffer && lines[^1].IsLastTextViewLineForSnapshotLine))
        {
            var nextLine = new SnapshotPoint(source.TopTextSnapshot, lines[^1].VisualEndIncludingLineBreak).GetContainingLine();
            var rows = FormatSnapshotLine(source, nextLine);
            double top = lines[^1].Bottom;
            foreach (var row in rows)
            {
                ApplyLineTransform(row, top, ViewRelativePosition.Top);
                row.SetTop(top);
                top += row.Height;
            }

            lines.AddRange(rows);
        }
    }

    private List<FormattedLine> FormatSnapshotLine(FormattedLineSource source, ITextSnapshotLine snapshotLine)
    {
        // Cache hit: the published collection already formatted this snapshot line with
        // the same source; reuse the rows (their old tops decide Translated vs. None).
        if (_reusableRows is not null && _reusableRows.Remove(snapshotLine.Start.Position, out var cached))
        {
            foreach (var row in cached)
            {
                _reusedRows.Add(row);
                _reusedOldTops[row] = row.Top;
            }

            return cached;
        }

        var rows = new List<FormattedLine>(source.FormatLineInVisualBuffer(snapshotLine).OfType<FormattedLine>());
        foreach (var row in rows)
        {
            // The default transform (driven by the row's space-negotiating adornments)
            // applies until the layout combines in the view's line transform source at
            // the row's placement, where the y-position is known.
            row.SetLineTransform(row.DefaultLineTransform);
        }

        return rows;
    }

    private void ApplyLineTransform(FormattedLine row, double yPosition, ViewRelativePosition placement)
    {
        var transform = row.DefaultLineTransform;
        if (_lineTransformSource is { } source)
        {
            transform = LineTransform.Combine(transform, source.GetLineTransform(row, yPosition, placement));
        }

        row.SetLineTransform(transform);
    }

    private FormattedLineSource EnsureLineSource()
    {
        bool wordWrap = (_options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0;
        double desiredWrapWidth = wordWrap ? Math.Max(_viewportWidth, 0.0) : 0.0;
        if (_sequencer is null)
        {
            _sequencer = _factory.CreateSequencer(this);
            _sequencer.SequenceChanged += (_, _) =>
            {
                InvalidateLineSource();
                QueueRelayout();
            };
        }

        if (_lineSource is null
            || _lineSource.SourceTextSnapshot != TextBuffer.CurrentSnapshot
            || _lineSource.TopTextSnapshot != _viewModel.VisualBuffer.CurrentSnapshot
            || _lineSourceWrapWidth != desiredWrapWidth)
        {
            _lineSourceWrapWidth = desiredWrapWidth;
            _lineSource = new FormattedLineSource(
                TextBuffer.CurrentSnapshot,
                _viewModel.VisualBuffer.CurrentSnapshot,
                _classifier,
                _classificationFormatMap,
                tabSize: _options.GetOptionValue(DefaultOptions.TabSizeOptionId),
                baseIndentation: 0.0,
                wordWrapWidth: desiredWrapWidth,
                maxAutoIndent: 0.0,
                useDisplayMode: true,
                bufferGraph: _bufferGraph,
                sequencer: _sequencer);
        }

        return _lineSource;
    }

    private void InvalidateLineSource()
    {
        _lineSource = null;
        _maxTextRightCoordinate = 0.0;
    }

    private void EnsureInitialLayout()
    {
        if (_lineCollection is null)
        {
            PerformLayout(new SnapshotPoint(TextBuffer.CurrentSnapshot, 0), 0.0, ViewRelativePosition.Top);
        }
    }

    private void QueueRelayout()
    {
        if (_isClosed || _layoutQueued)
        {
            return;
        }

        _layoutQueued = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _layoutQueued = false;
            if (!_isClosed)
            {
                RelayoutAtCurrentAnchor();
            }
        });
    }

    private void RelayoutAtCurrentAnchor()
        => PerformLayout(_anchorPosition, _anchorDistance, ViewRelativePosition.Top);

    private void PositionLineVisuals()
    {
        if (_lineCollection is null)
        {
            return;
        }

        foreach (var line in _lineCollection.Lines)
        {
            var visual = (Control)line.GetOrCreateVisual();
            Canvas.SetLeft(visual, line.Left - _viewportLeft);
            Canvas.SetTop(visual, line.Top - ViewportTop);
        }
    }

    #endregion

    #region Adornment layers and space reservation

    public IAdornmentLayer GetAdornmentLayer(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        ThrowIfClosed();
        if (!_adornmentLayers.TryGetValue(name, out var layer))
        {
            if (!_factory.IsAdornmentLayerDefined(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"No AdornmentLayerDefinition is exported for '{name}'.");
            }

            layer = new AdornmentLayer(this);
            _adornmentLayers[name] = layer;

            // Insert in definition order among the already created layers. Layers ordered
            // before "Text" render under the text (above the built-in selection layer);
            // the rest render above it, below the built-in caret layer.
            int rank = _factory.GetAdornmentLayerRank(name);
            bool belowText = rank < _factory.TextLayerRank;
            int windowStart = belowText ? Children.IndexOf(_selectionLayer) + 1 : Children.IndexOf(_textLayer) + 1;
            int windowEnd = belowText ? Children.IndexOf(_textLayer) : Children.IndexOf(_caretLayer);
            int insertAt = windowStart;
            for (int i = windowStart; i < windowEnd; i++)
            {
                if (Children[i] is AdornmentLayer existing
                    && _factory.GetAdornmentLayerRank(GetLayerName(existing)) <= rank)
                {
                    insertAt = i + 1;
                }
            }

            Children.Insert(insertAt, layer);
        }

        return layer;
    }

    private string GetLayerName(AdornmentLayer layer)
        => _adornmentLayers.First(pair => ReferenceEquals(pair.Value, layer)).Key;

    public ISpaceReservationManager GetSpaceReservationManager(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _spaceReservationStack.GetOrCreateManager(name);
    }

    public void QueueSpaceReservationStackRefresh()
    {
        if (!_isClosed)
        {
            _spaceReservationStack.QueueRefresh();
        }
    }

    #endregion

    #region Avalonia integration

    protected override Size ArrangeOverride(Size finalSize)
    {
        // The viewport is in logical (pre-zoom) units; the render transform maps it onto
        // the arranged slot. The children (the layers, which clip to their own bounds)
        // arrange to the logical viewport, not the slot: when zoomed out the logical
        // viewport is larger than the slot, and a slot-sized layer clip would cut the
        // rendered text short of the window.
        double scale = _zoomLevel / 100.0;
        var logicalBounds = new Rect(0.0, 0.0, finalSize.Width / scale, finalSize.Height / scale);
        foreach (var child in Children)
        {
            child.Arrange(logicalBounds);
        }

        if (!_isClosed)
        {
            SetViewportSize(logicalBounds.Width, logicalBounds.Height);
        }

        return finalSize;
    }


    private void SetViewportSize(double width, double height)
    {
        if (!_isClosed && (width != _viewportWidth || height != _viewportHeight))
        {
            bool widthChanged = width != _viewportWidth;
            bool heightChanged = height != _viewportHeight;
            _viewportWidth = width;
            _viewportHeight = height;
            bool wordWrap = (_options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) != 0;
            if (widthChanged && wordWrap)
            {
                InvalidateLineSource();
            }

            QueueRelayout();
            if (widthChanged)
            {
                ViewportWidthChanged?.Invoke(this, EventArgs.Empty);
            }

            if (heightChanged)
            {
                ViewportHeightChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        HandleMouseWheel(e);
    }

    /// <summary>
    /// The wheel gesture, also invoked by popup agents so scrolling keeps working while the
    /// pointer rests on an intellisense popup (quick info, signature help).
    /// </summary>
    internal void HandleMouseWheel(PointerWheelEventArgs e)
    {
        // Ctrl(Cmd)+wheel zooms in 10% steps per the VS gesture, when enabled and the
        // view is zoomable.
        if (!_isClosed
            && e.Delta.Y != 0
            && (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
            && _roles.Contains(PredefinedTextViewRoles.Zoomable)
            && _options.GetOptionValue(DefaultTextViewOptions.EnableMouseWheelZoomId))
        {
            ZoomLevel = e.Delta.Y > 0
                ? _zoomLevel * ZoomConstants.ScalingFactor
                : _zoomLevel / ZoomConstants.ScalingFactor;
            e.Handled = true;
            return;
        }

        if (!_isClosed && e.Delta.Y != 0)
        {
            _viewScroller.ScrollViewportVerticallyByPixels(e.Delta.Y * 3.0 * LineHeight);
            e.Handled = true;
        }

        if (!_isClosed && e.Delta.X != 0)
        {
            _viewScroller.ScrollViewportHorizontallyByPixels(-e.Delta.X * 3.0 * LineHeight);
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AvaloniaClipboardBridge.Instance.Attach(TopLevel.GetTopLevel(this));
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (!_isClosed && !e.Handled && !string.IsNullOrEmpty(e.Text) && !char.IsControl(e.Text[0]))
        {
            if (!Options.DoesViewProhibitUserInput())
            {
                EditorOperations.InsertText(e.Text);
            }

            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!_isClosed && !e.Handled)
        {
            e.Handled = HandleKey(e.Key, e.KeyModifiers);
        }
    }

    // Interim keymap: keystrokes map straight onto IEditorOperations (the VS-semantics
    // surface the scripted tests target). The Modern Commanding chain replaces this as the
    // dispatch mechanism when command handlers are wired.
    private bool HandleKey(Key key, KeyModifiers modifiers)
    {
        // Modifier roles come from the platform (macOS: Cmd for commands, Option for word
        // actions; elsewhere Ctrl for both); the fallback covers views without a platform.
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        bool extend = modifiers.HasFlag(KeyModifiers.Shift);
        bool word = hotkeys is null
            ? modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Alt)
            : modifiers.HasFlag(hotkeys.WholeWordTextActionModifiers);
        bool command = hotkeys is null
            ? modifiers.HasFlag(KeyModifiers.Control) || modifiers.HasFlag(KeyModifiers.Meta)
            : modifiers.HasFlag(hotkeys.CommandModifiers);
        var operations = EditorOperations;

        // Read-only views swallow editing keystrokes; navigation, selection and copy
        // still work.
        if (Options.DoesViewProhibitUserInput() && IsEditingKey(key, command))
        {
            return true;
        }

        switch (key)
        {
            case Key.Left when word:
                operations.MoveToPreviousWord(extend);
                return true;
            case Key.Left:
                operations.MoveToPreviousCharacter(extend);
                return true;
            case Key.Right when word:
                operations.MoveToNextWord(extend);
                return true;
            case Key.Right:
                operations.MoveToNextCharacter(extend);
                return true;
            case Key.Up when command && modifiers.HasFlag(KeyModifiers.Alt):
                AddCaretOnAdjacentLine(above: true);
                return true;
            case Key.Down when command && modifiers.HasFlag(KeyModifiers.Alt):
                AddCaretOnAdjacentLine(above: false);
                return true;
            case Key.Up:
                operations.MoveLineUp(extend);
                return true;
            case Key.Down:
                operations.MoveLineDown(extend);
                return true;
            case Key.Home when command:
                operations.MoveToStartOfDocument(extend);
                return true;
            case Key.Home:
                operations.MoveToHome(extend);
                return true;
            case Key.End when command:
                operations.MoveToEndOfDocument(extend);
                return true;
            case Key.End:
                operations.MoveToEndOfLine(extend);
                return true;
            case Key.PageUp:
                operations.PageUp(extend);
                return true;
            case Key.PageDown:
                operations.PageDown(extend);
                return true;
            case Key.Back when word:
                return operations.DeleteWordToLeft();
            case Key.Back:
                return operations.Backspace();
            case Key.Delete when word:
                return operations.DeleteWordToRight();
            case Key.Delete:
                return operations.Delete();
            case Key.Enter:
                return operations.InsertNewLine();
            case Key.Tab when extend:
                return operations.Unindent();
            case Key.Tab:
                return operations.Indent();
            case Key.Escape:
                MultiSelectionBroker.ClearSecondarySelections();
                _selection.Clear();
                return true;
            case Key.A when command:
                operations.SelectAll();
                return true;
            case Key.C when command:
                return operations.CopySelection();
            case Key.X when command:
                return operations.CutSelection();
            case Key.V when command:
                return operations.Paste();
            case Key.Z when command && extend:
            case Key.Y when command:
                return TryUndoRedo(undo: false);
            case Key.Z when command:
                return TryUndoRedo(undo: true);
            default:
                return false;
        }
    }

    private static bool IsEditingKey(Key key, bool command) => key switch
    {
        Key.Back or Key.Delete or Key.Enter or Key.Tab => true,
        Key.X or Key.V or Key.Z or Key.Y when command => true,
        _ => false,
    };

    /// <summary>
    /// Ctrl/Cmd+Alt+Up/Down: a caret on the neighboring line for every current selection
    /// (VS Code's "Add Cursor Above/Below"; the broker merges duplicates, so repeated
    /// presses grow the stack one line at a time).
    /// </summary>
    private void AddCaretOnAdjacentLine(bool above)
    {
        var broker = MultiSelectionBroker;
        var transformation = above
            ? PredefinedSelectionTransformations.MoveToPreviousLine
            : PredefinedSelectionTransformations.MoveToNextLine;
        var added = new List<Microsoft.VisualStudio.Text.Selection>();
        foreach (var selection in broker.AllSelections)
        {
            var moved = broker.TransformSelection(selection, transformation);
            if (moved != selection)
            {
                added.Add(moved);
            }
        }

        if (added.Count > 0)
        {
            broker.AddSelectionRange(added);
        }
    }

    private bool TryUndoRedo(bool undo)
    {
        var history = _factory.GetUndoManager(TextBuffer).TextBufferUndoHistory;
        if (undo && history.CanUndo)
        {
            history.Undo(1);
            return true;
        }

        if (!undo && history.CanRedo)
        {
            history.Redo(1);
            return true;
        }

        return false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_isClosed || e.Handled)
        {
            return;
        }

        Focus();
        var point = e.GetPosition(this);

        // Interim mouse handling: click moves the caret, shift-click and drag extend,
        // alt/cmd-click adds a caret, alt+shift-click/drag makes a box (column)
        // selection. The IMouseProcessor chain per §5.6 is M3+ work.
        bool box = e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (GetBufferPositionFromViewPoint(point, allowVirtualSpace: box) is { } position)
        {
            bool addCaret = e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            bool extend = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (box)
            {
                // The box anchors at the press point (VS Code's Option+Shift+drag); dragging
                // extends it into one selection per line.
                MultiSelectionBroker.SetBoxSelection(new Microsoft.VisualStudio.Text.Selection(position));
            }
            else if (addCaret)
            {
                MultiSelectionBroker.AddSelection(new Microsoft.VisualStudio.Text.Selection(position));
            }
            else if (extend)
            {
                _selection.Select(_selection.AnchorPoint, position);
            }
            else if (e.ClickCount == 2)
            {
                // Double-click selects the word under the click; no capture, so the
                // press-release of the second click can't collapse the selection
                // (word-by-word drag extension is mouse-processor work, §5.6).
                MultiSelectionBroker.SetSelection(new Microsoft.VisualStudio.Text.Selection(position));
                EditorOperations.SelectCurrentWord();
                e.Handled = true;
                return;
            }
            else
            {
                // A plain click collapses the selection to the click point.
                MultiSelectionBroker.SetSelection(new Microsoft.VisualStudio.Text.Selection(position));
            }

            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isClosed)
        {
            return;
        }

        if (ReferenceEquals(e.Pointer.Captured, this)
            && GetBufferPositionFromViewPoint(e.GetPosition(this), MultiSelectionBroker.IsBoxSelection) is { } position)
        {
            if (MultiSelectionBroker.IsBoxSelection)
            {
                MultiSelectionBroker.SetBoxSelection(
                    new Microsoft.VisualStudio.Text.Selection(MultiSelectionBroker.BoxSelection.AnchorPoint, position));
            }
            else
            {
                MultiSelectionBroker.SetSelection(new Microsoft.VisualStudio.Text.Selection(_selection.AnchorPoint, position));
            }

            return;
        }

        if (e.Pointer.Captured is null)
        {
            // A resting pointer keeps reporting moves within a couple of pixels; only
            // real movement restarts the hover cycle.
            var point = e.GetPosition(this);
            if (Math.Abs(point.X - _hoverPoint.X) > 2.0 || Math.Abs(point.Y - _hoverPoint.Y) > 2.0)
            {
                RestartHoverCycle(point);
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        CancelHoverCycle();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }
    }

    /// <summary>
    /// Strict hit test for hover. Unlike clicks — which clamp to the nearest line and land at
    /// the line end past the text — MouseHover must fire only when the pointer is actually
    /// over a character: its consumers (e.g. the quick info controller) use the position
    /// unfiltered, so a clamped position would show info for the last line's closing brace
    /// when the pointer rests in the empty space below the document.
    /// </summary>
    private SnapshotPoint? GetHoverPositionFromViewPoint(Point point)
    {
        if (_lineCollection is null)
        {
            return null;
        }

        double y = point.Y + ViewportTop;
        if (y < _lineCollection[0].Top || y >= _lineCollection[^1].Bottom)
        {
            return null;
        }

        return _lineCollection.GetTextViewLineContainingYCoordinate(y) is { } line
            ? line.GetBufferPositionFromXCoordinate(point.X + _viewportLeft, textOnly: true)
            : null;
    }

    private VirtualSnapshotPoint? GetBufferPositionFromViewPoint(Point point, bool allowVirtualSpace = false)
    {
        if (_lineCollection is null)
        {
            return null;
        }

        double y = Math.Clamp(point.Y + ViewportTop, _lineCollection[0].Top, _lineCollection[^1].Bottom - 0.01);
        if (_lineCollection.GetTextViewLineContainingYCoordinate(y) is { } line)
        {
            var position = line.GetInsertionBufferPositionFromXCoordinate(point.X + _viewportLeft);

            // Virtual space is opt-in (UseVirtualSpace, default off): with it off, a
            // click past the end of the line lands at the end of the line. Box selection
            // always gets virtual space (VS behavior) so the box keeps its column across
            // lines shorter than the anchor.
            return allowVirtualSpace || _options.IsVirtualSpaceEnabled()
                ? position
                : new VirtualSnapshotPoint(position.Position);
        }

        return null;
    }

    #endregion

    #region Change handling

    private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        InvalidateLineSource();
        if (_inLayout || _lineCollection is null)
        {
            QueueRelayout();
        }
        else
        {
            // Edits relayout synchronously: the published line collection must never lag
            // the buffer (callers query caret/selection geometry right after an edit).
            RelayoutAtCurrentAnchor();
        }
    }

    private void OnVisualBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        // An edit-buffer change propagates into the visual buffer and already arrives
        // through OnTextBufferChanged; this handler covers projection-only changes
        // (elision collapse/expand), which relayout the same way edits do.
        if (TextBuffer.CurrentSnapshot == _textSnapshot)
        {
            OnTextBufferChanged(sender, e);
        }
    }

    private void OnFormatMappingChanged(object? sender, EventArgs e)
    {
        InvalidateLineSource();
        QueueRelayout();
    }

    private void OnEditorFormatMappingChanged(object? sender, FormatItemsEventArgs e) =>
        _caretLayer.UpdateBrushes(_editorFormatMap);

    private void OnOptionChanged(object? sender, EditorOptionChangedEventArgs e)
    {
        if (e.OptionId == DefaultTextViewOptions.ZoomLevelName)
        {
            // Zoom is a render transform: no reformat, the viewport change relayouts.
            ApplyZoomLevel(_options.GetOptionValue(DefaultTextViewOptions.ZoomLevelId));
            return;
        }

        InvalidateLineSource();
        QueueRelayout();
    }

    private void OnClassificationChanged(object? sender, ClassificationChangedEventArgs e)
    {
        if (_lineCollection is { } collection)
        {
            var changeSpan = e.ChangeSpan.TranslateTo(_textSnapshot, SpanTrackingMode.EdgeInclusive);
            if (changeSpan.IntersectsWith(collection.FormattedSpan))
            {
                _classificationDamage.Add(changeSpan);
                QueueRelayout();
            }
        }
    }

    #endregion

    public void Close()
    {
        if (_isClosed)
        {
            throw new InvalidOperationException("The view is already closed.");
        }

        _isClosed = true;
        CancelHoverCycle();
        _spaceReservationStack.Close();
        _viewModel.EditBuffer.Changed -= OnTextBufferChanged;
        if (!ReferenceEquals(_viewModel.VisualBuffer, _viewModel.EditBuffer))
        {
            _viewModel.VisualBuffer.Changed -= OnVisualBufferChanged;
        }
        _classificationFormatMap.ClassificationFormatMappingChanged -= OnFormatMappingChanged;
        _editorFormatMap.FormatMappingChanged -= OnEditorFormatMappingChanged;
        _options.OptionChanged -= OnOptionChanged;
        if (_classifier is { } classifier)
        {
            classifier.ClassificationChanged -= OnClassificationChanged;
        }

        if (_lineCollection is { } lines)
        {
            lines.Invalidate();
            foreach (var line in lines.Lines)
            {
                line.RemoveVisual();
                line.Dispose();
            }

            _lineCollection = null;
        }

        _viewModel.Dispose();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Attaches the view's classifier. The classifier aggregator requires a constructed view,
    /// so the factory calls this immediately after construction, before any layout.
    /// </summary>
    internal void SetClassifier(IClassifier? classifier)
    {
        _classifier = classifier;
        InvalidateLineSource();
        if (classifier is not null)
        {
            classifier.ClassificationChanged += OnClassificationChanged;
        }
    }

    private void ThrowIfClosed()
    {
        if (_isClosed)
        {
            throw new InvalidOperationException("The view is closed.");
        }
    }

    private void ValidateBufferPosition(SnapshotPoint bufferPosition)
    {
        if (bufferPosition.Snapshot is null || bufferPosition.Snapshot.TextBuffer != TextBuffer)
        {
            throw new ArgumentException("The position belongs to a different buffer.");
        }
    }
}
