//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;

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
    public interface ITextImageFactoryService2 : ITextImageFactoryService
    {
        /// <summary>
        /// Create an <see cref="ITextImage"/> from a memory mapped file.
        /// </summary>
        /// <param name="source">A utf-16 encoded image of the file.</param>
        /// <returns>An <see cref="ITextImage"/> that is mapped to the contents of <paramref name="source"/>.</returns>
        /// <remarks>
        /// <para><paramref name="source"/> must be encoded as utf-16.</para>
        /// <para>Any <see cref="ITextImage"/> created from <paramref name="source"/> be invalidated if <paramref name="source"/> is either
        /// modified or disposed of.</para>
        /// </remarks>
        ITextImage CreateTextImage(MemoryMappedFile source);
    }
}
