using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace RoslynPad.Roslyn.CodeActions
{
    public static class CodeActionExtensions
    {
        public static bool HasCodeActions(this CodeAction codeAction)
        {
            if (codeAction == null) throw new ArgumentNullException(nameof(codeAction));

            return codeAction.HasCodeActions;
        }

        public static ImmutableArray<CodeAction> GetCodeActions(this CodeAction codeAction)
        {
            if (codeAction == null) throw new ArgumentNullException(nameof(codeAction));

            return codeAction.GetCodeActions();
        }

        public static int? GetGlyph(this CodeAction codeAction)
        {
            if (codeAction == null) throw new ArgumentNullException(nameof(codeAction));

            return codeAction.Glyph;
        }
    }
}