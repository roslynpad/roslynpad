//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text.UI.Adornments;

    /// <summary>
    /// Provides context for structural block tool tips for a given sequence
    /// of <see cref="IStructureElement"/>s.
    /// </summary>
    public interface IStructureContextSource : IDisposable
    {
        /// <summary>
        /// Gets the context for the given structure tags.
        /// </summary>
        /// <param name="elements">The structure tags to get context for.</param>
        /// <param name="token">The cancellation token for this asynchronous method call.</param>
        /// <returns>The object to be displayed in the structure tool tip.</returns>
        /// <remarks>
        /// If the object returned by this method implements ITextView, ITextView.Close() is called
        /// when the tooltip is dismissed.
        /// </remarks>
        Task<object> GetStructureContextAsync(IEnumerable<IStructureElement> elements, CancellationToken token);
    }
}
