#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Concrete metadata view for margin providers (ADR-003 rule 5).
/// </summary>
public sealed class MarginProviderMetadata : IOrderable
{
    public MarginProviderMetadata(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Name = MetadataValue.Get<string>(data, nameof(Name)) ?? string.Empty;
        MarginContainer = MetadataValue.Get<string>(data, nameof(MarginContainer)) ?? string.Empty;
        Before = MetadataValue.GetMany<string>(data, nameof(Before));
        After = MetadataValue.GetMany<string>(data, nameof(After));
        ContentTypes = MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        TextViewRoles = MetadataValue.GetMany<string>(data, nameof(TextViewRoles));
    }

    public string Name { get; }

    public string MarginContainer { get; }

    public IEnumerable<string> Before { get; }

    public IEnumerable<string> After { get; }

    public IEnumerable<string> ContentTypes { get; }

    public IEnumerable<string> TextViewRoles { get; }
}

/// <summary>
/// A margin container edge (Left/Right/Top/Bottom): stacks its MEF-discovered child margins
/// in definition order and answers <c>GetTextViewMargin</c> recursively.
/// </summary>
internal sealed class MarginContainer : IWpfTextViewMargin
{
    private readonly StackPanel _panel;
    private readonly List<(string Name, IWpfTextViewMargin Margin)> _children = [];
    private bool _isDisposed;

    public MarginContainer(string name, bool horizontal)
    {
        Name = name;
        _panel = new StackPanel
        {
            Orientation = horizontal ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal,
        };
    }

    public string Name { get; }

    public Control VisualElement => _panel;

    public double MarginSize => Name is PredefinedMarginNames.Top or PredefinedMarginNames.Bottom
        ? _panel.Bounds.Height
        : _panel.Bounds.Width;

    public bool Enabled => true;

    public void AddMargin(string name, IWpfTextViewMargin margin)
    {
        _children.Add((name, margin));
        margin.VisualElement.IsVisible = margin.Enabled;
        _panel.Children.Add(margin.VisualElement);
    }

    public ITextViewMargin? GetTextViewMargin(string marginName)
    {
        if (string.Equals(marginName, Name, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        foreach (var (name, margin) in _children)
        {
            if (string.Equals(marginName, name, StringComparison.OrdinalIgnoreCase))
            {
                return margin;
            }

            if (margin.GetTextViewMargin(marginName) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            foreach (var (_, margin) in _children)
            {
                margin.Dispose();
            }
        }
    }
}

/// <summary>
/// Line numbers, right-aligned to the view lines (VS margin semantics: one number per
/// snapshot line, on its first view line).
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(PredefinedMarginNames.LineNumber)]
[MarginContainer(PredefinedMarginNames.Left)]
[ContentType("text")]
[Order(After = PredefinedMarginNames.Glyph)]
public sealed class LineNumberMarginProvider : IWpfTextViewMarginProvider
{
    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new LineNumberMargin(wpfTextViewHost.TextView);
    }

    private sealed class LineNumberMargin : Control, IWpfTextViewMargin
    {
        private static readonly IBrush NumberBrush = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85));

        private readonly IWpfTextView _view;
        private bool _isDisposed;

        public LineNumberMargin(IWpfTextView view)
        {
            _view = view;
            view.LayoutChanged += (_, _) => Refresh();
            view.Options.OptionChanged += (_, _) => Refresh();
            view.ZoomLevelChanged += (_, _) => Refresh();
            Refresh();
        }

        public Control VisualElement => this;

        public double MarginSize => Bounds.Width;

        public bool Enabled => _view.Options.GetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId);

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => string.Equals(marginName, PredefinedMarginNames.LineNumber, StringComparison.OrdinalIgnoreCase) ? this : null;

        protected override Size MeasureOverride(Size availableSize)
        {
            int digits = Math.Max(3, _view.TextSnapshot.LineCount.ToString(CultureInfo.InvariantCulture).Length);
            double columnWidth = _view.FormattedLineSource?.ColumnWidth ?? 8.0;
            // The margin scales with the view's zoom (the view itself is scaled by a
            // render transform; the margin draws at the zoomed size directly).
            return new Size((digits + 2) * columnWidth * (_view.ZoomLevel / 100.0), 0.0);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_isDisposed || _view.IsClosed || _view.InLayout || !Enabled)
            {
                return;
            }

            // Render is strictly read-only: a layout must never be triggered from the
            // render pass (Avalonia throws on invalidation during rendering).
            if (_view is not ITextView2 view2 || !view2.TryGetTextViewLines(out var textViewLines))
            {
                return;
            }

            var properties = _view.FormattedLineSource.DefaultTextProperties;
            double zoom = _view.ZoomLevel / 100.0;
            foreach (var line in textViewLines)
            {
                if (!line.IsFirstTextViewLineForSnapshotLine)
                {
                    continue;
                }

                int number = line.Start.GetContainingLine().LineNumber + 1;
                var text = new FormattedText(
                    number.ToString(CultureInfo.InvariantCulture),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    properties.Typeface,
                    properties.FontRenderingEmSize * zoom,
                    NumberBrush);
                context.DrawText(text, new Point(Bounds.Width - text.Width - 6.0 * zoom, (line.TextTop - _view.ViewportTop) * zoom));
            }
        }

        private void Refresh()
        {
            IsVisible = Enabled;
            InvalidateMeasure();
            InvalidateVisual();
        }

        public void Dispose() => _isDisposed = true;
    }
}

