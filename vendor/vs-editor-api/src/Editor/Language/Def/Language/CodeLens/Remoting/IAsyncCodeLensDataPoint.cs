//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents an async CodeLens data point.
    /// </summary>
    public interface IAsyncCodeLensDataPoint
    {
        /// <summary>
        /// Raised when the data point is invalidated.
        /// </summary>
        event AsyncEventHandler InvalidatedAsync;

        /// <summary>
        /// The <see cref="CodeLensDescriptor"/> object that uniquely identifies the data point.
        /// </summary>
        CodeLensDescriptor Descriptor { get; }

        /// <summary>
        /// Gets lens data from the data point.
        /// </summary>
        /// <returns>
        /// A <see cref="CodeLensDataPointDescriptor"/> object representing the lens data from the data point.
        /// </returns>
        Task<CodeLensDataPointDescriptor> GetDataAsync(CancellationToken token);

        /// <summary>
        /// Gets lens details from the data point.
        /// </summary>
        /// <returns>
        /// A <see cref="CodeLensDetailEntryDescriptor"/> object representing the lens details of the data point.
        /// </returns>
        Task<CodeLensDetailsDescriptor> GetDetailsAsync(CancellationToken token);
    }
}
