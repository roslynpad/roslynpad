//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// A service that returns an <see cref="IClassifier"/> that aggregates and normalizes all <see cref="IClassifier"/>
    /// contributions for a <see cref="ITextBuffer"/>.
    /// </summary>
    /// <remarks>
    /// <para>The normalized classifications produced by this aggregator are sorted and do not overlap. If a span of text
    /// had multiple classifications based on the original classifier contributions, then in the normalized
    /// classification it has a transient classification (<see cref="IClassificationTypeRegistryService"/>) that corresponds to
    /// all of the original classifications.</para>
    /// <para>Classifier aggregators are cached for each <see cref="ITextBuffer"/> object.</para>
    /// </remarks>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IClassifierAggregatorService aggregator = null;
    /// </remarks>
    public interface IClassifierAggregatorService
    {
        /// <summary>
        /// Gets the cached <see cref="IClassifier"/> for the given <see cref="ITextBuffer"/>.  
        /// If one does not exist, an <see cref="IClassifier"/> will be created and cached with the given <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> with which to retrieve/create the <see cref="IClassifier"/>.</param>
        /// <returns>The cached <see cref="IClassifier"/>.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="textBuffer"/> is null.</exception>
        IClassifier GetClassifier(ITextBuffer textBuffer);
    }
}
