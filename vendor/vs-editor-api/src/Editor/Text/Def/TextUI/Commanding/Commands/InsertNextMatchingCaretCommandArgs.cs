using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture to add a new caret at the next instance of whatever the primary selection contains within the given view.
    /// </summary>
    public sealed class InsertNextMatchingCaretCommandArgs : EditorCommandArgs
    {
        public InsertNextMatchingCaretCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
