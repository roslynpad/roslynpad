using System;
using System.Collections.Immutable;
using Castle.Core.Interceptor;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn
{
    internal sealed class DocumentTrackingServiceProxy : IInterceptor
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.IDocumentTrackingService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        internal static readonly Lazy<Type> GeneratedType = new Lazy<Type>(() => RoslynInterfaceProxy.GenerateFor(InterfaceType, isWorkspaceService: true));

        void IInterceptor.Intercept(IInvocation invocation)
        {
            var workspace = invocation.Proxy.GetFieldValue<RoslynWorkspace>(RoslynInterfaceProxy.WorkspaceField);
            switch (invocation.Method.Name)
            {
                case "GetActiveDocument":
                    invocation.ReturnValue = workspace.OpenDocumentId;
                    break;
                case "GetVisibleDocuments":
                    invocation.ReturnValue = ImmutableArray.Create(workspace.OpenDocumentId);
                    break;
            }
        }
    }
}