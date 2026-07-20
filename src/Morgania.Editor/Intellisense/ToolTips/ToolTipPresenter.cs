#nullable enable

namespace Microsoft.VisualStudio.Text.Adornments.Implementation;

using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Threading;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(IToolTipPresenterFactory))]
[Name("default")]
public sealed class ToolTipPresenterFactory : IToolTipPresenterFactory
{
    private readonly Lazy<IViewElementFactoryService> _viewElementFactory;
    private readonly Lazy<IEditorFormatMapService> _editorFormatMaps;

    [ImportingConstructor]
    public ToolTipPresenterFactory(
        Lazy<IViewElementFactoryService> viewElementFactory,
        Lazy<IEditorFormatMapService> editorFormatMaps)
    {
        _viewElementFactory = viewElementFactory;
        _editorFormatMaps = editorFormatMaps;
    }

    public IToolTipPresenter Create(ITextView textView, ToolTipParameters parameters)
        => new ToolTipPresenter(
            textView,
            parameters,
            _viewElementFactory.Value,
            PopupBrushes.Read(_editorFormatMaps.Value.GetEditorFormatMap(textView)));
}

/// <summary>
/// The default single-use tooltip presenter (the Modern ToolTip spec): converts the content
/// through the view element factories and shows it in a popup on the "quickinfo" space
/// reservation manager. Mouse-tracking presenters dismiss when the pointer leaves both the
/// applicable span and the tip, unless the content asks to be kept open.
/// </summary>
internal sealed class ToolTipPresenter : IToolTipPresenter, IToolTipPresenter2
{
    private const double MaxTipWidth = 600.0;

    private readonly ITextView _view;
    private readonly ToolTipParameters _parameters;
    private readonly IViewElementFactoryService _viewElementFactory;
    private readonly Border _container;
    private readonly StackPanel _panel;
    private readonly double _maxTipWidth;
    private ITrackingSpan? _applicableToSpan;
    private ISpaceReservationManager? _manager;
    private ISpaceReservationAgent? _agent;
    private bool _dismissed;

    public ToolTipPresenter(
        ITextView view,
        ToolTipParameters parameters,
        IViewElementFactoryService viewElementFactory,
        PopupBrushes brushes,
        double maxTipWidth = MaxTipWidth)
    {
        _view = view;
        _parameters = parameters;
        _viewElementFactory = viewElementFactory;
        _maxTipWidth = maxTipWidth;
        _panel = new StackPanel();
        _container = new Border
        {
            Child = _panel,
            Background = brushes.Background,
            BorderBrush = brushes.BorderBrush,
            BorderThickness = new Thickness(1.0),
            CornerRadius = new CornerRadius(3.0),
            Padding = new Thickness(8.0, 5.0),
            MaxWidth = maxTipWidth,
        };
        _container.SetValue(TextElement.ForegroundProperty, brushes.Foreground);
    }

    public event EventHandler? Dismissed;

    public bool IsMouseOverAggregated => _container.IsPointerOver;

