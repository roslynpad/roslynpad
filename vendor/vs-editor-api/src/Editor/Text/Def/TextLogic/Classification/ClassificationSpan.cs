//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System;

    /// <summary>
    /// Describes a region of text by an <see cref="IClassificationType"/>.
    /// </summary>
    /// <remarks>
    /// This class is immutable.
    /// </remarks>
    public class ClassificationSpan
    {
        SnapshotSpan span;
        IClassificationType classification;

        /// <summary>
        /// Initializes a new instance of a <see cref="ClassificationSpan"/>.
        /// </summary>
        /// <param name="span">The span of text to which the classification applies.</param>
        /// <param name="classification">
        /// The classification type of the span.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="classification"/> is null.</exception>
        public ClassificationSpan(SnapshotSpan span, IClassificationType classification)
        {
            if (classification == null)
            {
                throw new ArgumentNullException(nameof(classification));
            }
            this.span = span;
            this.classification = classification;
        }

        /// <summary>
        /// Gets the classification type of the text.
        /// </summary>
        public IClassificationType ClassificationType
        {
            get { return this.classification; }
        }

        /// <summary>
        /// Gets the snapshot span of the classified text.
        /// </summary>
        public SnapshotSpan Span
        {
            get { return this.span; }
        }

    }
}
