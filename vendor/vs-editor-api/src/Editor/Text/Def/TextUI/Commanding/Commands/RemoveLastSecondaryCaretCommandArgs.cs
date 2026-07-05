using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture used to remove a secondary caret from a text view. The the caret to remove is identified
    /// as the last caret in a circular loop, starting at the primary caret. Effectively, that means either the caret directly above
    /// the primary caret, or the last one in the view. This command is not available if there is only one caret.
    /// </summary>
    public sealed class RemoveLastSecondaryCaretCommandArgs : EditorCommandArgs
    {
        public RemoveLastSecondaryCaretCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
