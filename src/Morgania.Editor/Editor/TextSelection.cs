#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

/// <summary>
/// The legacy selection contract as a shim over the view's <see cref="IMultiSelectionBroker"/>
/// (the modern VS design: the broker owns selection state; this surface adapts it).
/// </summary>
internal sealed class TextSelection : ITextSelection
{
    private readonly WpfTextView _view;
    private bool _subscribed;
    private Selection _lastPrimary;

    public TextSelection(WpfTextView view)
    {
        _view = view;
    }

    public event EventHandler? SelectionChanged;

    private IMultiSelectionBroker Broker
    {
        get
        {
            var broker = _view.MultiSelectionBroker;
            if (!_subscribed)
            {
                _subscribed = true;
                _lastPrimary = broker.PrimarySelection;
                broker.MultiSelectionSessionChanged += OnSessionChanged;
            }

            return broker;
        }
    }

    public ITextView TextView => _view;

    // In box mode the legacy surface answers the box's corners, not the primary (per-line)
    // selection: EditorOperations re-selects from AnchorPoint/ActivePoint after box edits,
    // which must reproduce the whole box.
    private Selection Shape => Broker.IsBoxSelection ? Broker.BoxSelection : Broker.PrimarySelection;

    public VirtualSnapshotPoint AnchorPoint => Shape.AnchorPoint;

    public VirtualSnapshotPoint ActivePoint => Shape.ActivePoint;

    public VirtualSnapshotPoint Start => Shape.Start;

    public VirtualSnapshotPoint End => Shape.End;

    public bool IsReversed => Shape.IsReversed;

    public bool IsEmpty => Broker.SelectedSpans.All(static span => span.IsEmpty)
        && Broker.VirtualSelectedSpans.All(static span => span.IsEmpty);

    public bool IsActive
    {
        get => Broker.AreSelectionsActive;
        set => Broker.AreSelectionsActive = value;
    }

    public bool ActivationTracksFocus
    {
        get => Broker.ActivationTracksFocus;
        set => Broker.ActivationTracksFocus = value;
    }

    public TextSelectionMode Mode
    {
        get => Broker.IsBoxSelection ? TextSelectionMode.Box : TextSelectionMode.Stream;
        set
        {
            if (value == TextSelectionMode.Box && !Broker.IsBoxSelection)
            {
                Broker.SetBoxSelection(Broker.PrimarySelection);
            }
            else if (value == TextSelectionMode.Stream && Broker.IsBoxSelection)
            {
                Broker.BreakBoxSelection();
            }
        }
    }

    public VirtualSnapshotSpan StreamSelectionSpan => Broker.SelectionExtent;

    public NormalizedSnapshotSpanCollection SelectedSpans => Broker.SelectedSpans;

    public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans
        => new([.. Broker.VirtualSelectedSpans]);

    public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (IsEmpty)
        {
            return null;
        }

        var lineStart = new VirtualSnapshotPoint(line.Start);
        var lineEnd = new VirtualSnapshotPoint(line.EndIncludingLineBreak);
        foreach (var span in Broker.VirtualSelectedSpans)
        {
            if (span.IsEmpty || span.End <= lineStart || span.Start >= lineEnd)
            {
                continue;
            }

            var start = span.Start > lineStart ? span.Start : lineStart;
            var end = span.End < lineEnd ? span.End : lineEnd;
            return new VirtualSnapshotSpan(start, end);
        }

        return null;
    }

    public void Select(SnapshotSpan selectionSpan, bool isReversed)
        => Select(new Selection(selectionSpan, isReversed));

    public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        => Select(new Selection(anchorPoint, activePoint));

    private void Select(Selection selection)
    {
        // Select respects the current Mode: while a box selection is active, re-selecting
        // reshapes the box instead of replacing it with a stream selection. EditorOperations
        // relies on this after editing a box (e.g. typing over it) to keep a caret per line.
        if (Broker.IsBoxSelection)
        {
            Broker.SetBoxSelection(selection);
        }
        else
        {
            Broker.SetSelection(selection);
        }
    }

    public void Clear()
    {
        Broker.ClearSecondarySelections();
        var insertion = Broker.PrimarySelection.InsertionPoint;
        Broker.SetSelection(new Selection(insertion, Broker.PrimarySelection.InsertionPointAffinity));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        var primary = Broker.PrimarySelection;
        if (primary != _lastPrimary)
        {
            _lastPrimary = primary;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
