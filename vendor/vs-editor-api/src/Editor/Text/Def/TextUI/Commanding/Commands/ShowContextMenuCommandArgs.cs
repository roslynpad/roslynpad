using Microsoft.VisualStudio.Commanding;

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands
{
    public sealed class ShowContextMenuCommandArgs : EditorCommandArgs
    {
        /// <summary>
        /// X coordinate for the context menu, optionally provided by the caller of the command.
        /// </summary>
        public double? X { get; }

        /// <summary>
        /// Y coordinate for the context menu, optionally provided by the caller of the command.
        /// </summary>
        public double? Y { get; }

        /// <summary>
        /// Creates an empty instance of the <see cref="ShowContextMenuCommandArgs"/>, for the
        /// purpose of passing to the <see cref="IChainedCommandHandler{T}.GetCommandState(T, System.Func{CommandState})"/>
        /// to determine the state of the command.
        /// </summary>
        public ShowContextMenuCommandArgs(ITextView textView, ITextBuffer subjectBuffer) : base(textView, subjectBuffer)
        {
        }

        /// <summary>
        /// Creates an instance of the <see cref="ShowContextMenuCommandArgs"/> to execute the command.
        /// </summary>
        public ShowContextMenuCommandArgs(ITextView textView, ITextBuffer subjectBuffer, double x, double y) : base(textView, subjectBuffer)
        {
            X = x;
            Y = y;
        }
    }
}
