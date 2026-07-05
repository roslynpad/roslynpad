using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture to make the primary caret the next one up in the document, and make it visible to the user.
    /// </summary>
    public sealed class RotatePrimaryCaretPreviousCommandArgs : EditorCommandArgs
    {
        public RotatePrimaryCaretPreviousCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
