//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Structure
{
    using System;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Defines the interface for the <see cref="IStructureSpanningTreeService"/> which can be
    /// used to obtain instances of the <see cref="IStructureSpanningTreeManager"/>, which
    /// provides information about the structural hierarchy of code in an <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>
    /// This interface is a MEF component part and can be imported with a MEF import attribute.
    /// <code>
    /// [Import]
    /// internal IStructureSpanningTreeService StructureSpanningTreeService { get; }
    /// </code>
    /// </remarks>
    public interface IStructureSpanningTreeService
    {
        /// <summary>
        /// Gets the singleton <see cref="IStructureSpanningTreeManager"/> for the specified view.
        /// </summary>
        /// <param name="textView">The view to get the structure manager for.</param>
        /// <exception cref="InvalidOperationException">Throw if not called from the UI thread.</exception>
        /// <returns>The singleton instance of <see cref="IStructureSpanningTreeManager"/> for the view.</returns>
        IStructureSpanningTreeManager GetManager(ITextView textView);
    }
}
