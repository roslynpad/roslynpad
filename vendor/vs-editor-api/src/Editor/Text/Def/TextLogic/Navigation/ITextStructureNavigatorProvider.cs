//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Gets an <see cref="ITextStructureNavigator"/> for a given <see cref="ITextBuffer"/>.
    /// Component exporters must supply at least one content type attribute"/> to specify the applicable content types.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(NameSource=typeof(ITextStructureNavigatorProvider))]</para>
    /// <para>Use the <see cref="ITextStructureNavigatorSelectorService"/> to import a provider for a particular content type.</para>
    /// </remarks>
    public interface ITextStructureNavigatorProvider
    {
        /// <summary>
        /// Creates a new <see cref="ITextStructureNavigator"/> for a given <see cref="ITextBuffer"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> for which to get the <see cref="ITextStructureNavigator"/>.</param>
        /// <returns>The <see cref="ITextStructureNavigator"/> for <paramref name="textBuffer"/>, or null.</returns>
        /// <remarks>
        /// Providers should expect the result of this call to be cached and made available through the
        /// <see cref="ITextStructureNavigatorSelectorService"/>.
        /// </remarks>
        ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer);
    }
}
