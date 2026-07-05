using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    [Export(typeof(ICommandingTextBufferResolverProvider))]
    [ContentType("any")]
    [Shared]
    public class DefaultBufferResolverProvider : ICommandingTextBufferResolverProvider
    {
        public ICommandingTextBufferResolver CreateResolver(ITextView textView)
        {
            return new DefaultBufferResolver(textView);
        }
    }

    internal class DefaultBufferResolver : ICommandingTextBufferResolver
    {
        private readonly ITextView _textView;

        public DefaultBufferResolver(ITextView textView)
        {
            _textView = textView ?? throw new ArgumentNullException(nameof(textView));
        }

        public IEnumerable<ITextBuffer> ResolveBuffersForCommand<TArgs>() where TArgs : EditorCommandArgs
        {
            var sourceSnapshotPoints = new FrugalList<SnapshotPoint>(new[] { _textView.Caret.Position.BufferPosition });
            var resolvedBuffers = new FrugalList<ITextBuffer>();
            for (int i = 0; i < sourceSnapshotPoints.Count; i++)
            {
                SnapshotPoint curSnapshotPoint = sourceSnapshotPoints[i];
                if (curSnapshotPoint.Snapshot is IProjectionSnapshot curProjectionSnapshot)
                {
                    sourceSnapshotPoints.AddRange(curProjectionSnapshot.MapToSourceSnapshots(curSnapshotPoint));
                }

                // As the set of buffers isn't likely to exceed 5, just use the list to determine whether we've seen it before
                ITextBuffer curBuffer = curSnapshotPoint.Snapshot.TextBuffer;
                if (!resolvedBuffers.Contains(curBuffer))
                {
                    resolvedBuffers.Add(curBuffer);
                }
            }

            return resolvedBuffers;
        }
    }
}
