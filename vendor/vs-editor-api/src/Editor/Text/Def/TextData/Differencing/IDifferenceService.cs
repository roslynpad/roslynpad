//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Determines the differences between two
    /// sequences, based on adding or removing elements (but not translating or copying elements).
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IDifferenceService diffService = null;
    /// </remarks>
    public interface IDifferenceService
    {
        /// <summary>
        /// Computes the differences between the two sequences.
        /// </summary>
        /// <typeparam name="T">The type of the sequences.</typeparam>
        /// <param name="left">The left sequence. In most cases this is the "old" sequence.</param>
        /// <param name="right">The right sequence. In most cases this is the "new" sequence.</param>
        /// <returns>A collection of the differences between the two sequences.</returns>
        IDifferenceCollection<T> DifferenceSequences<T>(IList<T> left, IList<T> right);

        /// <summary>
        /// Computes the differences between the two sequences.  The supplied predicate will be called on each
        /// step through the <paramref name="left"/> sequence.
        /// </summary>
        /// <typeparam name="T">The type of the sequences.</typeparam>
        /// <param name="left">The left sequence. In most cases this is the "old" sequence.</param>
        /// <param name="right">The right sequence. In most cases this is the "new" sequence.</param>
        /// <param name="continueProcessingPredicate">A predicate that will be called on each step through the <paramref name="left"/> sequence,
        /// with the option of stopping the algorithm prematurely.</param>
        /// <returns>A collection of the differences between the two sequences.</returns>
        IDifferenceCollection<T> DifferenceSequences<T>(IList<T> left, IList<T> right, ContinueProcessingPredicate<T> continueProcessingPredicate);
    }
}
