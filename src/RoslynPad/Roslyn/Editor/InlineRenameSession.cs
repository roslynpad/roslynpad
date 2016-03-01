using System;
using System.Linq.Expressions;

namespace RoslynPad.Roslyn.Editor
{
    internal sealed class InlineRenameSession : IInlineRenameSession
    {
        private static readonly Type InterfaceType = Type.GetType("Microsoft.CodeAnalysis.Editor.IInlineRenameSession, Microsoft.CodeAnalysis.EditorFeatures", throwOnError: true);

        private readonly object _inner;

        public InlineRenameSession(object inner)
        {
            _inner = inner;
        }

        private static readonly Action<object> _cancel = CreateCancel();

        private static Action<object> CreateCancel()
        {
            var p = Expression.Parameter(typeof(object));

            return Expression.Lambda<Action<object>>(
                Expression.Call(Expression.Convert(p, InterfaceType), InterfaceType.GetMethod(nameof(Cancel)))
                , p).Compile();
        }

        public void Cancel()
        {
            _cancel(_inner);
        }

        private static readonly Action<object, bool> _commit = CreateCommit();

        private static Action<object, bool> CreateCommit()
        {
            var p = new[]
            {
                Expression.Parameter(typeof(object)),
                Expression.Parameter(typeof(bool)),
            };

            return Expression.Lambda<Action<object, bool>>(
                Expression.Call(Expression.Convert(p[0], InterfaceType), InterfaceType.GetMethod(nameof(Commit)), p[1])
                , p).Compile();
        }

        public void Commit(bool previewChanges = false)
        {
            _commit(_inner, previewChanges);
        }
    }
}