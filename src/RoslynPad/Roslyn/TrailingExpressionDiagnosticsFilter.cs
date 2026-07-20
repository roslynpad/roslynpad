using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.Build;

namespace RoslynPad.Roslyn;

/// <summary>
/// Replaces the default <see cref="IDiagnosticAnalyzerService"/> with one that hides the
/// diagnostics obsoleted by the execution-time Dump rewrite of a trailing bare expression
/// (<see cref="BuildCode.FindTrailingExpression"/>): the CS1002 for its missing semicolon and
/// the CA1806 for its unused result. Compiler errors are not suppressible by any Roslyn
/// configuration (not even a DiagnosticSuppressor), so the diagnostics are filtered at the
/// service every consumer (squiggles, inline diagnostics, light bulb) reads from.
/// </summary>
[ExportWorkspaceServiceFactory(typeof(IDiagnosticAnalyzerService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal sealed class TrailingExpressionDiagnosticsFilterFactory(
    IGlobalOptionService globalOptions,
    IDiagnosticsRefresher diagnosticsRefresher,
    [Import(AllowDefault = true)] IAsynchronousOperationListenerProvider? listenerProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) =>
        new TrailingExpressionDiagnosticsFilter(
            new DiagnosticAnalyzerService(globalOptions, diagnosticsRefresher, listenerProvider, workspaceServices.Workspace));
}

internal sealed class TrailingExpressionDiagnosticsFilter(IDiagnosticAnalyzerService inner) : IDiagnosticAnalyzerService
{
    private const string MissingSemicolonId = "CS1002";
    private const string IgnoredMethodResultId = "CA1806";

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        TextDocument document, TextSpan? range, DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority, DiagnosticKind diagnosticKind, CancellationToken cancellationToken)
    {
        var diagnostics = await inner.GetDiagnosticsForSpanAsync(
            document, range, diagnosticIdFilter, priority, diagnosticKind, cancellationToken).ConfigureAwait(false);

        if (document is not Document sourceDocument ||
            !diagnostics.Any(static d => d.Id is MissingSemicolonId or IgnoredMethodResultId))
        {
            return diagnostics;
        }

        var root = await sourceDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is not CompilationUnitSyntax compilationUnit ||
            BuildCode.FindTrailingExpression(compilationUnit)?.Statement is not ExpressionStatementSyntax statement)
        {
            return diagnostics;
        }

        var text = await sourceDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return [.. diagnostics.Where(d => GetSpan(d, text) is var span &&
            !(d.Id == MissingSemicolonId && span == statement.SemicolonToken.Span) &&
            !(d.Id == IgnoredMethodResultId && span == statement.Expression.Span))];
    }

    private static TextSpan GetSpan(DiagnosticData data, SourceText text) =>
        data.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text);

    public void RequestDiagnosticRefresh() => inner.RequestDiagnosticRefresh();

    public Task<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
        inner.ForceRunCodeAnalysisDiagnosticsAsync(project, cancellationToken);

    public Task<bool> IsAnyDiagnosticIdDeprioritizedAsync(Project project, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken) =>
        inner.IsAnyDiagnosticIdDeprioritizedAsync(project, diagnosticIds, cancellationToken);

    public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, ImmutableArray<DocumentId> documentIds, ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter, bool includeLocalDocumentDiagnostics, CancellationToken cancellationToken) =>
        inner.GetDiagnosticsForIdsAsync(project, documentIds, diagnosticIds, analyzerFilter, includeLocalDocumentDiagnostics, cancellationToken);

    public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds, AnalyzerFilter analyzerFilter, CancellationToken cancellationToken) =>
        inner.GetProjectDiagnosticsForIdsAsync(project, diagnosticIds, analyzerFilter, cancellationToken);

    public Task<ImmutableDictionary<ProjectId, ImmutableHashSet<string>>> GetAllDiagnosticIdsAsync(
        Solution solution, ImmutableArray<ProjectId> projectIds, CancellationToken cancellationToken) =>
        inner.GetAllDiagnosticIdsAsync(solution, projectIds, cancellationToken);

    public Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Solution solution, ProjectId? projectId, CancellationToken cancellationToken) =>
        inner.GetDiagnosticDescriptorsPerReferenceAsync(solution, projectId, cancellationToken);

    public Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        Solution solution, ProjectId projectId, AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken) =>
        inner.GetDiagnosticDescriptorsAsync(solution, projectId, analyzerReference, language, cancellationToken);

    public Task<ImmutableArray<string>> GetCompilationEndDiagnosticDescriptorIdsAsync(Solution solution, CancellationToken cancellationToken) =>
        inner.GetCompilationEndDiagnosticDescriptorIdsAsync(solution, cancellationToken);
}
