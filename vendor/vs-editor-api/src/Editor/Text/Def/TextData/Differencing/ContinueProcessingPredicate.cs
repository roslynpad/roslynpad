//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A predicate used by <see cref="IDifferenceService"/> that allows callers to stop differencing prematurely.
    /// </summary>
    /// <typeparam name="T">The type of sequences being differenced.</typeparam>
    /// <param name="leftIndex">The current index in the left sequence being differenced.</param>
    /// <param name="leftSequence">The left sequence being differenced.</param>
    /// <param name="longestMatchSoFar">The length of the longest match so far.</param>
    /// <returns><c>true</c> if the algorithm should continue processing, <c>false</c> to stop the algorithm.</returns>
    /// <remarks>
    /// When <c>false</c> is returned, the algorithm stops searching for matches and uses the information it has computed so
    /// far to create the <see cref="IDifferenceCollection&lt;T&gt;"/> that will be returned.
    /// </remarks>
    public delegate bool ContinueProcessingPredicate<T>(int leftIndex, IList<T> leftSequence, int longestMatchSoFar);
}
