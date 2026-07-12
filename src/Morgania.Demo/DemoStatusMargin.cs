namespace Microsoft.VisualStudio.Demo;

using System.Composition;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// A live status bar in the host's Bottom margin container: caret line/column, selection
/// size, caret count, and zoom on the left; the gesture cheat sheet on the right. This is
/// the margin extensibility seam (<see cref="IWpfTextViewMarginProvider"/>) that
/// extensions use for info bars.
/// </summary>
[Export(typeof(IWpfTextViewMarginProvider))]
[Name(StatusMargin.MarginName)]
[MarginContainer(PredefinedMarginNames.Bottom)]
[ContentType("code")]
[Order(After = PredefinedMarginNames.HorizontalScrollBar)]
public sealed class StatusMarginProvider : IWpfTextViewMarginProvider
{
    public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
    {
        ArgumentNullException.ThrowIfNull(wpfTextViewHost);
        return new StatusMargin(wpfTextViewHost.TextView);
    }
}

public sealed class StatusMargin : Border, IWpfTextViewMargin
{
    public const string MarginName = "DemoStatusMargin";

    private readonly IWpfTextView _view;
    private readonly TextBlock _position;

    public StatusMargin(IWpfTextView view)
    {
        _view = view;
        _position = new TextBlock
        {
            FontSize = 11.0,
            Foreground = Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        var cheatSheet = new TextBlock
        {
            FontSize = 11.0,
            Foreground = Brushes.White,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Text = "Ctrl+Space complete · hover for info · ( for signatures · Ctrl+M fold · Ctrl+Wheel zoom · Alt+Z wrap · Alt+Click add caret",
        };

        Height = 22.0;
        Padding = new Thickness(8.0, 0.0);
        Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
        Child = new DockPanel
        {
            LastChildFill = true,
            Children = { cheatSheet, _position },
        };
        DockPanel.SetDock(cheatSheet, Dock.Right);

        _view.Caret.PositionChanged += OnViewStateChanged;
        _view.Selection.SelectionChanged += OnViewStateChanged;
        _view.ZoomLevelChanged += OnViewStateChanged;
        UpdateStatus();
    }

    public Control VisualElement => this;

    public double MarginSize => Height;

    public bool Enabled => true;

    public ITextViewMargin? GetTextViewMargin(string marginName) =>
        string.Equals(marginName, MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;

    public void Dispose()
    {
        _view.Caret.PositionChanged -= OnViewStateChanged;
        _view.Selection.SelectionChanged -= OnViewStateChanged;
        _view.ZoomLevelChanged -= OnViewStateChanged;
    }

    private void OnViewStateChanged(object? sender, EventArgs e) => UpdateStatus();

    private void UpdateStatus()
    {
        if (_view.IsClosed)
        {
            return;
        }

        var caret = _view.Caret.Position.BufferPosition;
        var line = caret.GetContainingLine();
        int selected = _view.Selection.SelectedSpans.Sum(static span => span.Length);
        int carets = _view.GetMultiSelectionBroker().AllSelections.Count;
        _position.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Ln {line.LineNumber + 1}, Col {caret.Position - line.Start.Position + 1}   Sel {selected}   Carets {carets}   Zoom {_view.ZoomLevel:0}%");
    }
}
