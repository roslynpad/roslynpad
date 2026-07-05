//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Provides read access to an immutable snapshot of a <see cref="ITextBuffer"/> containing a sequence of Unicode characters. 
    /// The first character in the sequence has index zero.
    /// </summary>
    /// <remarks>Any <see cref="ITextSnapshot"/> will be upcastable to an <see cref="ITextSnapshot2"/>.</remarks>
    public interface ITextSnapshot2 : ITextSnapshot
    {
        /// <summary>
        /// Gets the underlying <see cref="ITextImage"/> of the snapshot.
        /// </summary>
        ITextImage TextImage { get; }

        /// <summary>
        /// Save the entire snapshot to the specified <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Path to save</param>
        /// <param name="replaceFile">If true, replace an exising file.</param>
        /// <param name="encoding">Encoding to use to save the file.</param>
        void SaveToFile(string filePath, bool replaceFile, Encoding encoding);
    }
}
