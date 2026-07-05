//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using System;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Selects and caches <see cref="ITextStructureNavigator"/> objects based on content type.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITextStructureNavigatorSelectorService navigator = null;
    /// </remarks>
    public interface ITextStructureNavigatorSelectorService
    {
        /// <summary>
        /// Gets a <see cref="ITextStructureNavigator"/> for the specified <see cref="ITextBuffer"/>, either by
        /// creating a new one or by using a cached value.
        /// </summary>
        /// <param name="textBuffer">
        /// The <see cref="ITextBuffer"/> that the <see cref="ITextStructureNavigator"/> will navigate.
        /// </param>
        /// <returns>
        /// A valid <see cref="ITextStructureNavigator"/>. This value will never be <c>null</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a navigator for the exact <see cref="IContentType"/> of the given <see cref="ITextBuffer"/> cannot be found, this method returns
        /// one for the parent <see cref="IContentType"/>. If there is more than one parent <see cref="IContentType"/> for which 
        /// there is a matching <see cref="ITextStructureNavigator"/>, then this method returns the <see cref="ITextStructureNavigator"/>
        /// of an arbitrary parent.
        /// </para>
        /// <para>
        /// If a new navigator is created, it is cached together with <paramref name="textBuffer"/>, and its lifetime is the same as that of <paramref name="textBuffer"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> is <c>null</c>.</exception>
        ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer);

        /// <summary>
        /// Creates a new <see cref="ITextStructureNavigator"/> for the specified <see cref="ITextBuffer"/> by using the
        /// specified <see cref="IContentType"/> to select the navigator.
        /// </summary>
        /// <param name="textBuffer">
        /// The <see cref="ITextBuffer"/> that the <see cref="ITextStructureNavigator"/> will navigate.
        /// </param>
        /// <param name="contentType">The content type to use.</param>
        /// <returns>
        /// A valid <see cref="ITextStructureNavigator"/>. This value is never <c>null</c>).
        /// </returns>
        /// <remarks>
        /// <para>
        /// If a navigator for the given content type cannot be found, this method
        /// uses one for the parent <see cref="IContentType"/>. If there is more than one parent <see cref="IContentType"/> for which 
        /// there is a matching <see cref="ITextStructureNavigator"/>, then this method returns the <see cref="ITextStructureNavigator"/>
        /// of an arbitrary parent.
        /// </para>
        /// <para>
        /// The navigator that is created is not cached; subsequent calls to this method for the same buffer and
        /// content type will return different <see cref="ITextStructureNavigator"/> objects.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textBuffer"/> is <c>null</c>.</exception>
        ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType);
    }
}
