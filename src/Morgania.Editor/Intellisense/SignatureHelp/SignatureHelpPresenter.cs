#nullable enable

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation;

using System.Composition;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

using TextSpan = Microsoft.VisualStudio.Text.Span;

[Export(typeof(IIntellisensePresenterProvider))]
[Name("default signature help presenter")]
[ContentType("any")]
[Order]
public sealed class DefaultSignatureHelpPresenterProvider : IIntellisensePresenterProvider
{
    private readonly IContentTypeRegistryService _contentTypeRegistry;
    private readonly ITextBufferFactoryService _bufferFactory;
    private readonly IClassifierAggregatorService _classifierAggregator;
    private readonly IClassificationFormatMapService _classificationFormatMaps;
    private readonly IEditorFormatMapService _editorFormatMaps;

    [ImportingConstructor]
    public DefaultSignatureHelpPresenterProvider(
        IContentTypeRegistryService contentTypeRegistry,
        ITextBufferFactoryService bufferFactory,
        IClassifierAggregatorService classifierAggregator,
        IClassificationFormatMapService classificationFormatMaps,
        IEditorFormatMapService editorFormatMaps)
    {
        _contentTypeRegistry = contentTypeRegistry;
        _bufferFactory = bufferFactory;
        _classifierAggregator = classifierAggregator;
        _classificationFormatMaps = classificationFormatMaps;
        _editorFormatMaps = editorFormatMaps;
    }

    public IIntellisensePresenter? TryCreateIntellisensePresenter(IIntellisenseSession session)
        => session is ISignatureHelpSession signatureHelpSession
            ? new SignatureHelpPresenter(
                signatureHelpSession,
                _contentTypeRegistry,
                _bufferFactory,
                _classifierAggregator,
                _classificationFormatMaps,
                PopupBrushes.Read(_editorFormatMaps.GetEditorFormatMap(signatureHelpSession.TextView)))
            : null;
}

/// <summary>
/// The default signature help popup: the selected signature with its current parameter
/// bolded, an "N of M" overload indicator, and the signature/parameter documentation.
/// Positioned above the applicable span (flipping below when there's no room), through the
/// "signaturehelp" space reservation manager so it never overlaps the completion popup.
/// </summary>
internal sealed class SignatureHelpPresenter : IPopupIntellisensePresenter
{
    private readonly PopupBrushes _brushes;
    private readonly ISignatureHelpSession _session;
    private readonly Border _container;
    private readonly TextBlock _signatureBlock = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _documentationBlock = new() { TextWrapping = TextWrapping.Wrap, FontStyle = FontStyle.Italic };
    private readonly TextBlock _parameterBlock = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ITextBuffer? _signatureBuffer;
    private readonly IClassifier? _classifier;
    private readonly IClassificationFormatMap? _formatMap;
    private ISignature? _observedSignature;

    public SignatureHelpPresenter(
        ISignatureHelpSession session,
        IContentTypeRegistryService contentTypeRegistry,
        ITextBufferFactoryService bufferFactory,
        IClassifierAggregatorService classifierAggregator,
        IClassificationFormatMapService classificationFormatMaps,
        PopupBrushes brushes)
    {
        _brushes = brushes;
        _session = session;

        // Signature content is classified through a side buffer of the language's signature
        // help content type ("{content type} Signature Help", based on "sighelp"), the same
        // contract the VS presenter uses; language services export classifiers for it that
        // read the session from the buffer's properties. Without a registered content type
        // the content renders unclassified.
        var subjectContentType = session.TextView.TextBuffer.ContentType;
        if (contentTypeRegistry.GetContentType(subjectContentType.TypeName + " Signature Help") is { } signatureContentType)
        {
            _signatureBuffer = bufferFactory.CreateTextBuffer(string.Empty, signatureContentType);
            _signatureBuffer.Properties.AddProperty(typeof(ISignatureHelpSession), session);
            _classifier = classifierAggregator.GetClassifier(_signatureBuffer);
            _formatMap = classificationFormatMaps.GetClassificationFormatMap(session.TextView);
        }
        var panel = new StackPanel { Spacing = 2.0 };
        panel.Children.Add(_signatureBlock);
        panel.Children.Add(_documentationBlock);
        panel.Children.Add(_parameterBlock);
        _container = new Border
        {
            Child = panel,
            Background = brushes.Background,
            BorderBrush = brushes.BorderBrush,
            BorderThickness = new Thickness(1.0),
            CornerRadius = new CornerRadius(3.0),
            Padding = new Thickness(8.0, 5.0),
            MaxWidth = 600.0,
        };
        _container.SetValue(TextElement.ForegroundProperty, brushes.Foreground);

        session.SelectedSignatureChanged += OnSelectedSignatureChanged;
        session.Recalculated += OnRecalculated;
        session.Dismissed += OnSessionDismissed;
        ObserveSignature(session.IsDismissed ? null : SafeSelectedSignature());
        Render();
    }

    public IIntellisenseSession Session => _session;

    public Control SurfaceElement => _container;

    public event EventHandler? SurfaceElementChanged
    {
        add { }
        remove { }
    }

    public ITrackingSpan PresentationSpan
    {
        get
        {
            if (SafeSelectedSignature()?.ApplicableToSpan is { } applicableToSpan)
            {
                return applicableToSpan;
            }

            var snapshot = _session.TextView.TextSnapshot;
            int position = _session.GetTriggerPoint(snapshot)?.Position ?? 0;
            return snapshot.CreateTrackingSpan(position, 0, SpanTrackingMode.EdgeInclusive);
        }
    }

