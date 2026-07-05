// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides <see cref="INavigableSymbolSource"/> for an <see cref="ITextBuffer"/> of a given content type.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component, and should be exported with the following attributes:</para>
    /// <code>
    ///    [Export(typeof(INavigableSymbolSourceProvider))]
    ///    [Name("name of the provider")]
    ///    [ContentType("content type")]
    /// </code>
    /// <para>And optionally, the OrderAttribute.</para>
    /// </remarks>
    public interface INavigableSymbolSourceProvider
    {
        /// <summary>
        /// Creates an <see cref="INavigableSymbolSource"/> for the given <see cref="ITextBuffer"/> 
        /// in the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> in which the text buffer was created.</param>
        /// <param name="buffer">The <see cref="ITextBuffer"/> for which the <see cref="INavigableSymbolSource"/> is created.</param>
        /// <returns>A valid <see cref="INavigableSymbolSource"/>, or <c>null</c> if the provider could not create one.</returns>
        /// <remarks>
        /// <para>
        /// This method should only be called once on the <paramref name="buffer"/> whose content type matches the provider's.
        /// </para>
        /// <para>
        /// If there are multiple sources matching the content type of the given text buffer, the best match based on the
        /// Order is used when calling <see cref="INavigableSymbolSource.GetNavigableSymbolAsync"/>.
        /// </para>
        /// </remarks>
        INavigableSymbolSource TryCreateNavigableSymbolSource(ITextView textView, ITextBuffer buffer);
    }
}
