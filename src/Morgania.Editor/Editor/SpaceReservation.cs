#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Formatting;

/// <summary>
/// The view's space reservation stack: named managers (declared by
/// <see cref="SpaceReservationManagerDefinition"/> exports, ordered by [Order]) whose agents
/// place popup content over the view — the positioning machinery under the IntelliSense
/// presenters. A refresh walks the managers in definition order; each agent positions its
/// popup avoiding the geometry the agents before it reserved.
/// </summary>
internal sealed class SpaceReservationStack
{
    private readonly WpfTextView _view;
    private readonly Dictionary<string, SpaceReservationManager> _managers = new(StringComparer.OrdinalIgnoreCase);
    private bool _refreshQueued;

    public SpaceReservationStack(WpfTextView view)
    {
        _view = view;
    }

    public bool IsMouseOver
    {
        get
        {
            foreach (var manager in _managers.Values)
            {
                if (manager.IsMouseOver)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool HasAggregateFocus
    {
        get
        {
            foreach (var manager in _managers.Values)
            {
                if (manager.HasAggregateFocus)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public ISpaceReservationManager GetOrCreateManager(string name)
    {
        if (!_managers.TryGetValue(name, out var manager))
        {
            if (!_view.Factory.IsSpaceReservationManagerDefined(name))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"No SpaceReservationManagerDefinition is exported for '{name}'.");
            }

            manager = new SpaceReservationManager(_view, name, _view.Factory.GetSpaceReservationManagerRank(name));
            _managers[name] = manager;
        }

        return manager;
    }

    /// <summary>Refreshes asynchronously, per the ITextView contract; multiple queued requests coalesce.</summary>
    public void QueueRefresh()
    {
        if (!_refreshQueued && _managers.Count > 0)
        {
            _refreshQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _refreshQueued = false;
                Refresh();
            });
        }
    }

    public void Refresh()
    {
        if (_view.IsClosed)
        {
            return;
        }

        // Definition order decides reservation priority: each manager positions its agents
        // around everything the earlier managers reserved.
        var reserved = new GeometryGroup();
        foreach (var manager in _managers.Values.OrderBy(static m => m.Rank))
        {
            manager.PositionAndDisplay(reserved);
        }
    }

    public void Close()
    {
        foreach (var manager in _managers.Values)
        {
            manager.RemoveAllAgents();
        }
    }
}

internal sealed class SpaceReservationManager : ISpaceReservationManager
{
    private readonly WpfTextView _view;
    private readonly List<ISpaceReservationAgent> _agents = [];

    public SpaceReservationManager(WpfTextView view, string name, int rank)
    {
        _view = view;
        Name = name;
        Rank = rank;
    }

    public string Name { get; }

    public int Rank { get; }

    public ReadOnlyCollection<ISpaceReservationAgent> Agents => new(_agents);

    public event EventHandler<SpaceReservationAgentChangedEventArgs>? AgentChanged;

    public event EventHandler? LostAggregateFocus;

    public event EventHandler? GotAggregateFocus;

    public bool IsMouseOver
    {
        get
        {
            foreach (var agent in _agents)
            {
                if (agent.IsMouseOver)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool HasAggregateFocus
    {
        get
        {
            foreach (var agent in _agents)
            {
                if (agent.HasFocus)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public ISpaceReservationAgent CreatePopupAgent(ITrackingSpan visualSpan, PopupStyles style, Control content)
    {
        ArgumentNullException.ThrowIfNull(visualSpan);
        ArgumentNullException.ThrowIfNull(content);
        return new PopupAgent(_view, visualSpan, style, content);
    }

    public void UpdatePopupAgent(ISpaceReservationAgent agent, ITrackingSpan visualSpan, PopupStyles styles)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(visualSpan);
        if (agent is PopupAgent popup && _agents.Contains(agent))
        {
            popup.Update(visualSpan, styles);
            _view.QueueSpaceReservationStackRefresh();
        }
    }

    public void AddAgent(ISpaceReservationAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agents.Add(agent);
        agent.GotFocus += OnAgentGotFocus;
        agent.LostFocus += OnAgentLostFocus;
        AgentChanged?.Invoke(this, new SpaceReservationAgentChangedEventArgs(oldAgent: null!, newAgent: agent));
        _view.QueueSpaceReservationStackRefresh();
    }

    public bool RemoveAgent(ISpaceReservationAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        if (!_agents.Remove(agent))
        {
            return false;
        }

        agent.GotFocus -= OnAgentGotFocus;
        agent.LostFocus -= OnAgentLostFocus;
        agent.Hide();
        (agent as PopupAgent)?.OnRemoved();
        AgentChanged?.Invoke(this, new SpaceReservationAgentChangedEventArgs(oldAgent: agent, newAgent: null!));
        // Removing a focused popup (its content leaves the tree) can end the view's
        // aggregate focus without any event reaching the unsubscribed agent handlers.
        _view.CheckAggregateFocus();
        return true;
    }

    internal void RemoveAllAgents()
    {
        foreach (var agent in _agents.ToArray())
        {
            RemoveAgent(agent);
        }
    }

    public void PositionAndDisplay(GeometryGroup reservedGeometry)
    {
        foreach (var agent in _agents.ToArray())
        {
            var geometry = agent.PositionAndDisplay(reservedGeometry);
            if (geometry is null)
            {
                RemoveAgent(agent);
            }
            else if (geometry.Bounds is { Width: > 0.0, Height: > 0.0 })
            {
                reservedGeometry.Children.Add(geometry);
            }
        }
    }

    private void OnAgentGotFocus(object? sender, EventArgs e)
    {
        GotAggregateFocus?.Invoke(this, EventArgs.Empty);
        _view.CheckAggregateFocus();
    }

    private void OnAgentLostFocus(object? sender, EventArgs e)
    {
        LostAggregateFocus?.Invoke(this, EventArgs.Empty);
        _view.CheckAggregateFocus();
    }
}

/// <summary>
/// The default popup agent: displays its content in the top-level's overlay layer, positioned
/// against the visual span's rendered geometry per <see cref="PopupStyles"/>, never scaling
/// with the view's zoom (the overlay lives outside the view's render transform).
/// </summary>
internal sealed class PopupAgent : ISpaceReservationAgent
{
    private readonly WpfTextView _view;
    private ITrackingSpan _visualSpan;
    private PopupStyles _style;
    private Canvas? _overlay;
    private bool _dismissed;
    private bool _tracking;

    public PopupAgent(WpfTextView view, ITrackingSpan visualSpan, PopupStyles style, Control content)
    {
        if (style.HasFlag(PopupStyles.DismissOnMouseLeaveText) && style.HasFlag(PopupStyles.DismissOnMouseLeaveTextOrContent))
        {
            throw new ArgumentException("DismissOnMouseLeaveText and DismissOnMouseLeaveTextOrContent are mutually exclusive.", nameof(style));
        }

        _view = view;
        _visualSpan = visualSpan;
        _style = style;
        Content = content;
        content.GotFocus += (_, _) => GotFocus?.Invoke(this, EventArgs.Empty);
        content.LostFocus += (_, _) => LostFocus?.Invoke(this, EventArgs.Empty);
        content.PointerWheelChanged += OnContentPointerWheelChanged;
    }

    /// <summary>
    /// Wheel input the popup content didn't consume (a completion list scrolls itself; quick
    /// info and signature help don't) scrolls the view, like scrolling over a VS tooltip.
    /// </summary>
    private void OnContentPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.Handled && !_dismissed && !_view.IsClosed)
        {
            _view.HandleMouseWheel(e);
        }
    }

    private Control? _adjunctContent;

    internal Control Content { get; }

    /// <summary>
    /// Optional secondary content shown beside the popup, top-aligned — the completion
    /// description box. Prefers the left side, falls back to the right; its geometry is
    /// reserved together with the popup's.
    /// </summary>
    internal Control? AdjunctContent
    {
        get => _adjunctContent;
        set
        {
            if (ReferenceEquals(_adjunctContent, value))
            {
                return;
            }

            if (_adjunctContent is { } previous)
            {
                previous.PointerWheelChanged -= OnContentPointerWheelChanged;
                _overlay?.Children.Remove(previous);
            }

            _adjunctContent = value;
            if (value is { } adjunct)
            {
                adjunct.PointerWheelChanged += OnContentPointerWheelChanged;
            }

            _view.QueueSpaceReservationStackRefresh();
        }
    }

    public bool IsMouseOver => Content.IsPointerOver || _adjunctContent?.IsPointerOver == true;

    public bool HasFocus => Content.IsKeyboardFocusWithin;

    public event EventHandler? LostFocus;

    public event EventHandler? GotFocus;

    internal void Update(ITrackingSpan visualSpan, PopupStyles styles)
    {
        if (styles.HasFlag(PopupStyles.DismissOnMouseLeaveText) && styles.HasFlag(PopupStyles.DismissOnMouseLeaveTextOrContent))
        {
            throw new ArgumentException("DismissOnMouseLeaveText and DismissOnMouseLeaveTextOrContent are mutually exclusive.", nameof(styles));
        }

        _visualSpan = visualSpan;
        _style = styles;
    }

    public Geometry? PositionAndDisplay(Geometry reservedSpace)
    {
        if (_dismissed || _view.IsClosed)
        {
            return null;
        }

        if (OverlayLayer.GetOverlayLayer(_view) is not { } overlay)
        {
            // Not in a visual tree that can host popups; the agent is removed (null per
            // the contract) and the popup's owner sees the removal through AgentChanged.
            return null;
        }

        var span = _visualSpan.GetSpan(_view.TextSnapshot);
        if (!_view.TryGetTextViewLines(out var lines)
            || GetAnchorTextRect((IWpfTextViewLineCollection)lines, span) is not { } textRect)
        {
            // The span is scrolled out of the layout: stay alive but hidden ("a non-null
            // but empty Geometry" keeps the agent per the contract).
            Hide();
            return new GeometryGroup();
        }

        // Text coordinates → view-local → overlay: TranslatePoint runs through the render
        // transform, so the anchor lands where the (possibly zoomed) text is on screen.
        var local = new Rect(
            textRect.X - _view.ViewportLeft,
            textRect.Y - _view.ViewportTop,
            textRect.Width,
            textRect.Height);
        if (_view.TranslatePoint(local.TopLeft, overlay) is not { } topLeft
            || _view.TranslatePoint(local.BottomRight, overlay) is not { } bottomRight)
        {
            Hide();
            return new GeometryGroup();
        }

        var anchor = new Rect(topLeft, bottomRight);
        Content.Measure(Size.Infinity);
        // The window's client area bounds the popup (the overlay itself may not have been
        // arranged yet — its Bounds lag behind the window's on the first pass).
        var overlayBounds = TopLevel.GetTopLevel(_view) is { } topLevel
            ? new Rect(topLevel.ClientSize)
            : new Rect(overlay.Bounds.Size);
        var rect = ChoosePlacement(anchor, Content.DesiredSize, overlayBounds, reservedSpace, _style);

        if (_overlay is null)
        {
            _overlay = overlay;
            overlay.Children.Add(Content);
        }

        Canvas.SetLeft(Content, rect.X);
        Canvas.SetTop(Content, rect.Y);
        Content.IsVisible = true;
        EnsureMouseTracking();

        if (_adjunctContent is { } adjunct)
        {
            adjunct.Measure(Size.Infinity);
            var adjunctRect = PlaceAdjunct(rect, adjunct.DesiredSize, overlayBounds);
            if (!overlay.Children.Contains(adjunct))
            {
                overlay.Children.Add(adjunct);
            }

            Canvas.SetLeft(adjunct, adjunctRect.X);
            Canvas.SetTop(adjunct, adjunctRect.Y);
            adjunct.IsVisible = true;
            return new GeometryGroup
            {
                Children = { new RectangleGeometry(rect), new RectangleGeometry(adjunctRect) },
            };
        }

        return new RectangleGeometry(rect);
    }

    /// <summary>
    /// Adjunct placement: beside the popup, top-aligned — on the left when it fits, else
    /// on the right, else clamped into the window on the roomier side.
    /// </summary>
    private static Rect PlaceAdjunct(Rect popup, Size size, Rect bounds)
    {
        const double Gap = 4.0;
        var left = new Rect(new Point(popup.X - size.Width - Gap, popup.Y), size);
        if (bounds.Contains(left))
        {
            return left;
        }

        var right = new Rect(new Point(popup.Right + Gap, popup.Y), size);
        if (bounds.Contains(right))
        {
            return right;
        }

        return ClampInto(popup.X - bounds.X >= bounds.Right - popup.Right ? left : right, bounds);
    }

    /// <summary>
    /// The popup's anchor: the rendered bounds of the visual span (its text-marker
    /// geometry), or the character bounds at the span start when the span is empty.
    /// Returns null when no part of the span is in the rendered layout.
    /// </summary>
    private static Rect? GetAnchorTextRect(IWpfTextViewLineCollection lines, SnapshotSpan span)
    {
        if (span.Length > 0 && lines.GetTextMarkerGeometry(span) is { } marker)
        {
            return marker.Bounds;
        }

        if (lines.GetTextViewLineContainingBufferPosition(span.Start) is { } line)
        {
            var bounds = span.Start < line.End
                ? line.GetCharacterBounds(span.Start)
                : new TextBounds(line.TextRight, line.Top, 0.0, line.Height, line.TextTop, line.TextHeight);
            return new Rect(bounds.Left, bounds.Top, Math.Max(bounds.Width, 0.0), bounds.Height);
        }

        return null;
    }

    /// <summary>
    /// Places the popup per <see cref="PopupStyles"/>: below the span (or right, with
    /// <see cref="PopupStyles.PositionLeftOrRight"/>), preferring the top/left side when
    /// requested, flipping to the opposite side when the preferred one runs off the window
    /// or into space other agents reserved, and finally clamping into the window.
    /// </summary>
    private static Rect ChoosePlacement(Rect anchor, Size size, Rect bounds, Geometry reservedSpace, PopupStyles style)
    {
        bool leftOrRight = style.HasFlag(PopupStyles.PositionLeftOrRight);
        bool preferTopOrLeft = style.HasFlag(PopupStyles.PreferLeftOrTopPosition);
        bool justify = style.HasFlag(PopupStyles.RightOrBottomJustify);

        Rect first, second;
        if (leftOrRight)
        {
            double y = justify ? anchor.Bottom - size.Height : anchor.Top;
            var right = new Rect(new Point(anchor.Right, y), size);
            var left = new Rect(new Point(anchor.Left - size.Width, y), size);
            (first, second) = preferTopOrLeft ? (left, right) : (right, left);
        }
        else
        {
            double x = justify ? anchor.Right - size.Width : anchor.Left;
            var below = new Rect(new Point(x, anchor.Bottom), size);
            var above = new Rect(new Point(x, anchor.Top - size.Height), size);
            (first, second) = preferTopOrLeft ? (above, below) : (below, above);
        }

        bool Fits(Rect rect) => bounds.Contains(rect) && !IntersectsReserved(rect, reservedSpace);

        if (Fits(first))
        {
            return first;
        }

        if (Fits(second))
        {
            return second;
        }

        // Neither side is free: clamp both into the window and keep the one closer to the
        // anchor (the documented PositionClosest behavior; without the flag, the preferred
        // side wins ties by ordering).
        var clampedFirst = ClampInto(first, bounds);
        var clampedSecond = ClampInto(second, bounds);
        if (style.HasFlag(PopupStyles.PositionClosest)
            && Distance(clampedSecond, anchor) < Distance(clampedFirst, anchor))
        {
            return clampedSecond;
        }

        return clampedFirst;
    }

    private static bool IntersectsReserved(Rect rect, Geometry reservedSpace)
    {
        if (reservedSpace is GeometryGroup group)
        {
            foreach (var child in group.Children)
            {
                if (IntersectsReserved(rect, child))
                {
                    return true;
                }
            }

            return false;
        }

        var reserved = reservedSpace.Bounds;
        return reserved is { Width: > 0.0, Height: > 0.0 } && rect.Intersects(reserved);
    }

    private static Rect ClampInto(Rect rect, Rect bounds)
    {
        double x = Math.Max(bounds.X, Math.Min(rect.X, bounds.Right - rect.Width));
        double y = Math.Max(bounds.Y, Math.Min(rect.Y, bounds.Bottom - rect.Height));
        return new Rect(x, y, rect.Width, rect.Height);
    }

    private static double Distance(Rect rect, Rect anchor)
    {
        var rectCenter = rect.Center;
        var anchorCenter = anchor.Center;
        return Math.Abs(rectCenter.X - anchorCenter.X) + Math.Abs(rectCenter.Y - anchorCenter.Y);
    }

    public void Hide()
    {
        if (_overlay is { } overlay)
        {
            overlay.Children.Remove(Content);
            if (_adjunctContent is { } adjunct)
            {
                overlay.Children.Remove(adjunct);
            }

            _overlay = null;
        }

        Content.IsVisible = false;
        if (_adjunctContent is { } adjunctContent)
        {
            adjunctContent.IsVisible = false;
        }
    }

    internal void OnRemoved()
    {
        Hide();
        StopMouseTracking();
    }

    private void EnsureMouseTracking()
    {
        if (!_tracking && (_style & (PopupStyles.DismissOnMouseLeaveText | PopupStyles.DismissOnMouseLeaveTextOrContent)) != 0)
        {
            _tracking = true;
            _view.PointerMoved += OnViewPointerMoved;
        }
    }

    private void StopMouseTracking()
    {
        if (_tracking)
        {
            _tracking = false;
            _view.PointerMoved -= OnViewPointerMoved;
        }
    }

    private void OnViewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dismissed)
        {
            return;
        }

        if (_style.HasFlag(PopupStyles.DismissOnMouseLeaveTextOrContent) && Content.IsPointerOver)
        {
            return;
        }

        var position = e.GetPosition(_view);
        var span = _visualSpan.GetSpan(_view.TextSnapshot);
        if (_view.TryGetTextViewLines(out var lines)
            && GetAnchorTextRect((IWpfTextViewLineCollection)lines, span) is { } textRect)
        {
            var local = new Rect(
                textRect.X - _view.ViewportLeft,
                textRect.Y - _view.ViewportTop,
                textRect.Width,
                textRect.Height);
            if (local.Contains(position))
            {
                return;
            }
        }

        // Left the span (and the content, for the OrContent style): the agent dismisses
        // itself on the next refresh pass, which removes it from its manager.
        _dismissed = true;
        _view.QueueSpaceReservationStackRefresh();
    }
}
