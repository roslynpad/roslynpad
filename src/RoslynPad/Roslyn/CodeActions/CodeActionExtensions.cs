using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeActions;

namespace RoslynPad.Roslyn.CodeActions
{
    public static class CodeActionExtensions
    {
        private static readonly Func<CodeAction, bool> _hasCodeActions = CreateHasCodeActions();

        private static Func<CodeAction, bool> CreateHasCodeActions()
        {
            var p = Expression.Parameter(typeof (CodeAction));
            return Expression.Lambda<Func<CodeAction, bool>>(
                Expression.Property(p, nameof(HasCodeActions)), p).Compile();
        }

        private static readonly Func<CodeAction, ImmutableArray<CodeAction>> _getCodeActions = CreateGetCodeActions();

        private static Func<CodeAction, ImmutableArray<CodeAction>> CreateGetCodeActions()
        {
            var p = Expression.Parameter(typeof(CodeAction));
            var method = typeof (CodeAction).GetMethod(nameof(GetCodeActions),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return Expression.Lambda<Func<CodeAction, ImmutableArray<CodeAction>>>(
                Expression.Call(p, method), p).Compile();
        }

        public static bool HasCodeActions(this CodeAction codeAction)
        {
            if (codeAction == null) throw new ArgumentNullException(nameof(codeAction));

            return _hasCodeActions(codeAction);
        }

        public static ImmutableArray<CodeAction> GetCodeActions(this CodeAction codeAction)
        {
            if (codeAction == null) throw new ArgumentNullException(nameof(codeAction));

            return _getCodeActions(codeAction);
        }
    }
}