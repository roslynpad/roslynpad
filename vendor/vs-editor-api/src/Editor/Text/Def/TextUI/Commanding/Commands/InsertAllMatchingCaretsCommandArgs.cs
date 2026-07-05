using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    /// <summary>
    /// This command refers to the gesture to select all instances of text that matches the current primary selection.
    /// </summary>
    public sealed class InsertAllMatchingCaretsCommandArgs : EditorCommandArgs
    {
        public InsertAllMatchingCaretsCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }
    }
}
