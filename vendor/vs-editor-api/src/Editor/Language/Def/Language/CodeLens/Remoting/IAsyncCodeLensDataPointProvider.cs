//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a provider which creates <see cref="IAsyncCodeLensDataPoint"/> instances from
    /// an <see cref="CodeLensDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following metadata:
    /// <code>
    ///     [Export(typeof(IAsyncCodeLensDataPointProvider))]
    ///     [Name("nameOfTheProvider")]
    ///     [ContentType("csharp")]
    /// </code>
    ///
    /// The following metadata are optional:
    /// <code>
    ///     <see cref="PriorityAttribute"/>
    ///     <see cref="LocalizedNameAttribute"/>
    ///     <see cref="OptionUserVisibleAttribute"/>
    ///     <see cref="OptionUserModifiableAttribute"/>
    ///     <see cref="DetailsTemplateNameAttribute"/>
    /// </code>
    /// </remarks>
    public interface IAsyncCodeLensDataPointProvider
    {
        /// <summary>
        /// Determines if this provider can create an <see cref="IAsyncCodeLensDataPoint"/> for the specified <see cref="CodeLensDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">The descriptor to check.</param>
        /// <returns>
        /// <c>true</c> if a data point can be created from the descriptor; <c>false</c> otherwise.
        /// </returns>
        Task<bool> CanCreateDataPointAsync(CodeLensDescriptor descriptor, CancellationToken token);

        /// <summary>
        /// Creates an <see cref="IAsyncCodeLensDataPoint"/>, on request, from a given descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor to use.</param>
        /// <returns>
        /// An <see cref="IAsyncCodeLensDataPoint"/> created from the descriptor.
        /// </returns>
        Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(CodeLensDescriptor descriptor, CancellationToken token);
    }
}
