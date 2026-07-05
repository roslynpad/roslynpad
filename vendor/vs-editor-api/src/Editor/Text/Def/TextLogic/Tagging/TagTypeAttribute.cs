//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Declares the types of tags an <see cref="ITagger&lt;T&gt;"/>
    /// produces. This attribute is placed on the provider of the tagger.
    /// </summary>
    public sealed class TagTypeAttribute : MultipleBaseMetadataAttribute
    {
        private Type type;

        /// <summary>
        /// Initializes a new instance of a <see cref="TagTypeAttribute"/>.
        /// </summary>
        /// <param name="tagType">The tag type, which must derive from <see cref="ITag"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tagType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="tagType"/> does not derive from <see cref="ITag"/>.</exception>
        public TagTypeAttribute(Type tagType)
        {
            if (tagType == null)
                throw new ArgumentNullException(nameof(tagType));
            if (!typeof(ITag).IsAssignableFrom(tagType))
                throw new ArgumentException("Given type must derive from ITag", nameof(tagType));

            this.type = tagType;
        }

        /// <summary>
        /// Gets the type of the tag.
        /// </summary>
        public Type TagTypes
        {
            get { return type; }
        }
    }
}