/// <summary>
/// A minimal glyph margin strip. Glyph factory providers (IGlyphFactoryProvider over
/// IGlyphTag) attach in a later milestone; the margin itself is discoverable and ordered.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(PredefinedMarginNames.Glyph)]
[MarginContainer(PredefinedMarginNames.Left)]
[ContentType("text")]
[Order]
public sealed class GlyphMarginProvider : IWpfTextViewMarginProvider
{
    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        => new GlyphMargin();

    private sealed class GlyphMargin : Border, IWpfTextViewMargin
    {
        public GlyphMargin()
        {
            Width = 18.0;
            Background = Brushes.Transparent;
        }

        public Control VisualElement => this;

        public double MarginSize => Width;

        public bool Enabled => true;

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => string.Equals(marginName, PredefinedMarginNames.Glyph, StringComparison.OrdinalIgnoreCase) ? this : null;

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// The vertical scrollbar margin, also implementing <see cref="IVerticalScrollBar"/> over a
/// line-linear scroll map (elisions expand; the outlining-aware map lands with projection
/// support in M5).
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(PredefinedMarginNames.VerticalScrollBar)]
[MarginContainer(PredefinedMarginNames.Right)]
[ContentType("text")]
[Order]
public sealed class VerticalScrollBarMarginProvider : IWpfTextViewMarginProvider
{
    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new VerticalScrollBarMargin(wpfTextViewHost.TextView);
    }

    internal sealed class VerticalScrollBarMargin : ScrollBar, IWpfTextViewMargin, IVerticalScrollBar
    {
        private readonly IWpfTextView _view;
        private readonly LineScrollMap _map;
        private bool _synchronizing;

        public VerticalScrollBarMargin(IWpfTextView view)
        {
            _view = view;
            _map = new LineScrollMap(view);
            Orientation = Avalonia.Layout.Orientation.Vertical;
            Width = 14.0;
            AllowAutoHide = false;
            Minimum = 0.0;
            SmallChange = 1.0;
            view.LayoutChanged += (_, _) => SynchronizeFromView();
            Scroll += (_, e) =>
            {
                if (!_synchronizing && !_view.IsClosed)
                {
                    _view.DisplayTextLineContainingBufferPosition(
                        _map.GetBufferPositionAtCoordinate(e.NewValue), 0.0, ViewRelativePosition.Top);
                }
            };
            SynchronizeFromView();
        }

        public Control VisualElement => this;

        public double MarginSize => Width;

        public bool Enabled => _view.Options.GetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId);

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => string.Equals(marginName, PredefinedMarginNames.VerticalScrollBar, StringComparison.OrdinalIgnoreCase) ? this : null;

        public void Dispose()
        {
        }

        #region IVerticalScrollBar

        public IScrollMap Map => _map;

        public double ThumbHeight => Math.Max(1.0, ViewportSize / Math.Max(1.0, Maximum + ViewportSize) * TrackSpanHeight);

        public double TrackSpanTop => 0.0;

        public double TrackSpanBottom => Bounds.Height;

        public double TrackSpanHeight => Bounds.Height;

        public event EventHandler? TrackSpanChanged;

        public double GetYCoordinateOfBufferPosition(SnapshotPoint bufferPosition)
            => GetYCoordinateOfScrollMapPosition(_map.GetCoordinateAtBufferPosition(bufferPosition));

        public double GetYCoordinateOfScrollMapPosition(double scrollMapPosition)
        {
            double range = Math.Max(1.0, _map.End - _map.Start + _map.ThumbSize);
            return TrackSpanTop + ((scrollMapPosition - _map.Start) / range * TrackSpanHeight);
        }

        public SnapshotPoint GetBufferPositionOfYCoordinate(double y)
        {
            double range = Math.Max(1.0, _map.End - _map.Start + _map.ThumbSize);
            double coordinate = _map.Start + ((y - TrackSpanTop) / TrackSpanHeight * range);
            return _map.GetBufferPositionAtCoordinate(coordinate);
        }

