#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation;

using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// A signature help session: augments its signatures from the broker's sources, selects the
/// best match, and hosts its presenter's popup through the presenter-declared space
/// reservation manager. Caret-tracking sessions dismiss when the caret leaves the selected
/// signature's applicability span; buffer changes recalculate.
/// </summary>
internal sealed class SignatureHelpSession : ISignatureHelpSession
{
    private readonly SignatureHelpBroker _broker;
    private readonly ITextView _view;
    private readonly ITrackingPoint _triggerPoint;
    private readonly bool _trackCaret;
    private readonly ObservableCollection<ISignature> _signatures = [];
    private readonly ReadOnlyObservableCollection<ISignature> _readOnlySignatures;
    private List<ISignatureHelpSource>? _sources;
    private ISignature? _selectedSignature;
    private IIntellisensePresenter? _presenter;
    private IPopupIntellisensePresenter? _popupPresenter;
    private ISpaceReservationManager? _manager;
    private ISpaceReservationAgent? _agent;
    private bool _isStarted;
    private bool _isDismissed;

    public SignatureHelpSession(SignatureHelpBroker broker, ITextView view, ITrackingPoint triggerPoint, bool trackCaret)
    {
        _broker = broker;
        _view = view;
        _triggerPoint = triggerPoint;
        _trackCaret = trackCaret;
        _readOnlySignatures = new ReadOnlyObservableCollection<ISignature>(_signatures);
    }

    public ReadOnlyObservableCollection<ISignature> Signatures => _readOnlySignatures;

    public ISignature SelectedSignature
    {
        get => _selectedSignature!;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!_signatures.Contains(value))
            {
                throw new ArgumentException("The signature is not a member of this session's Signatures collection.", nameof(value));
            }