    public event EventHandler? PresentationSpanChanged;

    public PopupStyles PopupStyles => PopupStyles.PreferLeftOrTopPosition;

    public event EventHandler<ValueChangedEventArgs<PopupStyles>>? PopupStylesChanged
    {
        add { }
        remove { }
    }

    public string SpaceReservationManagerName => IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName;

    public double Opacity
    {
        get => _container.Opacity;
        set => _container.Opacity = value;
    }

    private ISignature? SafeSelectedSignature()
        => _session.Signatures.Count > 0 ? _session.SelectedSignature : null;

    private void OnSelectedSignatureChanged(object? sender, SelectedSignatureChangedEventArgs e)
    {
        ObserveSignature(e.NewSelectedSignature);
        Render();
        PresentationSpanChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnRecalculated(object? sender, EventArgs e)
    {
        ObserveSignature(SafeSelectedSignature());
        Render();
    }

    private void OnSessionDismissed(object? sender, EventArgs e) => ObserveSignature(null);

    private void ObserveSignature(ISignature? signature)
    {
        if (_observedSignature is { } previous)
        {
            previous.CurrentParameterChanged -= OnCurrentParameterChanged;
        }

        _observedSignature = signature;
        if (signature is not null)
        {
            signature.CurrentParameterChanged += OnCurrentParameterChanged;
        }
    }

    private void OnCurrentParameterChanged(object? sender, CurrentParameterChangedEventArgs e) => Render();

    private void Render()
    {
        var signature = SafeSelectedSignature();
        if (signature is null)
        {
            return;
        }

        var inlines = _signatureBlock.Inlines!;
        inlines.Clear();
        if (_session.Signatures.Count > 1)
        {
            int index = _session.Signatures.IndexOf(signature);
            inlines.Add(new Run($"▲ {index + 1} of {_session.Signatures.Count} ▼  ") { Foreground = _brushes.DeemphasizedForeground });
        }

        string content = signature.Content ?? string.Empty;
        var locus = signature.CurrentParameter?.Locus;
        TextSpan? currentParameterSpan = locus is { } span && span.Start >= 0 && span.End <= content.Length && span.Length > 0
            ? span
            : null;
        AppendContentRuns(inlines, content, currentParameterSpan);

        _documentationBlock.Text = signature.Documentation;
        _documentationBlock.IsVisible = !string.IsNullOrEmpty(signature.Documentation);
        _documentationBlock.Foreground = _brushes.DeemphasizedForeground;

        if (signature.CurrentParameter is { } parameter && !string.IsNullOrEmpty(parameter.Documentation))
        {
            var parameterInlines = _parameterBlock.Inlines!;
            parameterInlines.Clear();
            parameterInlines.Add(new Run(parameter.Name + ": ") { FontWeight = FontWeight.Bold });
            parameterInlines.Add(new Run(parameter.Documentation) { Foreground = _brushes.DeemphasizedForeground });
            _parameterBlock.IsVisible = true;
        }
        else
        {
            _parameterBlock.IsVisible = false;
        }
    }

    /// <summary>
    /// Emits the signature content as runs colored by the language's signature help classifier
    /// (plain when no classifier contributes), bolding the current parameter's locus.
    /// </summary>
    private void AppendContentRuns(InlineCollection inlines, string content, TextSpan? currentParameterSpan)
    {
        var classified = ClassifyContent(content);

        var boundaries = new SortedSet<int> { 0, content.Length };
        if (currentParameterSpan is { } bold)
        {
            boundaries.Add(bold.Start);
            boundaries.Add(bold.End);
        }

        foreach (var (span, _) in classified)
        {
            boundaries.Add(span.Start);
            boundaries.Add(span.End);
        }

        var points = boundaries.ToList();
        for (int i = 0; i + 1 < points.Count; i++)
        {
            int start = points[i];
            int end = points[i + 1];
            var run = new Run(content[start..end]);
            foreach (var (span, brush) in classified)
            {
                if (span.Contains(start))
                {
                    run.Foreground = brush;
                    break;
                }
            }

            if (currentParameterSpan is { } parameterSpan && start >= parameterSpan.Start && start < parameterSpan.End)
            {
                run.FontWeight = FontWeight.Bold;
            }

            inlines.Add(run);
        }
    }

    /// <summary>
    /// Classifies the signature content through the side buffer. The buffer's text must equal
    /// the selected signature's content when the classifier runs — that is the contract
    /// signature help classifiers key on.
    /// </summary>
    private List<(TextSpan Span, IBrush Brush)> ClassifyContent(string content)
    {
        var classified = new List<(TextSpan, IBrush)>();
        if (_signatureBuffer is null || _classifier is null || _formatMap is null || content.Length == 0)
        {
            return classified;
        }

        var snapshot = _signatureBuffer.CurrentSnapshot;
        if (!string.Equals(snapshot.GetText(), content, StringComparison.Ordinal))
        {
            snapshot = _signatureBuffer.Replace(new TextSpan(0, snapshot.Length), content);
        }

        foreach (var classificationSpan in _classifier.GetClassificationSpans(new SnapshotSpan(snapshot, 0, snapshot.Length)))
        {
            var properties = _formatMap.GetTextProperties(classificationSpan.ClassificationType);
            if (!properties.ForegroundBrushEmpty)
            {
                classified.Add((classificationSpan.Span.Span, properties.ForegroundBrush));
            }
        }

        return classified;
    }
}
