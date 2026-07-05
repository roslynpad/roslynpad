//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.Threading;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Generates RTF-formatted text from a collection of snapshot spans.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part and should be imported using the following attribute:
    /// [Import] 
    /// </remarks>
    public interface IRtfBuilderService2 : IRtfBuilderService
    {
        /// <summary>
        /// Gets an RTF string containing the formatted text of the snapshot spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="delimiter">
        /// A delimiter string to be inserted between the RTF generated code for the <see cref="SnapshotSpan"/>s in the <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// <see cref="CancellationToken"/> used to indicate when to abandon the effort to generate the rich text.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, string delimiter, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an RTF string containing the formatted text of the snapshot spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans. A new line "\par" rtf keyword will be placed between the provided
        /// <see cref="SnapshotSpan"/>s.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="cancellationToken">
        /// <see cref="CancellationToken"/> used to indicate when to abandon the effort to generate the rich text.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an RTF string that contains the formatted text of the spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans, 
        /// with the characteristics and formatting properties of <paramref name="textView"/>.
        /// All the snapshot spans must belong to <paramref name="textView"/>.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the snapshot spans.
        /// </param>
        /// <param name="delimiter">
        /// A delimiter string to be inserted between the RTF generated code for the <see cref="SnapshotSpan"/>s in the <see cref="NormalizedSnapshotSpanCollection"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// <see cref="CancellationToken"/> used to indicate when to abandon the effort to generate the rich text.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, ITextView textView, string delimiter, CancellationToken cancellationToken);

        /// <summary>
        /// Gets an RTF string that contains the formatted text of the spans.
        /// </summary>
        /// <remarks>
        /// The generated RTF text is based on an in-order walk of the snapshot spans, 
        /// with the characteristics and formatting properties of <paramref name="textView"/>.
        /// All the snapshot spans must belong to <paramref name="textView"/>. A new line "\par" rtf keyword will be 
        /// placed between the provided <see cref="SnapshotSpan"/>s.
        /// </remarks>
        /// <param name="spans">
        /// The collection of snapshot spans.
        /// </param>
        /// <param name="textView">
        /// The <see cref="ITextView"/> that contains the snapshot spans.
        /// <param name="cancellationToken">
        /// <see cref="CancellationToken"/> used to indicate when to abandon the effort to generate the rich text.
        /// </param>
        /// <returns>
        /// A <see cref="string"/> containing RTF data.
        /// </returns>
        string GenerateRtf(NormalizedSnapshotSpanCollection spans, ITextView textView, CancellationToken cancellationToken);
    }
}
