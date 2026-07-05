//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// The factory service for creating <see cref="ITextImage"/>s.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported as follows:
    /// <code>
    /// [Import]
    /// ITextImageFactoryService factory = null;
    /// </code>
    /// </remarks>
    public interface ITextImageFactoryService
    {
        /// <summary>
        /// Create an <see cref="ITextImage"/> that contains <paramref name="text"/>.
        /// </summary>
        ITextImage CreateTextImage(string text);

        /// <summary>
        /// Create an <see cref="ITextImage"/> that contains the contents of <paramref name="reader"/>.
        /// </summary>
        /// <param name="length">An estimate of the total length of the file. -1 if the file size is unknown.</param>
        /// <remarks>
        /// <para>The <paramref name="length"/> is used to decide whether or not to attempt to compress the text read from <paramref name="reader"/>. It does not need to be accurate.</para></remarks>
        ITextImage CreateTextImage(TextReader reader, long length = -1);
    }
}
