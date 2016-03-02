using System;
using System.Linq.Expressions;

namespace RoslynPad.Roslyn.Editor
{
    internal sealed class InlineRenameSession : IInlineRenameSession
    {
        private readonly Microsoft.CodeAnalysis.Editor.IInlineRenameSession _inner;

        public InlineRenameSession(Microsoft.CodeAnalysis.Editor.IInlineRenameSession inner)
        {
            _inner = inner;
        }

        public void Cancel()
        {
            _inner.Cancel();
        }

        public void Commit(bool previewChanges = false)
        {
            _inner.Commit(previewChanges);
        }
    }
}