using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    internal class SingleBufferResolver : ICommandingTextBufferResolver
    {
        private readonly ITextBuffer[] _textBuffer;

        public SingleBufferResolver(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            _textBuffer = new ITextBuffer[] { textBuffer };
        }

        public IEnumerable<ITextBuffer> ResolveBuffersForCommand<TArgs>() where TArgs : EditorCommandArgs
        {
            return _textBuffer;
        }
    }
}
