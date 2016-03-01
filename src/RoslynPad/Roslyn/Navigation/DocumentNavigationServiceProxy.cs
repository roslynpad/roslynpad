using System;
using Castle.Core.Interceptor;

namespace RoslynPad.Roslyn.Navigation
{
    internal sealed class DocumentNavigationServiceProxy : IInterceptor
    {
        internal static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Navigation.IDocumentNavigationService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        internal static readonly Lazy<Type> GeneratedType = new Lazy<Type>(() => RoslynInterfaceProxy.GenerateFor(InterfaceType, isWorkspaceService: true));

        void IInterceptor.Intercept(IInvocation invocation)
        {
            invocation.ReturnValue = true;
        }
    }
}