//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A service that returns an <see cref="IClassifier"/> that aggregates and normalizes all <see cref="IClassifier"/>
    /// contributions for all <see cref="ITextBuffer"/>s in the buffer graph of a particular <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>
    /// <para>The normalized classifications produced by this aggregator are sorted and do not overlap. If a span of text
    /// had multiple classifications based on the original classifier contributions, then in the normalized
    /// classification it has a transient classification (<see cref="IClassificationTypeRegistryService"/>) that corresponds to
    /// all of the original classifications.</para>
    /// <para>Classifier aggregators are cached for each <see cref="ITextBuffer"/> and <see cref="ITextView"/> combination.</para>
    /// </remarks>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IViewClassifierAggregatorService aggregator = null;
    /// </remarks>
    public interface IViewClassifierAggregatorService
    {
        /// <summary>
        /// Gets the cached <see cref="IClassifier"/> for the given <see cref="ITextView"/>.
        /// If one does not exist, an <see cref="IClassifier"/> will be created and cached for each <see cref="ITextBuffer"/> in the
        /// view's buffer graph.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> to use in retrieving or creating the <see cref="IClassifier"/>.</param>
        /// <returns>The cached <see cref="IClassifier"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="textView"/> is null.</exception>
        IClassifier GetClassifier(ITextView textView);
    }
}
