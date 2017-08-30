using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynPad.Roslyn.Diagnostics
{
    internal interface ISyntaxChangeNotificationService
    {
        event EventHandler<Document> OpenedDocumentSyntaxChanged;
    }

    [Export(typeof(ISyntaxChangeNotificationService)), Shared]
    [ExportIncrementalAnalyzerProvider(nameof(SyntaxChangeNotificationService), workspaceKinds: null)]
    internal class SyntaxChangeNotificationService : ISyntaxChangeNotificationService, IIncrementalAnalyzerProvider
    {
        public event EventHandler<Document> OpenedDocumentSyntaxChanged;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new NotificationService(this);
        }

        private void RaiseOpenDocumentSyntaxChangedEvent(Document document)
        {
            OpenedDocumentSyntaxChanged?.Invoke(this, document);
        }

        private class NotificationService : IIncrementalAnalyzer
        {
            private readonly SyntaxChangeNotificationService _owner;

            public NotificationService(SyntaxChangeNotificationService owner)
            {
                _owner = owner;
            }

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                _owner.RaiseOpenDocumentSyntaxChangedEvent(document);
                return Task.CompletedTask;
            }

            #region unused 

            public void RemoveDocument(DocumentId documentId)
            {
            }

            public void RemoveProject(ProjectId projectId)
            {
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e) => false;

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken) => Task.CompletedTask;

            #endregion
        }
    }
}