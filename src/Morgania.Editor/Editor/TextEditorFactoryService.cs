#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

[Export(typeof(ITextEditorFactoryService))]
[Shared]
public sealed class TextEditorFactoryService : ITextEditorFactoryService
{
    private readonly ITextBufferFactoryService _textBufferFactory;
    private readonly IEditorOptionsFactoryService _editorOptionsFactory;
    private readonly IBufferGraphFactoryService _bufferGraphFactory;
    private readonly IViewClassifierAggregatorService _viewClassifierAggregator;
    private readonly IClassificationFormatMapService _classificationFormatMapService;
    private readonly IEditorFormatMapService _editorFormatMapService;
    private readonly IMultiSelectionBrokerFactory _multiSelectionBrokerFactory;
    private readonly IEditorOperationsFactoryService _editorOperationsFactory;
    private readonly ITextBufferUndoManagerProvider _undoManagerProvider;
    private readonly ITextStructureNavigatorSelectorService _navigatorSelectorService;
    private readonly ITextAndAdornmentSequencerFactoryService _sequencerFactory;
    private readonly Lazy<IWpfTextViewCreationListener, ContentTypeAndTextViewRoleMetadata>[] _creationListeners;
    private readonly Lazy<ITextViewCreationListener, ContentTypeAndTextViewRoleMetadata>[] _textViewCreationListeners;
    private readonly Lazy<ITextViewConnectionListener, ContentTypeAndTextViewRoleMetadata>[] _connectionListeners;
    private readonly Lazy<IWpfTextViewMarginProvider, MarginProviderMetadata>[] _marginProviders;
    private readonly Lazy<ILineTransformSourceProvider, ContentTypeAndTextViewRoleMetadata>[] _lineTransformSourceProviders;
    private readonly Lazy<ITextViewModelProvider, ContentTypeAndTextViewRoleMetadata>[] _viewModelProviders;
    private readonly IProjectionBufferFactoryService _projectionBufferFactory;
    private readonly Dictionary<string, int> _adornmentLayerRanks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _spaceReservationManagerRanks = new(StringComparer.OrdinalIgnoreCase);

