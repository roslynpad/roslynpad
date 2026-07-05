//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Assigns <see cref="IClassificationType"/> objects to the text in a <see cref="ITextBuffer"/>.
    /// </summary>
    public interface IClassifier
    {
        /// <summary>
        /// Gets all the <see cref="ClassificationSpan"/> objects that intersect the given range of text.
        /// </summary>
        /// <param name="span">
        /// The snapshot span.
        /// </param>
        /// <returns>
        /// A list of <see cref="ClassificationSpan"/> objects that intersect with the given range. 
        /// </returns>
        IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span);

        /// <summary>
        /// Occurs when the classification of a span of text has changed. 
        /// </summary>
        /// <remarks>
        /// This event does not need to be raised for newly-inserted text. 
        /// However, it should be raised if any text other than that which was actually inserted has been reclassified.
        /// It should also be raised if the deletion of text causes the remaining
        /// text to be reclassified.</remarks>
        event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
    }
}
