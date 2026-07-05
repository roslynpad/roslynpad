//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    /// <summary>
    /// Provides for computing the desired indentation for a line.
    /// </summary>
    public interface ISmartIndent : IDisposable
    {
        /// <summary>
        /// Gets the desired indentation of an <see cref="ITextSnapshotLine"/>.
        /// </summary>
        /// <param name="line">The line for which to compute the indentation.</param>
        /// <returns>The number of spaces to place at the start of the line, or null if there is no desired indentation.</returns>
        int? GetDesiredIndentation(ITextSnapshotLine line);
    }
}