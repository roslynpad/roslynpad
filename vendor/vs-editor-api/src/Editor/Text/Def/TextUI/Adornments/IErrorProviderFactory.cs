//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Gets a error tagger (a <see cref="SimpleTagger&lt;T&gt;"/> of type <see cref="ErrorTag"/>) for the given buffer, 
    /// or creates a new one if there is no error tagger already cached in the owned properties of the buffer.
    /// </summary>
    /// <remarks>This is a MEF somponent part, and should be exported with the following attribute:
    /// [Export(typeof(IErrorProviderFactory))]
    /// </remarks>
    public interface IErrorProviderFactory
    {
        /// <summary>
        /// Gets the cached error tagger for a given <see cref="ITextBuffer"/>. 
        /// If one does not exist, creates and caches a new <see cref="SimpleTagger&lt;ErrorTag&gt;"/> 
        /// with the <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> with which to get the error tagger.</param>
        /// <returns>The cached error tagger for the <paramref name="textBuffer"/>.</returns>
        SimpleTagger<ErrorTag> GetErrorTagger(ITextBuffer textBuffer);
    }
}
