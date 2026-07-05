//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents an extension point used to create an <see cref="IBraceCompletionContext"/>
    /// to provide language-specific handling on top of default <see cref="IBraceCompletionSession"/>s.
    /// </summary>
    /// <remarks><see cref="IBraceCompletionContextProvider"/> extends the default brace completion 
    /// behaviors provided by <see cref="IBraceCompletionDefaultProvider"/>. It allows for additional 
    /// formatting after the closing brace has been inserted as well as custom handling 
    /// of overtype scenarios and newline insertions.</remarks>
    /// <remarks>For a fully customizeable <see cref="IBraceCompletionSession"/> use <see cref="IBraceCompletionSessionProvider"/>.</remarks>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IBraceCompletionContextProvider))]
    /// </para>
    /// <para>
    /// Exports must include at least one [BracePair] attribute and at least one [ContentType] attribute.
    /// </para>
    /// </remarks>
    public interface IBraceCompletionContextProvider
    {
        /// <summary>
        /// Creates an <see cref="IBraceCompletionContext"/> to handle language-specific 
        /// actions such as parsing and formatting.
        /// </summary>
        /// <remarks>Opening points within strings and comments are usually invalid points to start an <see cref="IBraceCompletionSession"/> and will return false.</remarks>
        /// <param name="textView">View containing the <paramref name="openingPoint"/>.</param>
        /// <param name="openingPoint">Insertion point of the <paramref name="openingBrace"/>.</param>
        /// <param name="openingBrace">Opening brace that has been typed by the user.</param>
        /// <param name="closingBrace">Closing brace character</param>
        /// <param name="context">Brace completion context if created.</param>
        /// <returns>Returns true if the <paramref name="openingPoint"/> was a valid point in the buffer to start a <see cref="IBraceCompletionSession"/>.</returns>
        bool TryCreateContext(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionContext context);
    }
}