            if (!ReferenceEquals(_selectedSignature, value))
            {
                var previous = _selectedSignature;
                _selectedSignature = value;
                SelectedSignatureChanged?.Invoke(this, new SelectedSignatureChangedEventArgs(previous!, value));
                UpdatePopup();
            }
        }
    }

    public ITextView TextView => _view;

    public IIntellisensePresenter Presenter => _presenter!;

    public bool IsDismissed => _isDismissed;

    public PropertyCollection Properties { get; } = new();

    public event EventHandler<SelectedSignatureChangedEventArgs>? SelectedSignatureChanged;

    public event EventHandler? PresenterChanged;

    public event EventHandler? Dismissed;

    public event EventHandler? Recalculated;

    public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        if (ReferenceEquals(textBuffer, _triggerPoint.TextBuffer))
        {
            return _triggerPoint;
        }

        var mapped = GetTriggerPoint(textBuffer.CurrentSnapshot)
            ?? throw new ArgumentException("The trigger point does not map to the requested buffer.", nameof(textBuffer));
        return mapped.Snapshot.CreateTrackingPoint(mapped.Position, PointTrackingMode.Negative);
    }

    public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
    {
        ArgumentNullException.ThrowIfNull(textSnapshot);
        if (ReferenceEquals(textSnapshot.TextBuffer, _triggerPoint.TextBuffer))
        {
            return _triggerPoint.GetPoint(textSnapshot);
        }

        var point = _triggerPoint.GetPoint(_triggerPoint.TextBuffer.CurrentSnapshot);
        return _view.BufferGraph.MapDownToBuffer(
                point, PointTrackingMode.Negative, textSnapshot.TextBuffer, PositionAffinity.Predecessor)
            ?? _view.BufferGraph.MapUpToBuffer(
                point, PointTrackingMode.Negative, PositionAffinity.Predecessor, textSnapshot.TextBuffer);
    }

    public void Start()
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("The session is already started.");
        }

        if (_isDismissed)
        {
            throw new InvalidOperationException("The session is dismissed.");
        }

        _isStarted = true;
        _sources = _broker.CreateSources(_view.TextBuffer);
        if (!Augment())
        {
            Dismiss();
            return;
        }

        SignatureHelpBroker.GetSessionList(_view).Add(this);
        _view.TextBuffer.Changed += OnBufferChanged;
        _view.Closed += OnViewClosed;
        _view.LostAggregateFocus += OnViewLostAggregateFocus;
        if (_trackCaret)
        {
            _view.Caret.PositionChanged += OnCaretPositionChanged;
        }

        _presenter = _broker.CreatePresenter(this);
        PresenterChanged?.Invoke(this, EventArgs.Empty);
        _popupPresenter = _presenter as IPopupIntellisensePresenter;
        if (_popupPresenter is { } popup)
        {
            popup.PresentationSpanChanged += OnPresentationChanged;
            popup.PopupStylesChanged += OnPopupStylesChanged;
            popup.SurfaceElementChanged += OnPresentationChanged;
            _manager = _view.GetSpaceReservationManager(popup.SpaceReservationManagerName);
            _agent = _manager.CreatePopupAgent(popup.PresentationSpan, popup.PopupStyles, popup.SurfaceElement);
            _manager.AgentChanged += OnAgentChanged;
            _manager.AddAgent(_agent);
        }
    }

    public void Dismiss()
    {
        if (_isDismissed)
        {
            return;
        }

        _isDismissed = true;
        if (_isStarted)
        {
            _view.TextBuffer.Changed -= OnBufferChanged;
            _view.Closed -= OnViewClosed;
            _view.LostAggregateFocus -= OnViewLostAggregateFocus;
            if (_trackCaret)
            {
                _view.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            SignatureHelpBroker.GetSessionList(_view).Remove(this);
        }

        if (_popupPresenter is { } popup)
        {
            popup.PresentationSpanChanged -= OnPresentationChanged;
            popup.PopupStylesChanged -= OnPopupStylesChanged;
            popup.SurfaceElementChanged -= OnPresentationChanged;
        }

        if (_manager is { } manager)
        {
            manager.AgentChanged -= OnAgentChanged;
            if (_agent is { } agent)
            {
                _agent = null;
                manager.RemoveAgent(agent);
            }
        }

        if (_sources is { } sources)
        {
            _sources = null;
            foreach (var source in sources)
            {
                source.Dispose();
            }
        }

        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    public void Recalculate()
    {
        if (_isDismissed)
        {
            throw new InvalidOperationException("The session is dismissed.");
        }

        // VS parity: recalculating a created-but-unstarted session starts it. Roslyn's
        // signature help presenter never calls Start — it creates the session, seeds its
        // source callback through session properties, and only ever recalculates.
        if (!_isStarted)
        {
            Start();
            return;
        }

        if (!Augment())
        {
            Dismiss();
            return;
        }

        Recalculated?.Invoke(this, EventArgs.Empty);
        UpdatePopup();
    }

    public bool Match()
    {
        if (_signatures.Count == 0)
        {
            return false;
        }

        // The highest-priority source that can determine a best match wins; sources that
        // return null defer to the next one (the contract's chain).
        foreach (var source in _sources ?? [])
        {
            if (source.GetBestMatch(this) is { } match && _signatures.Contains(match))
            {
                SelectedSignature = match;
                return true;
            }
        }

        return false;
    }

    public void Collapse() => Dismiss();

    /// <summary>Re-augments the signature list; false when no source contributed anything.</summary>
    private bool Augment()
    {
        var signatures = new List<ISignature>();
        foreach (var source in _sources ?? [])
        {
            source.AugmentSignatureHelpSession(this, signatures);
        }

        _signatures.Clear();
        foreach (var signature in signatures)
        {
            _signatures.Add(signature);
        }

        if (_signatures.Count == 0)
        {
            return false;
        }

        if (_selectedSignature is null || !_signatures.Contains(_selectedSignature))
        {
            _selectedSignature = null;
            if (!Match())
            {
                SelectedSignature = _signatures[0];
            }
        }

        return true;
    }

    private void UpdatePopup()
    {
        if (_popupPresenter is { } popup && _agent is { } agent && _manager is { } manager)
        {
            manager.UpdatePopupAgent(agent, popup.PresentationSpan, popup.PopupStyles);
        }
    }

    private void OnPresentationChanged(object? sender, EventArgs e) => UpdatePopup();

    private void OnPopupStylesChanged(object? sender, ValueChangedEventArgs<PopupStyles> e) => UpdatePopup();

    private void OnAgentChanged(object? sender, SpaceReservationAgentChangedEventArgs e)
    {
        if (!_isDismissed && _agent is not null && ReferenceEquals(e.OldAgent, _agent) && e.NewAgent is null)
        {
            _agent = null;
            Dismiss();
        }
    }

    private void OnBufferChanged(object? sender, TextContentChangedEventArgs e) => Recalculate();

    private void OnViewClosed(object? sender, EventArgs e) => Dismiss();

    private void OnViewLostAggregateFocus(object? sender, EventArgs e) => Dismiss();

    private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
    {
        if (_selectedSignature?.ApplicableToSpan is not { } applicableToSpan)
        {
            return;
        }

        var caret = _view.Caret.Position.BufferPosition;
        var span = applicableToSpan.GetSpan(caret.Snapshot);
        if (caret < span.Start || caret > span.End)
        {
            Dismiss();
        }
    }
}