    [ImportingConstructor]
    public TextEditorFactoryService(
        IProjectionBufferFactoryService projectionBufferFactory,
        ITextBufferFactoryService textBufferFactory,
        IEditorOptionsFactoryService editorOptionsFactory,
        IBufferGraphFactoryService bufferGraphFactory,
        IViewClassifierAggregatorService viewClassifierAggregator,
        IClassificationFormatMapService classificationFormatMapService,
        IEditorFormatMapService editorFormatMapService,
        IMultiSelectionBrokerFactory multiSelectionBrokerFactory,
        IEditorOperationsFactoryService editorOperationsFactory,
        ITextBufferUndoManagerProvider undoManagerProvider,
        ITextAndAdornmentSequencerFactoryService sequencerFactory,
        ITextStructureNavigatorSelectorService navigatorSelectorService,
        [ImportMany] Lazy<AdornmentLayerDefinition, Orderable>[] adornmentLayerDefinitions,
        [ImportMany] Lazy<SpaceReservationManagerDefinition, Orderable>[] spaceReservationManagerDefinitions,
        [ImportMany] Lazy<IWpfTextViewCreationListener, ContentTypeAndTextViewRoleMetadata>[] creationListeners,
        [ImportMany] Lazy<ITextViewCreationListener, ContentTypeAndTextViewRoleMetadata>[] textViewCreationListeners,
        [ImportMany] Lazy<ITextViewConnectionListener, ContentTypeAndTextViewRoleMetadata>[] connectionListeners,
        [ImportMany] Lazy<IWpfTextViewMarginProvider, MarginProviderMetadata>[] marginProviders,
        [ImportMany] Lazy<ILineTransformSourceProvider, ContentTypeAndTextViewRoleMetadata>[] lineTransformSourceProviders,
        [ImportMany] Lazy<ITextViewModelProvider, ContentTypeAndTextViewRoleMetadata>[] viewModelProviders)
    {
        _sequencerFactory = sequencerFactory;
        _creationListeners = creationListeners;
        _textViewCreationListeners = textViewCreationListeners;
        _connectionListeners = connectionListeners;
        _marginProviders = marginProviders;
        _lineTransformSourceProviders = lineTransformSourceProviders;
        _viewModelProviders = viewModelProviders;
        _projectionBufferFactory = projectionBufferFactory;
        ArgumentNullException.ThrowIfNull(adornmentLayerDefinitions);
        _textBufferFactory = textBufferFactory;
        _editorOptionsFactory = editorOptionsFactory;
        _bufferGraphFactory = bufferGraphFactory;
        _viewClassifierAggregator = viewClassifierAggregator;
        _classificationFormatMapService = classificationFormatMapService;
        _editorFormatMapService = editorFormatMapService;
        _multiSelectionBrokerFactory = multiSelectionBrokerFactory;
        _editorOperationsFactory = editorOperationsFactory;
        _undoManagerProvider = undoManagerProvider;
        _navigatorSelectorService = navigatorSelectorService;

        var ordered = Orderer.Order(adornmentLayerDefinitions.ToList());
        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Metadata.Name is { } name)
            {
                _adornmentLayerRanks.TryAdd(name, i);
            }
        }

        var orderedManagers = Orderer.Order(spaceReservationManagerDefinitions.ToList());
        for (int i = 0; i < orderedManagers.Count; i++)
        {
            if (orderedManagers[i].Metadata.Name is { } name)
            {
                _spaceReservationManagerRanks.TryAdd(name, i);
            }
        }
    }

    public event EventHandler<TextViewCreatedEventArgs>? TextViewCreated;

    public ITextViewRoleSet NoRoles => new TextViewRoleSet([]);

    public ITextViewRoleSet DefaultRoles => CreateTextViewRoleSet(
        PredefinedTextViewRoles.Analyzable,
        PredefinedTextViewRoles.Document,
        PredefinedTextViewRoles.Editable,
        PredefinedTextViewRoles.Interactive,
        PredefinedTextViewRoles.Structured,
        PredefinedTextViewRoles.Zoomable);

    public ITextViewRoleSet AllPredefinedRoles => CreateTextViewRoleSet(
        PredefinedTextViewRoles.Analyzable,
        PredefinedTextViewRoles.CodeDefinitionView,
        PredefinedTextViewRoles.Debuggable,
        PredefinedTextViewRoles.Document,
        PredefinedTextViewRoles.Editable,
        PredefinedTextViewRoles.EmbeddedPeekTextView,
        PredefinedTextViewRoles.Interactive,
        PredefinedTextViewRoles.PreviewTextView,
        PredefinedTextViewRoles.PrimaryDocument,
        PredefinedTextViewRoles.Printable,
        PredefinedTextViewRoles.Structured,
        PredefinedTextViewRoles.Zoomable);

    public ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles) => new TextViewRoleSet(roles);

    public ITextViewRoleSet CreateTextViewRoleSet(params string[] roles) => new TextViewRoleSet(roles);

    public IWpfTextView CreateTextView()
        => CreateTextView(_textBufferFactory.CreateTextBuffer());

    public IWpfTextView CreateTextView(ITextBuffer textBuffer)
        => CreateTextView(textBuffer, DefaultRoles);

    public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles)
        => CreateTextView(textBuffer, roles, _editorOptionsFactory.GlobalOptions);

    public IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        return CreateTextView(new VacuousTextDataModel(textBuffer), roles, parentOptions);
    }

    public IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions)
    {
        ArgumentNullException.ThrowIfNull(dataModel);
        ArgumentNullException.ThrowIfNull(roles);

        // The first matching view model provider wins (elision/projection view models
        // arrive through here); with none, the view renders the data model directly.
        var contentType = dataModel.ContentType;
        foreach (var provider in _viewModelProviders)
        {
            bool contentTypeMatches = provider.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            bool rolesMatch = provider.Metadata.TextViewRoles?.Any() != true || roles.ContainsAny(provider.Metadata.TextViewRoles);
            if (contentTypeMatches && rolesMatch && provider.Value.CreateTextViewModel(dataModel, roles) is { } viewModel)
            {
                return CreateTextView(viewModel, roles, parentOptions);
            }
        }

        // Built-in default: Structured views get an elision buffer as their visual buffer
        // (the projection seam that outlining collapse drives); others render the
        // document buffer directly.
        if (roles.Contains(PredefinedTextViewRoles.Structured))
        {
            var documentSnapshot = dataModel.DocumentBuffer.CurrentSnapshot;
            var elisionBuffer = _projectionBufferFactory.CreateElisionBuffer(
                projectionEditResolver: null,
                new NormalizedSnapshotSpanCollection(new SnapshotSpan(documentSnapshot, 0, documentSnapshot.Length)),
                ElisionBufferOptions.None);
            return CreateTextView(new ElisionTextViewModel(dataModel, elisionBuffer), roles, parentOptions);
        }

        return CreateTextView(new VacuousTextViewModel(dataModel), roles, parentOptions);
    }

    public IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(parentOptions);

        var appearanceCategory = parentOptions.GetOptionValue(DefaultTextViewOptions.AppearanceCategory);
        var view = new WpfTextView(
            this,
            viewModel,
            roles,
            _editorOptionsFactory,
            parentOptions,
            classifier: null,
            _classificationFormatMapService.GetClassificationFormatMap(appearanceCategory),
            _editorFormatMapService.GetEditorFormatMap(appearanceCategory),
            _bufferGraphFactory.CreateBufferGraph(viewModel.VisualBuffer));

        // The classifier aggregator needs a functioning view, so it attaches after construction.
        view.SetClassifier(_viewClassifierAggregator.GetClassifier(view));

        // Undo is a host responsibility; attaching the manager creates the buffer's undo
        // history, which the editor operations record their transactions into.
        _undoManagerProvider.GetTextBufferUndoManager(viewModel.EditBuffer);

        // Creation listeners scoped by content type and role — the standard extensibility
        // hook (adornment providers and other per-view components attach here).
        var contentType = viewModel.EditBuffer.ContentType;
        foreach (var listener in _creationListeners)
        {
            bool contentTypeMatches = listener.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            bool rolesMatch = listener.Metadata.TextViewRoles?.Any() != true || roles.ContainsAny(listener.Metadata.TextViewRoles);
            if (contentTypeMatches && rolesMatch)
            {
                listener.Value.TextViewCreated(view);
            }
        }

        // Non-Wpf creation listeners are a separate contract with the same scoping —
        // the vendored Quick Info controller and brace completion attach through it.
        foreach (var listener in _textViewCreationListeners)
        {
            bool contentTypeMatches = listener.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            bool rolesMatch = listener.Metadata.TextViewRoles?.Any() != true || roles.ContainsAny(listener.Metadata.TextViewRoles);
            if (contentTypeMatches && rolesMatch)
            {
                listener.Value.TextViewCreated(view);
            }
        }

        // Connection listeners have the same scoping but are keyed to the *subject buffers*
        // whose content type matches (Roslyn's TextBufferAssociatedViewService tracks
        // buffer↔view association through these events). Morgania buffer graphs are fixed for
        // the view's lifetime, so buffers connect at creation and disconnect at close.
        foreach (var listener in _connectionListeners)
        {
            bool rolesMatch = listener.Metadata.TextViewRoles?.Any() != true || roles.ContainsAny(listener.Metadata.TextViewRoles);
            if (!rolesMatch)
            {
                continue;
            }

            var subjectBuffers = view.BufferGraph.GetTextBuffers(
                buffer => listener.Metadata.ContentTypes?.Any(buffer.ContentType.IsOfType) != false);
            if (subjectBuffers.Count == 0)
            {
                continue;
            }

            // Subscribe the disconnect before notifying the connect: listeners may add their own
            // Closed handlers during SubjectBuffersConnected that assume disconnection already ran
            // (event handlers fire in subscription order).
            view.Closed += (_, _) => listener.Value.SubjectBuffersDisconnected(view, ConnectionReason.TextViewLifetime, subjectBuffers);
            listener.Value.SubjectBuffersConnected(view, ConnectionReason.TextViewLifetime, subjectBuffers);
        }

        TextViewCreated?.Invoke(this, new TextViewCreatedEventArgs(view));
        return view;
    }

    internal int TextLayerRank => GetAdornmentLayerRank(PredefinedAdornmentLayers.Text);

    internal IMultiSelectionBroker CreateMultiSelectionBroker(ITextView textView)
        => _multiSelectionBrokerFactory.CreateBroker(textView);

    internal ITextAndAdornmentSequencer CreateSequencer(ITextView textView)
        => _sequencerFactory.Create(textView);

    /// <summary>
    /// Creates the view's aggregate line transform source from the matching provider
    /// exports (scoped by content type and roles), or null if none participate.
    /// </summary>
    internal ILineTransformSource? CreateLineTransformSource(WpfTextView view)
    {
        var contentType = view.TextViewModel.EditBuffer.ContentType;
        List<ILineTransformSource>? sources = null;
        foreach (var provider in _lineTransformSourceProviders)
        {
            bool contentTypeMatches = provider.Metadata.ContentTypes?.Any(contentType.IsOfType) != false;
            bool rolesMatch = provider.Metadata.TextViewRoles?.Any() != true || view.Roles.ContainsAny(provider.Metadata.TextViewRoles);
            if (contentTypeMatches && rolesMatch && provider.Value.Create(view) is { } source)
            {
                (sources ??= []).Add(source);
            }
        }

        return sources switch
        {
            null => null,
            [var single] => single,
            _ => new AggregateLineTransformSource([.. sources]),
        };
    }

    private sealed class AggregateLineTransformSource(ILineTransformSource[] sources) : ILineTransformSource
    {
        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            var transform = sources[0].GetLineTransform(line, yPosition, placement);
            for (int i = 1; i < sources.Length; i++)
            {
                transform = LineTransform.Combine(transform, sources[i].GetLineTransform(line, yPosition, placement));
            }

            return transform;
        }
    }

    internal IEditorOperations GetEditorOperations(ITextView textView)
        => _editorOperationsFactory.GetEditorOperations(textView);

    internal ITextBufferUndoManager GetUndoManager(ITextBuffer textBuffer)
        => _undoManagerProvider.GetTextBufferUndoManager(textBuffer);

    internal ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer)
        => _navigatorSelectorService.GetTextStructureNavigator(textBuffer);

    public IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus)
    {
        ArgumentNullException.ThrowIfNull(wpfTextView);
        return new WpfTextViewHost(wpfTextView, setFocus, _marginProviders);
    }

    internal bool IsAdornmentLayerDefined(string name) => _adornmentLayerRanks.ContainsKey(name);

    internal int GetAdornmentLayerRank(string name)
        => _adornmentLayerRanks.TryGetValue(name, out int rank) ? rank : int.MaxValue;

    internal bool IsSpaceReservationManagerDefined(string name) => _spaceReservationManagerRanks.ContainsKey(name);

    internal int GetSpaceReservationManagerRank(string name)
        => _spaceReservationManagerRanks.TryGetValue(name, out int rank) ? rank : int.MaxValue;
}
