using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace RoslynPad.Roslyn.WorkspaceServices
{
    [ExportWorkspaceService(typeof(IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService)), Shared]
    internal sealed class AddMetadataReferenceCodeActionOperationFactoryWorkspaceService : IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService
    {
        public CodeActionOperation CreateAddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
        {
            return new AddMetadataReferenceOperation(projectId, assemblyIdentity);
        }

        private class AddMetadataReferenceOperation : CodeActionOperation
        {
            private readonly AssemblyIdentity _assemblyIdentity;
            private readonly ProjectId _projectId;

            public AddMetadataReferenceOperation(ProjectId projectId, AssemblyIdentity assemblyIdentity)
            {
                _projectId = projectId;
                _assemblyIdentity = assemblyIdentity;
            }

            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            {
                var roslynPadWorkspace = workspace as RoslynWorkspace;
                roslynPadWorkspace?.RoslynHost?.AddMetadataReference(_projectId, _assemblyIdentity);
            }

            public override string Title => "Add Reference";
        }
    }
}