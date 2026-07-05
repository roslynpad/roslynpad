using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture which takes the last secondary caret as defined in
    /// <see cref="RemoveLastSecondaryCaretCommandArgs"/>, and moving it to the location where a caret would
    /// be added by <see cref="InsertNextMatchingCaretCommandArgs"/>. This command is only available if there are
    /// already multiple carets.
    /// </summary>
    public sealed class IncrementLastSecondaryCaretCommandArgs : EditorCommandArgs
    {
        public IncrementLastSecondaryCaretCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
