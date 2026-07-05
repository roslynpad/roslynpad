#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Collections.ObjectModel;

using Avalonia.Controls;

using Microsoft.VisualStudio.Text;

/// <summary>
/// An adornment layer: an absolutely positioned panel above the text. Text-relative
/// adornments are placed at the leading edge of their visual span and repositioned on every
/// layout. Space negotiation and the full positioning matrix are part of M3.
/// </summary>
internal sealed class AdornmentLayer : Canvas, IAdornmentLayer
{
    private readonly WpfTextView _view;
    private readonly List<AdornmentLayerElement> _elements = [];

    public AdornmentLayer(WpfTextView view)
    {
        _view = view;
        ClipToBounds = true;
    }

    public IWpfTextView TextView => _view;

    public ReadOnlyCollection<IAdornmentLayerElement> Elements
        => new([.. _elements]);

    public bool IsEmpty => _elements.Count == 0;

    public new double Opacity
    {
        get => base.Opacity;
        set => base.Opacity = value;
    }

    public bool AddAdornment(SnapshotSpan visualSpan, object? tag, Control adornment)
        => AddAdornment(AdornmentPositioningBehavior.TextRelative, visualSpan, tag, adornment, null);

    public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object? tag, Control adornment, AdornmentRemovedCallback? removedCallback)
    {
        ArgumentNullException.ThrowIfNull(adornment);
        if (behavior == AdornmentPositioningBehavior.TextRelative && visualSpan is null)
        {
            throw new ArgumentNullException(nameof(visualSpan));
        }

        if (visualSpan is { } span && !_view.TextViewLines.IntersectsBufferSpan(span))
        {
            return false;
        }

        var element = new AdornmentLayerElement(adornment, behavior, removedCallback, tag, visualSpan);
        _elements.Add(element);
        Children.Add(adornment);
        Position(element);
        return true;
    }

    public void RemoveAdornment(Control adornment)
    {
        ArgumentNullException.ThrowIfNull(adornment);
        RemoveMatchingAdornments(element => element.Adornment == adornment);
    }

    public void RemoveAdornmentsByTag(object tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        RemoveMatchingAdornments(element => Equals(element.Tag, tag));
    }

    public void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan)
        => RemoveMatchingAdornments(visualSpan, _ => true);

    public void RemoveAllAdornments() => RemoveMatchingAdornments(_ => true);

    public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        for (int i = _elements.Count - 1; i >= 0; i--)
        {
            var element = _elements[i];
            if (match(element))
            {
                _elements.RemoveAt(i);
                Children.Remove(element.Adornment);
                element.RemovedCallback?.Invoke(element.Tag, element.Adornment);
            }
        }
    }

    public void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        RemoveMatchingAdornments(element =>
            element.VisualSpan is { } span
            && visualSpan.IntersectsWith(span.TranslateTo(visualSpan.Snapshot, SpanTrackingMode.EdgeInclusive))
            && match(element));
    }

    /// <summary>Repositions all elements after a layout; called by the view.</summary>
    internal void OnLayoutChanged()
    {
        for (int i = _elements.Count - 1; i >= 0; i--)
        {
            var element = _elements[i];
            if (element.VisualSpan is { } span)
            {
                var translated = span.TranslateTo(_view.TextSnapshot, SpanTrackingMode.EdgeInclusive);
                if (!_view.TextViewLines.IntersectsBufferSpan(translated))
                {
                    _elements.RemoveAt(i);
                    Children.Remove(element.Adornment);
                    element.RemovedCallback?.Invoke(element.Tag, element.Adornment);
                    continue;
                }

                element.SetVisualSpan(translated);
            }

            Position(element);
        }
    }

    private void Position(AdornmentLayerElement element)
    {
        if (element.Behavior == AdornmentPositioningBehavior.TextRelative && element.VisualSpan is { } span)
        {
            var start = span.Start.Position <= _view.TextSnapshot.Length ? span.Start : new SnapshotPoint(_view.TextSnapshot, _view.TextSnapshot.Length);
            if (_view.TextViewLines.ContainsBufferPosition(start))
            {
                var line = _view.TextViewLines.GetTextViewLineContainingBufferPosition(start);
                var bounds = line.GetCharacterBounds(start);
                SetLeft(element.Adornment, bounds.Left - _view.ViewportLeft);
                SetTop(element.Adornment, line.TextTop - _view.ViewportTop);
            }
        }
    }

    private sealed class AdornmentLayerElement : IAdornmentLayerElement
    {
        public AdornmentLayerElement(Control adornment, AdornmentPositioningBehavior behavior, AdornmentRemovedCallback? removedCallback, object? tag, SnapshotSpan? visualSpan)
        {
            Adornment = adornment;
            Behavior = behavior;
            RemovedCallback = removedCallback;
            Tag = tag;
            VisualSpan = visualSpan;
        }

        public Control Adornment { get; }

        public AdornmentPositioningBehavior Behavior { get; }

        public AdornmentRemovedCallback? RemovedCallback { get; }

        public object? Tag { get; }

        public SnapshotSpan? VisualSpan { get; private set; }

        public void SetVisualSpan(SnapshotSpan span) => VisualSpan = span;
    }
}