    public void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content)
    {
        ArgumentNullException.ThrowIfNull(applicableToSpan);
        ArgumentNullException.ThrowIfNull(content);
        if (_dismissed)
        {
            return;
        }

        _applicableToSpan = applicableToSpan;
        _container.MaxWidth = _view.ViewportWidth > 0.0 ? Math.Min(_maxTipWidth, _view.ViewportWidth) : _maxTipWidth;
        _panel.Children.Clear();
        foreach (var item in content)
        {
            if (item is not null && _viewElementFactory.CreateViewElement<Control>(_view, item) is { } element)
            {
                _panel.Children.Add(element);
            }
        }

        if (_agent is null)
        {
            _manager = _view.GetSpaceReservationManager(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName);
            _agent = _manager.CreatePopupAgent(applicableToSpan, PopupStyles.None, _container);
            _manager.AgentChanged += OnAgentChanged;
            _manager.AddAgent(_agent);

            _view.TextBuffer.Changed += OnBufferChanged;
            _view.Closed += OnViewClosed;
            _view.LostAggregateFocus += OnViewLostAggregateFocus;
            if (_parameters.TrackMouse && _view is IWpfTextView wpfView)
            {
                wpfView.VisualElement.PointerMoved += OnViewPointerMoved;
                wpfView.VisualElement.PointerExited += OnViewPointerExited;
            }
        }
        else
        {
            _manager!.UpdatePopupAgent(_agent, applicableToSpan, PopupStyles.None);
        }
    }

    public void Dismiss()
    {
        if (_dismissed)
        {
            return;
        }

        _dismissed = true;
        _view.TextBuffer.Changed -= OnBufferChanged;
        _view.Closed -= OnViewClosed;
        _view.LostAggregateFocus -= OnViewLostAggregateFocus;
        if (_view is IWpfTextView wpfView)
        {
            wpfView.VisualElement.PointerMoved -= OnViewPointerMoved;
            wpfView.VisualElement.PointerExited -= OnViewPointerExited;
        }

        if (_manager is { } manager)
        {
            manager.AgentChanged -= OnAgentChanged;
            if (_agent is { } agent)
            {
                manager.RemoveAgent(agent);
                _agent = null;
            }
        }

        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    private void OnAgentChanged(object? sender, SpaceReservationAgentChangedEventArgs e)
    {
        // The manager can drop the agent on its own (the popup lost its host); the
        // presenter must still report dismissal.
        if (!_dismissed && ReferenceEquals(e.OldAgent, _agent) && e.NewAgent is null)
        {
            _agent = null;
            Dismiss();
        }
    }

    private void OnBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        // Mouse-tracking tips always dismiss on change (IgnoreBufferChange is only valid
        // on non-tracking tips, per the ToolTipParameters contract).
        if (_parameters.TrackMouse || !_parameters.IgnoreBufferChange)
        {
            Dismiss();
        }
    }

    private void OnViewClosed(object? sender, EventArgs e) => Dismiss();

    private void OnViewLostAggregateFocus(object? sender, EventArgs e) => Dismiss();

    private void OnViewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dismissed || _applicableToSpan is null || _parameters.KeepOpen || _container.IsPointerOver)
        {
            return;
        }

        if (_view is not IWpfTextView wpfView
            || _view is not ITextView2 view2
            || !view2.TryGetTextViewLines(out var lines))
        {
            return;
        }

        var span = _applicableToSpan.GetSpan(_view.TextSnapshot);
        var position = e.GetPosition(wpfView.VisualElement);
        double x = position.X + _view.ViewportLeft;
        double y = position.Y + _view.ViewportTop;
        if (span.Length > 0
            && ((Formatting.IWpfTextViewLineCollection)lines).GetTextMarkerGeometry(span) is { } marker
            && marker.Bounds.Inflate(3.0).Contains(new Point(x, y)))
        {
            return;
        }

        // The pointer left the applicable span and isn't over the tip: schedule the check
        // once more after this input settles (moving from the text into the tip crosses
        // ground that belongs to neither), then dismiss.
        DismissOncePointerSettlesOutsideTip();
    }

    /// <summary>
    /// A fast exit can leave the view without a single move inside it, so tracking tips
    /// also dismiss when the pointer exits the view — which includes moving onto the tip
    /// itself (the popup covers the view from the overlay layer), hence the settled check.
    /// </summary>
    private void OnViewPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_dismissed && !_parameters.KeepOpen)
        {
            DismissOncePointerSettlesOutsideTip();
        }
    }

    private void DismissOncePointerSettlesOutsideTip() => Dispatcher.UIThread.Post(() =>
    {
        if (!_dismissed && !_container.IsPointerOver && !_parameters.KeepOpen)
        {
            Dismiss();
        }
    }, DispatcherPriority.Input);
}
