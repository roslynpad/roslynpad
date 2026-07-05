//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Document
{
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// A tag associated with a span of modified text. 
    /// </summary>
    /// <remarks>
    /// <para>Use the CreateTagAggregator method of IViewTagAggregatorFactoryService to instantiate an aggregator of change tags.</para>
    /// <para>Change taggers lose their change history when they are no longer consumed by any tag aggregators. They resume
    /// tracking changes if a new aggregator is created.</para>
    /// </remarks>
    public class ChangeTag : ITag
    {
        /// <summary>
        /// Gets the type of change for the tag.
        /// </summary>
        public ChangeTypes ChangeTypes { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="ChangeTag"/> with the specified change type.
        /// </summary>
        /// <param name="type">The type of change for the tag.</param>
        public ChangeTag(ChangeTypes type)
        {
            this.ChangeTypes = type;
        }
    }
}
