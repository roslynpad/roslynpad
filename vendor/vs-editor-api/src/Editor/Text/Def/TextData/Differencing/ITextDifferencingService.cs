//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// This service has methods that compute differences over strings, snapshots, and spans.
    /// Differences are computed according to the specified <see cref="StringDifferenceTypes"/>,
    /// starting with the most general type (line is more general than word, 
    /// and word is more general than character).
    /// This service is meant to be provided by extenders to override the diff behavior by content
    /// type, thus allowing more control over the differences produced and how they align semantically
    /// (based upon the given language/content type).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// <code>
    /// [Export(typeof(ITextDifferencingService))]
    /// </code>
    /// Component exporters must add at least one content type attribute to specify the
    /// content types for which the component is valid, such as:
    /// <code>
    /// [ContentType("basic")]
    /// </code>
    /// </para>
    /// <para>Use the <see cref="ITextDifferencingSelectorService"/> to get the most specific <see cref="ITextDifferencingService"/>
    /// for a particular content type.</para>
    /// <para>
    /// Implementors of this class are free to interpret differencing fairly liberally.  For example, a text
    /// differencing service for C# files could perform differencing by namespace/class/method, and then return
    /// these results in line form (or whatever is requested by the <see cref="StringDifferenceOptions"/>).  To that end,
    /// the results returned are also allowed to be non-minimal, allowing implementations to return results that fit
    /// better into the semantic model of a given language (in order to try to more closely match user intent, instead of being technically
    /// minimal).  The normal contract of <see cref="IDifferenceCollection&lt;T&gt;" /> does still apply, however, in that
    /// differences cannot overlap, are sorted, etc.
    /// </para>
    /// <para>
    /// When calling into an instance of this interface and choosing a <see cref="WordSplitBehavior"/>, you should be using
    /// <see cref="WordSplitBehavior.LanguageAppropriate"/>, unless a different word splitting behavior is
    /// explicitly needed.  This allows the individual implementation latitude to apply the best word splitting rules
    /// for the given content type.
    /// </para>
    /// </remarks>
    public interface ITextDifferencingService
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
        /// <param name="left">The left span. In most cases this is from an "old" snapshot.</param>
        /// <param name="right">The right span. In most cases this is from a "new" snapshot.</param>
        /// <param name="differenceOptions">The options to use.</param>
        /// <returns>A hierarchical collection of differences.</returns>
        IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan left,
                                                            SnapshotSpan right,
                                                            StringDifferenceOptions differenceOptions);

        /// <summary>
        /// Computes the differences between two snapshot spans, using the given difference options.
        /// </summary>
        /// <param name="left">The left span. In most cases this is from an "old" snapshot.</param>
        /// <param name="right">The right span. In most cases this is from a "new" snapshot.</param>
        /// <param name="differenceOptions">The options to use.</param>
        /// <param name="getLineTextCallback">A callback for retrieving the text of snapshot lines (when performing differencing
        /// at the line level) that can optionally filter/modify the text, as long as it doesn't introduce line
        /// breaks (i.e. split the given line into multiple lines).</param>
        /// <returns>A hierarchical collection of differences.</returns>
        /// <remarks>
        /// <para>The <paramref name="getLineTextCallback"/> can be used for things like ignoring all intraline whitespace or
        /// case during line differencing.
        /// </para>
        /// <para>
        /// Also, the <paramref name="getLineTextCallback"/> is <i>only</i> used for line-level differencing. If word/character
        /// differencing is requested, the implementation should use the original snapshot text directly, as there is no
        /// guaranteed way to map from words in a filtered line back to the original line.
        /// </para>
        /// <para>
        /// The <paramref name="getLineTextCallback"/> will be called only for full lines that intersect each requested
        /// <see cref="SnapshotSpan"/>. If a line only partially intersects the given left or right span, then the
        /// intersection of the line and the span is used directly.
        /// </para>
        /// </remarks>
        IHierarchicalDifferenceCollection DiffSnapshotSpans(SnapshotSpan left,
                                                            SnapshotSpan right,
                                                            StringDifferenceOptions differenceOptions,
                                                            Func<ITextSnapshotLine, string> getLineTextCallback);
    }
}
