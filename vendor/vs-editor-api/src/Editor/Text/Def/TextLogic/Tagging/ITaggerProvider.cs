//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Creates an <see cref="ITagger&lt;T&gt;"/> for a given buffer.
    /// </summary>
    /// <remarks>This is a MEF component part, and implementers must use the following attributes:
    /// [Export(nameSource=typeof(ITaggerProvider))]
    /// Exports must specify at least one content type attribute and at least one tag type attribute.</remarks>
    public interface ITaggerProvider
    {
        /// <summary>
        /// Creates a tag provider for the specified buffer.
        /// </summary>
        /// <param name="buffer">The <see cref="ITextBuffer"/>.</param>
        /// <typeparam name="T">The type of the tag.</typeparam>
        ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag;
    }
}
