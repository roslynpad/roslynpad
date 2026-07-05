//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// This service has several shortcut methods that compute
    /// differences over strings, snapshots, and spans.
    /// Differences are computed according to the specified <see cref="StringDifferenceTypes"/>,
    /// starting with the most general type 
    /// (line is more general than word, and word is more general than character).
    /// </summary>
    /// <example>
    /// Given string A:
    /// ---
    /// This is a
    /// line!
    /// ---
    /// And string B:
    /// ---
    /// This is but a
    /// line!
    /// ---
    /// 
    /// The returned difference collection contains one line difference, which maps to line 1 of each string.
    /// This difference contains one
    /// word difference, which is the addition of the words "but" and " ".
    /// </example>
    /// <remarks>
    /// <para>
    /// This type is deprecated.  Use <see cref="ITextDifferencingSelectorService"/> instead, which allows you
    /// to retrieve an <see cref="ITextDifferencingService"/> for a specific content type and provides a superset
    /// of the methods available on this interface.
    /// </para>
    /// <para>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IHierarchicalStringDifferenceService diffService = null;
    /// </para>
    /// </remarks>
    [Obsolete("This interface has been deprecated in favor of the ITextDifferencingSelectorService MEF service.")]
    public interface IHierarchicalStringDifferenceService
    {
        /// <summary>
        /// Computes the differences between two strings, using the given difference options.
        /// </summary>
        /// <param name="left">The left string. In most cases this is the the "old" string.</param>
        /// <param name="right">The right string. In most cases this is the "new" string.</param>
        /// <param name="differenceOptions">The options to use in differencing</param>
        /// <returns>A hierarchical collection of differences.</returns>
        IHierarchicalDifferenceCollection DiffStrings(string left,
                                                      string right,
                                                      StringDifferenceOptions differenceOptions);

        /// <summary>
        /// Computes the differences between two snapshot spans, using the given difference options.
        /// </summary>
        /// <param name="left">The left snapshot. In most cases this is the the "old" snapshot.</param>
        /// <param name="right">The right snapshot. In most cases this is the "new" snapshot.</param>
        /// <param name="differenceOptions">The options to use.</param>
        /// <returns>A hierarchical collection of differences.</returns>
        IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan left,
                                                            SnapshotSpan right,
                                                            StringDifferenceOptions differenceOptions);
    }
}
