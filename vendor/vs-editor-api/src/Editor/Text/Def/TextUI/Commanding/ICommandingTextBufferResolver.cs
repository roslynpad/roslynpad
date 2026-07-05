using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Editor.Commanding
{
    /// <summary>
    /// Given a text view and a command type, resolves a list of text buffers on which a command should be executed.
    /// Default implementation of this service returns a list of buffers in the text view which can be mapped
    /// to the caret position. Other implementations might take into acount text selection in addition to the caret position,
    /// for example when executing Format Document command in a projection scenario.
    /// </summary>
    public interface ICommandingTextBufferResolver
    {
        /// <summary>
        /// Given a command type, resolves a list of text buffers on which a command should be executed.
        /// </summary>
        /// <typeparam name="TArgs">Command type.</typeparam>
        /// <returns>A list of text buffers on which a command should be executed.</returns>
        IEnumerable<ITextBuffer> ResolveBuffersForCommand<TArgs>() where TArgs : EditorCommandArgs;
    }
}
