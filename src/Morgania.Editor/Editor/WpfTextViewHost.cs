#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia.Controls;

using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The view host: the text view surrounded by its four margin containers, whose child
/// margins are MEF-discovered per container, filtered by content type and view roles, and
/// stacked in definition order (M4 acceptance). The top/left containers reserve space;
/// the right/bottom containers overlay the view's cell (VS Code-style scrollbars).
/// </summary>
internal sealed class WpfTextViewHost : IWpfTextViewHost
{
    private readonly Grid _hostControl;
    private readonly MarginContainer[] _containers;
    private bool _isClosed;

    public WpfTextViewHost(
        IWpfTextView wpfTextView,
        bool setFocus,
        IEnumerable<Lazy<IWpfTextViewMarginProvider, MarginProviderMetadata>> marginProviders)
    {
        TextView = wpfTextView;

        // VS convention: the host is discoverable from its view's property bag (hosts of
        // the view control need it to reach the margins).
        wpfTextView.Properties.AddProperty(typeof(IWpfTextViewHost), this);

        var left = new MarginContainer(PredefinedMarginNames.Left, horizontal: false);
        var right = new MarginContainer(PredefinedMarginNames.Right, horizontal: false);
        var top = new MarginContainer(PredefinedMarginNames.Top, horizontal: true);
        var bottom = new MarginContainer(PredefinedMarginNames.Bottom, horizontal: true);
        _containers = [left, right, top, bottom];

        var contentType = wpfTextView.TextBuffer.ContentType;
        foreach (var container in _containers)
        {
            var applicable = marginProviders
                .Where(provider =>
                    string.Equals(provider.Metadata.MarginContainer, container.Name, StringComparison.OrdinalIgnoreCase)
                    && provider.Metadata.ContentTypes?.Any(contentType.IsOfType) != false
                    && (provider.Metadata.TextViewRoles?.Any() != true || wpfTextView.Roles.ContainsAny(provider.Metadata.TextViewRoles)))
                .ToList();
            foreach (var provider in Orderer.Order(applicable))
            {
                if (provider.Value.CreateMargin(this, container) is { } margin)
                {
                    container.AddMargin(provider.Metadata.Name, margin);
                }
            }
        }

        _hostControl = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        AddCell(top.VisualElement, row: 0, column: 0, columnSpan: 3);
        AddCell(left.VisualElement, row: 1, column: 0);

        // The zoom render transform can paint the view beyond its arranged slot; the
        // decorator's clip keeps it inside its cell (the transform ignores the view's own
        // ClipToBounds, which scales along with the content).
        AddCell(new Decorator { Child = wpfTextView.VisualElement, ClipToBounds = true }, row: 1, column: 1);

        // The right/bottom containers (the scrollbars) float over the view's cell instead of
        // reserving space beside it, so content flows under them (VS Code overlay scrollbars).
        right.VisualElement.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
        AddCell(right.VisualElement, row: 1, column: 1);
        bottom.VisualElement.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        AddCell(bottom.VisualElement, row: 1, column: 1);

        if (setFocus)
        {
            wpfTextView.VisualElement.Focus();
        }
    }

    public event EventHandler? Closed;

    public bool IsClosed => _isClosed;

    public Control HostControl => _hostControl;

    public IWpfTextView TextView { get; }

    public IWpfTextViewMargin? GetTextViewMargin(string marginName)
    {
        ArgumentNullException.ThrowIfNull(marginName);
        foreach (var container in _containers)
        {
            if (container.GetTextViewMargin(marginName) is IWpfTextViewMargin margin)
            {
                return margin;
            }
        }

        return null;
    }

    public void Close()
    {
        if (_isClosed)
        {
            throw new InvalidOperationException("The host is already closed.");
        }

        _isClosed = true;
        foreach (var container in _containers)
        {
            container.Dispose();
        }

        if (!TextView.IsClosed)
        {
            TextView.Close();
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adds a control overlaying the text view's cell (top-anchored chrome like the
    /// find/replace panel), outside the view's zoom transform and unaffected by scrolling.
    /// </summary>
    internal void AddViewOverlay(Control control)
    {
        Grid.SetRow(control, 1);
        Grid.SetColumn(control, 1);
        _hostControl.Children.Add(control);
    }

    private void AddCell(Control control, int row, int column, int columnSpan = 1)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        Grid.SetColumnSpan(control, columnSpan);
        _hostControl.Children.Add(control);
    }
}
