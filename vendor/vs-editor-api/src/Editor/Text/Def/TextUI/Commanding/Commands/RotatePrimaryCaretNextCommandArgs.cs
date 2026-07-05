using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture to change the primary caret to the next caret in the document, and make that visible to the user.
    /// </summary>
    public sealed class RotatePrimaryCaretNextCommandArgs : EditorCommandArgs
    {
        public RotatePrimaryCaretNextCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
