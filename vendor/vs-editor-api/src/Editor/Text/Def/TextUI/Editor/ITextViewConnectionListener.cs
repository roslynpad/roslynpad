//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System.Collections.ObjectModel;
    using System.Collections.Generic;

    /// <summary>
    /// Listens to text buffers of a particular content type to find out when they are opened or closed
    /// in the text editor.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ITextViewConnectionListener))]
    /// [ContentType("...")]
    /// [TextViewRole("...")]
    /// </remarks>
    public interface ITextViewConnectionListener
    {
        /// <summary>
        /// Called when one or more <see cref="ITextBuffer"/> objects of the appropriate <see cref="Morgania.Utilities.IContentType"/> are connected to a <see cref="ITextView"/>.
        /// </summary>
        /// <remarks>
        /// A connection can occur at one of three times: (1) when the view is first created; (2) when the buffer becomes a member of the 
        /// <see cref="Morgania.Text.Projection.IBufferGraph"/> for the view; or (3) when the 
        /// <see cref="Morgania.Utilities.IContentType"/> of the buffer changes.
        /// </remarks>
        /// <param name="textView">The <see cref="ITextView"/> to which the subject buffers are being connected.</param>
        /// <param name="reason">The cause of the connection.</param>
        /// <param name="subjectBuffers">The non-empty list of <see cref="ITextBuffer"/> objects with matching
        /// content types.</param>
        void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers);

        /// <summary>
        /// Called when one or more <see cref="ITextBuffer"/> objects no longer satisfy the conditions for being included in the subject buffers.
        /// </summary>
        /// <remarks>
        /// Text buffers can be disconnected when they are removed as source buffers of some projection buffer, 
        /// or when their content type changes, or when the <see cref="ITextView"/> is closed.
        /// </remarks>
        /// <param name="textView">The <see cref="ITextView"/> from which the subject buffers are being disconnected.</param>
        /// <param name="reason">The cause of the disconnection.</param>
        /// <param name="subjectBuffers">The non-empty list of <see cref="ITextBuffer"/> objects.</param>
        void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers);
    }
}
