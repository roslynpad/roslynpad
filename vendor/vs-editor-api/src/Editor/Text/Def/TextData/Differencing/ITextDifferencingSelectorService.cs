//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Used to retrieve an <see cref="ITextDifferencingService"/> for a given content type.  These services may be
    /// provided by extenders and may return differences that more closely match the semantics
    /// of the given content type, instead of just simple textual differencing.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextDifferencingSelectorService diffService = null;
    /// </para>
    /// <para>The methods on this service are guaranteed to never return <c>null</c>.  If there isn't a
    /// specific <see cref="ITextDifferencingService"/> registered for the given content type or
    /// any of its parent types, a default service will be used, which will use the default <see cref="IDifferenceService"/>
    /// to perform simple textual differencing.</para>
    /// </remarks>
    public interface ITextDifferencingSelectorService
    {
        /// <summary>
        /// Get the <see cref="ITextDifferencingService"/> for the given content type.
        /// </summary>
        /// <param name="contentType">The content type to use.</param>
        /// <returns>An <see cref="ITextDifferencingService"/>, which is guaranteed to never be <c>null</c>.</returns>
        /// <remarks><para>If two <see cref="ITextDifferencingService"/> exports tie for being most specific,
        /// one will be chosen arbitrarily.</para></remarks>
        ITextDifferencingService GetTextDifferencingService(IContentType contentType);

        /// <summary>
        /// Gets the default (fallback) <see cref="ITextDifferencingService"/>, which performs
        /// simple textual differencing.
        /// </summary>
        /// <returns>The default <see cref="ITextDifferencingService"/> implementation (never <c>null</c>).</returns>
        ITextDifferencingService DefaultTextDifferencingService { get; }
    }
}
