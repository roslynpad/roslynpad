using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace RoslynPad.Roslyn
{
    public static class DocumentExtensions
    {
        private static readonly MethodInfo _getLanguageService = typeof (DocumentExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(x => x.Name == nameof(GetLanguageService) && x.GetParameters().Length == 1);

        public static TLanguageService GetLanguageService<TLanguageService>(this Document document)
            where TLanguageService : class, ILanguageService
        {
            return document?.Project?.LanguageServices?.GetService<TLanguageService>();
        }

        public static object GetLanguageService(this Document document, Type languageServiceType)
        {
            return _getLanguageService.MakeGenericMethod(languageServiceType)
                .Invoke(null, new object[] { document });
        }
    }
}