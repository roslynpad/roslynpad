using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.Diagnostics
{
    public static class SolutionCrawlerRegistrationService
    {
        private static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.SolutionCrawler.ISolutionCrawlerRegistrationService, Microsoft.CodeAnalysis.Features", throwOnError: true);

        public static void Register(Workspace workspace)
        {
            var service = workspace.Services.GetService(InterfaceType);
            var methodInfo = InterfaceType.GetMethod("Register");
            Debug.Assert(methodInfo != null, "methodInfo != null");
            methodInfo.Invoke(service, new object[] { workspace });
        }
    }
}