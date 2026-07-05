//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// A service that creates <see cref="ITextSearchNavigator"/> objects.
    /// </summary>
    /// <remarks>
    /// This service is provided by the editor and should be consumed via the Managed Extensibility Framework.
    /// </remarks>
    /// <example>
    /// [Import]
    /// ITextSearchNavigatorFactoryService TextSearchNavigatorProvider { get; set; }
    /// </example>
    public interface ITextSearchNavigatorFactoryService
    {
        /// <summary>
        /// Creates an <see cref="ITextSearchNavigator"/> that searches the provided <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">
        /// The <see cref="ITextBuffer"/> to search.
        /// </param>
        /// <param name="searchTerm">
        /// The term to search for.
        /// </param>
        /// <param name="searchOptions">
        /// The options to use for performing of search.
        /// </param>
        /// <returns>
        /// An <see cref="ITextSearchNavigator"/> that searches the provided <see cref="ITextBuffer"/>.
        /// </returns>
        ITextSearchNavigator3 CreateSearchNavigator(ITextBuffer buffer);
    }
}
