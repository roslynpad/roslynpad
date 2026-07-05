//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Creates or reuses an <see cref="IScrollMap"/> for an <see cref="ITextView"/>.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IScrollMapFactoryService factory = null;
    /// </remarks>
    public interface IScrollMapFactoryService
    {
        /// <summary>
        /// Creates or reuses an existing scroll map for the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView"><see cref="ITextView"/> for which to get an <see cref="IScrollMap"/>.</param>
        /// <returns>An <see cref="IScrollMap"/> for <paramref name="textView"/>.</returns>
        /// <remarks>The coordinate system returned by this scroll map will act as if elisions are not expanded.</remarks>
        IScrollMap Create(ITextView textView);

        /// <summary>
        /// Creates or reuses an existing scroll map for the specified <see cref="ITextView"/>.
        /// </summary>
        /// <param name="textView"><see cref="ITextView"/> for which to get an <see cref="IScrollMap"/>.</param>
        /// <param name="areElisionsExpanded">Does the coordinate system used by this scroll map act as if all elisions are expanded?</param>
        /// <returns>An <see cref="IScrollMap"/> for <paramref name="textView"/>.</returns>
        IScrollMap Create(ITextView textView, bool areElisionsExpanded);
    }
}
