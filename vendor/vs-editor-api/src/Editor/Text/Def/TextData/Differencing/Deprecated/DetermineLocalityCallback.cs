//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A delegate for determining the locality for a given difference type and left/right strings.
    /// </summary>
    /// <param name="differenceType">The type of difference to get the locality for.  This is
    /// guaranteed to be only a single type.</param>
    /// <param name="leftStrings">The left text, decomposed into a list of strings.</param>
    /// <param name="rightStrings">The right text, decomposed into a list of strings.</param>
    /// <returns>The locality, if desired, or <c>null</c>, to fallback to the default
    /// locality.</returns>
    /// <remarks>
    /// <para>This callback and methods that use it are now deprectated. Neither the default implementation of <see cref="ITextDifferencingService"/> or
    /// extensions that implement that interface are required to use this callback.</para>
    /// </remarks>
    [Obsolete("Methods that use this callback are now deprecated, and instances of this callback will not be used.")]
    public delegate int? DetermineLocalityCallback(StringDifferenceTypes differenceType, IList<string> leftStrings, IList<string> rightStrings);
}
