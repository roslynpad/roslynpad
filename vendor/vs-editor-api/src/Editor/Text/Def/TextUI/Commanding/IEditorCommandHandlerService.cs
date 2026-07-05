using Microsoft.VisualStudio.Commanding;
using System;

namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    /// <summary>
    /// A service to execute commands on a text view.
    /// </summary>
    /// <remarks>
    /// Instances of this service are created by <see cref="IEditorCommandHandlerServiceFactory"/>.
    /// </remarks>
    public interface IEditorCommandHandlerService
    {
        /// <summary>
        /// Get the <see cref="CommandState"/> for command handlers of a given command.
        /// </summary>
        /// <param name="argsFactory">A factory of <see cref="EditorCommandArgs"/> that specifies what kind of command is being queried.</param>
        /// <param name="nextCommandHandler">The next command handler to be called if no command handlers were
        /// able to determine the command state.</param>
        /// <typeparam name="T">The type of the command being queried.</typeparam>
        /// <returns>The command state of a given command.</returns>
        CommandState GetCommandState<T>(Func<ITextView, ITextBuffer, T> argsFactory, Func<CommandState> nextCommandHandler) where T : EditorCommandArgs;

        /// <summary>
        /// Execute a given command on the <see cref="ITextView"/> associated with this <see cref="IEditorCommandHandlerService"/> instance.
        /// </summary>
        /// <param name="argsFactory">A factory of <see cref="EditorCommandArgs"/> that specifies what kind of command is being executed.
        /// <param name="nextCommandHandler">The next command handler to be called if no command handlers were
        /// able to handle the command.</param>
        /// <typeparam name="T">The type of the command being executed.</typeparam>
        void Execute<T>(Func<ITextView, ITextBuffer, T> argsFactory, Action nextCommandHandler) where T : EditorCommandArgs;
    }
}
