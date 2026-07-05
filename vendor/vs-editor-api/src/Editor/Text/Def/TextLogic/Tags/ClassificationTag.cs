//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// An implementation of <see cref="IClassificationTag" />.
    /// </summary>
    public class ClassificationTag : IClassificationTag
    {
        /// <summary>
        /// Create a new tag associated with the given type of
        /// classification.
        /// </summary>
        /// <param name="type">The type of classification</param>
        /// <exception cref="ArgumentNullException">If the type is passed in as null</exception>
        public ClassificationTag(IClassificationType type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ClassificationType = type;
        }

        /// <summary>
        /// The classification type associated with this tag.
        /// </summary>
        public IClassificationType ClassificationType { get; private set; }
    }
}
