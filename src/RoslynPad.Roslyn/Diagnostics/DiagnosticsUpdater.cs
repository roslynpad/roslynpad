using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace RoslynPad.Roslyn.Diagnostics;

public class DiagnosticsUpdater : IDiagnosticsUpdater, IDisposable
{
    private readonly Workspace _workspace;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
    private readonly object _lock = new();
    private readonly AsyncBatchingWorkQueue<DocumentId> _workQueue;
    private readonly CancellationTokenSource _cts;

    private HashSet<DiagnosticData> _currentDiagnostics;

    public ImmutableHashSet<string> DisabledDiagnostics { get; set; } = [];

    [ExportWorkspaceServiceFactory(typeof(IDiagnosticsUpdater))]
    [method: ImportingConstructor]
    internal class Factory(IDiagnosticAnalyzerService diagnosticAnalyzerService) : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DiagnosticsUpdater(workspaceServices.Workspace, diagnosticAnalyzerService);
        }
    }

    [ImportingConstructor]
    public DiagnosticsUpdater(Workspace workspace, IDiagnosticAnalyzerService diagnosticAnalyzerService)
    {
        workspace.DocumentOpened += OnDocumentOpened;
        workspace.DocumentActiveContextChanged += OnDocumentActiveContextChanged;
        workspace.WorkspaceChanged += OnWorkspaceChanged;
        foreach (var document in workspace.CurrentSolution.Projects.SelectMany(p => p.Documents))
        {
            ConnectDocument(document);
        }

        _workspace = workspace;
        _diagnosticAnalyzerService = diagnosticAnalyzerService;
        _currentDiagnostics = [];
        _cts = new CancellationTokenSource();

        _workQueue = new AsyncBatchingWorkQueue<DocumentId>(DelayTimeSpan.Short, ProcessWorkQueueAsync, new AsynchronousOperationListener(), _cts.Token);
    }

    private async ValueTask ProcessWorkQueueAsync(ImmutableSegmentedList<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        foreach (var documentId in documentIds)
        {
            if (await _workspace.CurrentSolution.GetDocumentAsync(documentId, cancellationToken: cancellationToken).ConfigureAwait(false) is { } document)
            {
                await UpdateDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        _workspace.DocumentOpened -= OnDocumentOpened;
        _workspace.DocumentActiveContextChanged -= OnDocumentActiveContextChanged;
        _workspace.WorkspaceChanged -= OnWorkspaceChanged;
        _cts.Cancel();
    }

    private void OnDocumentOpened(object? sender, DocumentEventArgs args) => ConnectDocument(args.Document);

    private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs e) => _workQueue.AddWork(e.NewActiveContextDocumentId);

    private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        if (e.DocumentId is { } documentId)
        {
            _workQueue.AddWork(documentId);
        }
    }

    private void ConnectDocument(Document document)
    {
        if (document.TryGetText(out var text))
        {
            text.Container.TextChanged += (o, e) => _workQueue.AddWork(document.Id);
        }

        _workQueue.AddWork(document.Id);
    }

    private async Task UpdateDiagnosticsAsync(Document document, CancellationToken cancellationToken)
    {
        var diagnostics = await GetDiagnostics(document, cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
            var addedDiagnostics = diagnostics.Where(d => !_currentDiagnostics.Contains(d) && !DisabledDiagnostics.Contains(d.Id)).ToHashSet();
            _currentDiagnostics.ExceptWith(diagnostics);
            var removedDiagnostics = _currentDiagnostics;

            _currentDiagnostics = [];
            foreach (var diag in diagnostics)
            {
                _currentDiagnostics.Add(diag);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (addedDiagnostics.Count > 0 || removedDiagnostics.Count > 0)
            {
                DiagnosticsChanged?.Invoke(new DiagnosticsChangedArgs(document.Id, addedDiagnostics, removedDiagnostics));
            }
        }
    }

    private async Task<ImmutableArray<DiagnosticData>> GetDiagnostics(Document document, CancellationToken cancellationToken)
    {
        try
        {
            return await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return [];
        }
    }

    public event Action<DiagnosticsChangedArgs>? DiagnosticsChanged;
}
