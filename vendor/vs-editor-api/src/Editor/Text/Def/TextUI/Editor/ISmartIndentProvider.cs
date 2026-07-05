//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    /// <summary>
    /// Gets an <see cref="ISmartIndent"/> object for a given <see cref="ITextView"/>.
    /// Component exporters must supply at least one content type attribute to specify the applicable content types.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(NameSource=typeof(ISmartIndentProvider))]
    /// [ContentType("some content type")]
    /// </remarks>
    public interface ISmartIndentProvider
    {
        /// <summary>
        /// Creates an <see cref="ISmartIndent"/> object for the given <see cref="ITextView"/>. 
        /// </summary>
        /// <param name="textView">
        /// The <see cref="ITextView"/> on which the <see cref="ISmartIndent"/> will navigate.
        /// </param>
        /// <returns>
        /// A valid <see cref="ISmartIndent"/>. This value will never be <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="textView"/> is <c>null</c>.</exception>
        ISmartIndent CreateSmartIndent(ITextView textView);
    }
}
