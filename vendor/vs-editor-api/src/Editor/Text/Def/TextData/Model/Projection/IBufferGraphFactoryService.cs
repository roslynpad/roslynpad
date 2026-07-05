//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Projection
{
    using System;

    /// <summary>
    /// Creates a buffer graph from a graph of <see cref="ITextBuffer"/> objects created by projection.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IBufferGraphFactoryService factory = null;
    /// </remarks>
    public interface IBufferGraphFactoryService
    {
        /// <summary>
        /// Initializes a new instance of an <see cref="IBufferGraph"/> for the specified <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which to create the <see cref="IBufferGraph"/>.</param>
        /// <returns>The <see cref="IBufferGraph"/>.</returns>
        /// <exception cref="ArgumentNullException"> if <paramref name="textBuffer"/> is null.</exception>
        IBufferGraph CreateBufferGraph(ITextBuffer textBuffer);
    }
}
