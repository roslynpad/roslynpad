//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents an extension point used to create an <see cref="IBraceCompletionSession"/>
    /// for brace completion. A session tracks a set of braces and handles actions
    /// performed by the user within the braces to allow for over typing of the 
    /// closing brace and additional formatting.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IBraceCompletionSessionProvider))]
    /// </para>
    /// <para>
    /// Exports must include at least one [BracePair] attribute and at least one [ContentType] attribute.
    /// </para>
    /// </remarks>
    public interface IBraceCompletionSessionProvider
    {
        /// <summary>
        /// If appropriate, creates an <see cref="IBraceCompletionSession"/> based on the language context at the <paramref name="openingPoint"/>.
        /// </summary>
        /// <remarks>Opening points within strings and comments are usually invalid points to start an <see cref="IBraceCompletionSession"/> and will return false.</remarks>
        /// <param name="textView">View containing the <paramref name="openingPoint"/>.</param>
        /// <param name="openingPoint">Insertion point of the <paramref name="openingBrace"/> within the subject buffer. 
        /// The content type of the subject buffer will match one of the [ContentType] attributes for this extension.</param>
        /// <param name="openingBrace">Opening brace that has been typed by the user.</param>
        /// <param name="closingBrace">Closing brace character</param>
        /// <param name="session">Brace completion session if created.</param>
        /// <returns>Returns true if the <paramref name="openingPoint"/> was a valid point in the buffer to start a <see cref="IBraceCompletionSession"/>.</returns>
        bool TryCreateSession(ITextView textView, SnapshotPoint openingPoint, char openingBrace, char closingBrace, out IBraceCompletionSession session);
    }
}