        #endregion

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            TrackSpanChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SynchronizeFromView()
        {
            if (_view.IsClosed || _view.InLayout)
            {
                return;
            }

            // Only read published lines; a layout must never be triggered from here (this
            // also runs during host construction, before the first layout).
            if (_view is not ITextView2 view2 || !view2.TryGetTextViewLines(out var lines))
            {
                return;
            }

            _synchronizing = true;
            try
            {
                IsVisible = Enabled;
                // The scrollbar tracks the visual buffer: collapsed regions shrink the
                // scrollable range rather than leaving unreachable dead space.
                int firstLine = (int)_map.GetCoordinateAtBufferPosition(lines.FirstVisibleLine.Start);
                double viewportLines = Math.Max(1.0, _view.ViewportHeight / _view.LineHeight);
                Maximum = Math.Max(0.0, _view.VisualSnapshot.LineCount - viewportLines);
                ViewportSize = viewportLines;
                LargeChange = viewportLines;
                Value = firstLine;
            }
            finally
            {
                _synchronizing = false;
            }
        }

        private sealed class LineScrollMap : IScrollMap
        {
            private readonly IWpfTextView _view;

            public LineScrollMap(IWpfTextView view)
            {
                _view = view;
                view.TextBuffer.Changed += (_, _) => MappingChanged?.Invoke(this, EventArgs.Empty);
                if (!ReferenceEquals(view.TextViewModel.VisualBuffer, view.TextViewModel.EditBuffer))
                {
                    // Collapse/expand reshapes the visual buffer without an edit.
                    view.TextViewModel.VisualBuffer.Changed += (_, _) => MappingChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public event EventHandler? MappingChanged;

            public ITextView TextView => _view;

            public bool AreElisionsExpanded => false;

            public double Start => 0.0;

            public double End => Math.Max(0.0, _view.VisualSnapshot.LineCount - 1);

            public double ThumbSize => Math.Max(1.0, _view.ViewportHeight / _view.LineHeight);

            public double GetCoordinateAtBufferPosition(SnapshotPoint bufferPosition)
                => _view.TextViewModel
                    .GetNearestPointInVisualSnapshot(bufferPosition, _view.VisualSnapshot, PointTrackingMode.Negative)
                    .GetContainingLine()
                    .LineNumber;

            public SnapshotPoint GetBufferPositionAtCoordinate(double coordinate)
            {
                int lineNumber = Math.Clamp((int)Math.Round(coordinate), 0, _view.VisualSnapshot.LineCount - 1);
                var visualLine = _view.VisualSnapshot.GetLineFromLineNumber(lineNumber);
                return _view.BufferGraph.MapDownToSnapshot(
                        visualLine.Start, PointTrackingMode.Negative, _view.TextSnapshot, PositionAffinity.Successor)
                    ?? new SnapshotPoint(_view.TextSnapshot, 0);
            }

            public double GetFractionAtBufferPosition(SnapshotPoint bufferPosition)
                => End <= Start ? 0.0 : GetCoordinateAtBufferPosition(bufferPosition) / (End - Start);

            public SnapshotPoint GetBufferPositionAtFraction(double fraction)
                => GetBufferPositionAtCoordinate(Start + (fraction * (End - Start)));
        }
    }
}

/// <summary>
/// The horizontal scrollbar margin (hidden when word wrap is enabled).
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(PredefinedMarginNames.HorizontalScrollBar)]
[MarginContainer(PredefinedMarginNames.Bottom)]
[ContentType("text")]
[Order]
public sealed class HorizontalScrollBarMarginProvider : IWpfTextViewMarginProvider
{
    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new HorizontalScrollBarMargin(wpfTextViewHost.TextView);
    }

    private sealed class HorizontalScrollBarMargin : ScrollBar, IWpfTextViewMargin
    {
        private readonly IWpfTextView _view;
        private bool _synchronizing;

        public HorizontalScrollBarMargin(IWpfTextView view)
        {
            _view = view;
            Orientation = Avalonia.Layout.Orientation.Horizontal;
            Height = 14.0;
            AllowAutoHide = false;
            Minimum = 0.0;
            view.LayoutChanged += (_, _) => SynchronizeFromView();
            view.Options.OptionChanged += (_, _) => SynchronizeFromView();
            Scroll += (_, e) =>
            {
                if (!_synchronizing && !_view.IsClosed)
                {
                    _view.ViewportLeft = e.NewValue;
                }
            };
        }

        public Control VisualElement => this;

        public double MarginSize => Height;

        public bool Enabled
            => _view.Options.GetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId)
               && (_view.Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId) & WordWrapStyles.WordWrap) == 0;

        public ITextViewMargin? GetTextViewMargin(string marginName)
            => string.Equals(marginName, PredefinedMarginNames.HorizontalScrollBar, StringComparison.OrdinalIgnoreCase) ? this : null;

        public void Dispose()
        {
        }

        private void SynchronizeFromView()
        {
            if (_view.IsClosed)
            {
                return;
            }

            _synchronizing = true;
            try
            {
                IsVisible = Enabled;
                Maximum = Math.Max(0.0, _view.MaxTextRightCoordinate + 20.0 - _view.ViewportWidth);
                ViewportSize = Math.Max(1.0, _view.ViewportWidth);
                LargeChange = _view.ViewportWidth;
                SmallChange = 16.0;
                Value = _view.ViewportLeft;
            }
            finally
            {
                _synchronizing = false;
            }
        }
    }
}
