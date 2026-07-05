//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System;

    /// <summary>
    /// Provides information for the <see cref="IClassifier.ClassificationChanged"/> event.
    /// </summary>
    public class ClassificationChangedEventArgs : EventArgs
    {
        SnapshotSpan changeSpan;

        /// <summary>
        /// Initializes a new instance of a <see cref="ClassificationChangedEventArgs"/> object.
        /// </summary>
        /// <param name="changeSpan">
        /// The span of the classification that changed.
        /// </param>
        public ClassificationChangedEventArgs(SnapshotSpan changeSpan)
        {
            this.changeSpan = changeSpan;
        }

        /// <summary>
        /// Gets the span of the classification that changed.
        /// </summary>
        public SnapshotSpan ChangeSpan
        {
            get { return this.changeSpan; }
        }
    }
}