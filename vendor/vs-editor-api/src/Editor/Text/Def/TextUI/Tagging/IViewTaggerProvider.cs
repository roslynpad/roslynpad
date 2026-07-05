//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Creates an <see cref="ITagger&lt;T&gt;"/> for a given buffer.
    /// </summary>
    /// <remarks>This is a MEF component part, and implementers must use the following attributes:
    /// [Export(nameSource=typeof(ITaggerProvider))]
    /// Exports must specify at least one content type attribute and at least one tag type attribute. Exports may
    /// optionally specify a TextViewRole; if no TextViewRole is specified, the tagger applies to views with any roles.
    /// </remarks>
    public interface IViewTaggerProvider
    {
        /// <summary>
        /// Creates a tag provider for the specified view and buffer.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/>.</param>
        /// <param name="buffer">The <see cref="ITextBuffer"/>.</param>
        /// <typeparam name="T">The type of the tag.</typeparam>
        ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag;
    }
}
