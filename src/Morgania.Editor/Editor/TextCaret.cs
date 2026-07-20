#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;

/// <summary>
/// The legacy caret contract as a shim over the primary selection of the view's
/// <see cref="IMultiSelectionBroker"/> (the modern VS design: the broker owns caret and
/// selection state; this surface adapts it).
/// </summary>
internal sealed class TextCaret : ITextCaret
{
    private readonly WpfTextView _view;
    private readonly IBufferGraph _bufferGraph;
    private bool _isHidden;
    private bool _subscribed;
    private VirtualSnapshotPoint _lastPosition;
    private PositionAffinity _lastAffinity = PositionAffinity.Successor;

    public TextCaret(WpfTextView view, IBufferGraph bufferGraph, bool isHidden)
    {
        _view = view;
        _bufferGraph = bufferGraph;
        // The initial state goes to the field: the IsHidden setter notifies the caret
        // layer, which reads view state that is not built yet mid-construction.
        _isHidden = isHidden;
        _lastPosition = new VirtualSnapshotPoint(new SnapshotPoint(view.TextSnapshot, 0));
    }

    public event EventHandler<CaretPositionChangedEventArgs>? PositionChanged;

    private IMultiSelectionBroker Broker
    {
        get
        {
            var broker = _view.MultiSelectionBroker;
            if (!_subscribed)
            {
                _subscribed = true;
                _lastPosition = broker.PrimarySelection.InsertionPoint;
                broker.MultiSelectionSessionChanged += OnSessionChanged;
            }

            return broker;
        }
    }

    public CaretPosition Position => MakePosition(Broker.PrimarySelection);

    public ITextViewLine ContainingTextViewLine => _view.GetTextViewLineContainingBufferPosition(Position.BufferPosition);

    public double Left => GetBounds().Left;

    public double Width => 2.0;

    public double Right => Left + Width;

    public double Top => GetBounds().Top;

    public double Height => GetBounds().Height;

    public double Bottom => GetBounds().Bottom;

    public bool OverwriteMode
        => Broker.TryGetSelectionPresentationProperties(Broker.PrimarySelection, out var properties) && properties.IsOverwriteMode;

    public bool InVirtualSpace => Broker.PrimarySelection.InsertionPoint.IsInVirtualSpace;

    public bool IsHidden
    {
        get => _isHidden;
        set
        {
            if (_isHidden != value)
            {
                _isHidden = value;
                _view.CaretLayerControl.OnViewUpdated();
            }
        }
    }

    public void EnsureVisible() => Broker.TryEnsureVisible(Broker.PrimarySelection, EnsureSpanVisibleOptions.MinimumScroll);

    public CaretPosition MoveTo(ITextViewLine textLine)
    {
        ArgumentNullException.ThrowIfNull(textLine);
        return MoveTo(new VirtualSnapshotPoint(textLine.Start), PositionAffinity.Successor, true);
    }

    public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate)
        => MoveTo(textLine, xCoordinate, true);

    public CaretPosition MoveTo(ITextViewLine textLine, double xCoordinate, bool captureHorizontalPosition)
    {
        ArgumentNullException.ThrowIfNull(textLine);
        return MoveTo(textLine.GetInsertionBufferPositionFromXCoordinate(xCoordinate), PositionAffinity.Successor, captureHorizontalPosition);
    }

    public CaretPosition MoveTo(SnapshotPoint bufferPosition) => MoveTo(new VirtualSnapshotPoint(bufferPosition));

    public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        => MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity);

    public CaretPosition MoveTo(SnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
        => MoveTo(new VirtualSnapshotPoint(bufferPosition), caretAffinity, captureHorizontalPosition);

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition) => MoveTo(bufferPosition, PositionAffinity.Successor);

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity)
        => MoveTo(bufferPosition, caretAffinity, true);

    public CaretPosition MoveTo(VirtualSnapshotPoint bufferPosition, PositionAffinity caretAffinity, bool captureHorizontalPosition)
    {
        _lastAffinity = caretAffinity;

        // Legacy caret semantics: MoveTo only moves the insertion point; the selection's
        // anchor and active points are untouched (callers pair Selection.Select with
        // Caret.MoveTo). An empty selection travels with the caret, and a box selection
        // keeps its shape (breaking the box is Selection.Mode's job, not the caret's).
        if (Broker.IsBoxSelection)
        {
            var box = Broker.BoxSelection;
            Broker.SetBoxSelection(new Selection(bufferPosition, box.AnchorPoint, box.ActivePoint, caretAffinity));
            return Position;
        }

        var primary = Broker.PrimarySelection;
        var newSelection = primary.IsEmpty
            ? new Selection(bufferPosition, caretAffinity)
            : new Selection(bufferPosition, primary.AnchorPoint, primary.ActivePoint, caretAffinity);
        Broker.SetSelection(newSelection);
        return Position;
    }

    public CaretPosition MoveToNextCaretPosition()
    {
        Broker.TryPerformActionOnSelection(Broker.PrimarySelection, PredefinedSelectionTransformations.MoveToNextCaretPosition, out _);
        return Position;
    }

    public CaretPosition MoveToPreviousCaretPosition()
    {
        Broker.TryPerformActionOnSelection(Broker.PrimarySelection, PredefinedSelectionTransformations.MoveToPreviousCaretPosition, out _);
        return Position;
    }

    public CaretPosition MoveToPreferredCoordinates()
    {
        // The broker tracks preferred coordinates internally for line up/down transforms;
        // moving to them explicitly is expressed as a zero-line move.
        if (Broker.TryGetSelectionPresentationProperties(Broker.PrimarySelection, out var properties)
            && _view.TextViewLines.GetTextViewLineContainingYCoordinate(properties.PreferredYCoordinate) is { } line)
        {
            return MoveTo(line, properties.PreferredXCoordinate);
        }

        return Position;
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        var newPosition = Broker.PrimarySelection.InsertionPoint;
        if (newPosition != _lastPosition.TranslateTo(newPosition.Position.Snapshot))
        {
            var oldCaretPosition = MakePosition(_lastPosition, _lastAffinity);
            _lastPosition = newPosition;
            PositionChanged?.Invoke(this, new CaretPositionChangedEventArgs(_view, oldCaretPosition, Position));
        }
        else
        {
            _lastPosition = newPosition;
        }
    }

    private CaretPosition MakePosition(Selection selection)
        => MakePosition(selection.InsertionPoint, selection.InsertionPointAffinity);

    private CaretPosition MakePosition(VirtualSnapshotPoint point, PositionAffinity affinity)
        => new(
            point.TranslateTo(_view.TextSnapshot),
            _bufferGraph.CreateMappingPoint(point.Position, PointTrackingMode.Positive),
            affinity);

    private TextBounds GetBounds()
    {
        if (Broker.TryGetSelectionPresentationProperties(Broker.PrimarySelection, out var properties)
            && properties.TryGetContainingTextViewLine(out _))
        {
            return properties.CaretBounds;
        }

        var line = _view.GetTextViewLineContainingBufferPosition(Position.BufferPosition);
        return line.GetCharacterBounds(Position.VirtualBufferPosition);
    }
}
