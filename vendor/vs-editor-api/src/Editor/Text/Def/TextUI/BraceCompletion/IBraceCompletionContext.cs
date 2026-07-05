//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    /// <summary>
    /// Represents a simple context used to extend the default brace completion behaviors to include 
    /// language-specific behaviors such as parsing and formatting.
    /// </summary>
    public interface IBraceCompletionContext
    {
        /// <summary>
        /// Called before the session is added to the stack.
        /// </summary>
        /// <remarks>If additional formatting is required for the opening or closing brace it should be done here.</remarks>
        /// <param name="session">Default brace completion session</param>
        void Start(IBraceCompletionSession session);

        /// <summary>
        /// Called after the session has been removed from the stack.
        /// </summary>
        /// <param name="session">Default brace completion session</param>
        void Finish(IBraceCompletionSession session);

        /// <summary>
        /// Called by the editor when return is pressed while both braces are on the same line
        /// and no typing has occurred in the session.
        /// </summary>
        /// <remarks>Called after the newline has been inserted into the buffer.</remarks>
        /// <remarks>Formatting for scenarios where the closing brace needs to be moved down an additional
        /// line past the caret should be done here.</remarks>
        /// <param name="session">Default brace completion session</param>
        void OnReturn(IBraceCompletionSession session);

        /// <summary>
        /// Called by the editor when the closing brace character has been typed.
        /// </summary>
        /// <remarks>The closing brace character will not be inserted into the buffer until after this returns.</remarks>
        /// <remarks>Does not occur if there is any non-whitespace between the caret and the closing brace.</remarks>
        /// <remarks>Language-specific decisions may be made here to take into account scenarios such as an escaped closing char.</remarks>
        /// <param name="session">Default brace completion session</param>
        /// <returns>Returns true if the context is a valid overtype scenario.</returns>
        bool AllowOverType(IBraceCompletionSession session);
    }
}
