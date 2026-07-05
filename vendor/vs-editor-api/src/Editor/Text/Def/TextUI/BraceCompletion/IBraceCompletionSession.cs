//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents a brace completion session for the purpose of tracking a pair of braces
    /// and handling actions occurring between the OpeningPoint and ClosingPoint.
    /// </summary>
    public interface IBraceCompletionSession
    {
        /// <summary>
        /// Gets the starting point of the session.
        /// </summary>
        /// <remarks>The OpeningPoint and ClosingPoint are used to determine if the caret is within the session.
        /// If either one is null after Start has been called the session will be removed from the stack.</remarks>
        ITrackingPoint OpeningPoint { get; }

        /// <summary>
        /// Gets the ending point of the session.
        /// </summary>
        /// <remarks>The OpeningPoint and ClosingPoint are used to determine if the caret is within the session.
        /// If either one is null after Start has been called the session will be removed from the stack.</remarks>
        ITrackingPoint ClosingPoint { get; }

        /// <summary>
        /// Gets the text view in which the brace completion session is occurring.
        /// </summary>
        ITextView TextView { get; }

        /// <summary>
        /// Gets the subject buffer in which the brace completion session is occurring.
        /// </summary>
        ITextBuffer SubjectBuffer { get; }

        /// <summary>
        /// Gets the opening brace character.
        /// </summary>
        char OpeningBrace { get; }

        /// <summary>
        /// Gets the closing brace character.
        /// </summary>
        char ClosingBrace { get; }

        /// <summary>
        /// Called before the session is added to the stack.
        /// </summary>
        /// <remarks>This method is called after the opening brace has been inserted into the buffer.</remarks>
        void Start();

        /// <summary>
        /// Called after the session has been removed from the stack.
        /// </summary>
        void Finish();

        /// <summary>
        /// Called by the editor when the closing brace character has been typed and before it is 
        /// inserted into the buffer.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the closing brace character from being 
        /// inserted into the buffer.</param>
        void PreOverType(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the closing brace character has been typed.
        /// </summary>
        void PostOverType();

        /// <summary>
        /// Called by the editor when tab has been pressed and before it is inserted into the buffer.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the tab from being inserted into the buffer.</param>
        void PreTab(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the tab has been inserted.
        /// </summary>
        void PostTab();

        /// <summary>
        /// Called by the editor before the character has been removed.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the backspace action from completing.</param>
        void PreBackspace(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the character has been removed.
        /// </summary>
        void PostBackspace();

        /// <summary>
        /// Called by the editor when delete is pressed within the session.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the deletion.</param>
        void PreDelete(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the delete action.
        /// </summary>
        void PostDelete();

        /// <summary>
        /// Called by the editor when return is pressed within the session.
        /// </summary>
        /// <param name="handledCommand">Set to true to prevent the newline insertion.</param>
        void PreReturn(out bool handledCommand);

        /// <summary>
        /// Called by the editor after the newline has been inserted.
        /// </summary>
        void PostReturn();
    }
}
