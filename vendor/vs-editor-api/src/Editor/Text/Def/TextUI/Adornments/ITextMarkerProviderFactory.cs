//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Gets a text marker tagger (a <see cref="SimpleTagger&lt;T&gt;"/> of type <see cref="TextMarkerTag"/> for a given buffer, or creates a new one if 
    /// no text marker tagger is already cached in the owned properties of the buffer.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be exported with the following attribute:
    /// [Export(typeof(ITextMarkerProviderFactory))]
    /// </remarks>
    public interface ITextMarkerProviderFactory
    {
        /// <summary>
        /// Gets the cached text marker tagger for a given <see cref="ITextBuffer"/>. 
        /// If one does not exist, creates and caches a new <see cref="SimpleTagger&lt;TextMarkerTag&gt;"/>
        /// with the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> with which to get the text marker tagger.</param>
        /// <returns>The cached <see cref="SimpleTagger&lt;T&gt;"/> for <paramref name="textBuffer"/>.</returns>
        SimpleTagger<TextMarkerTag> GetTextMarkerTagger(ITextBuffer textBuffer);
    }
}
