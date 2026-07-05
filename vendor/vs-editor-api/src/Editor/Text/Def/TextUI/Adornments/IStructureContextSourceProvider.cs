//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Creates an <see cref="IStructureContextSource"/> for a given buffer.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported as follows:
    /// [Export(typeof(IStructureContextSourceProvider))]
    /// [Name("MyProviderName")]
    /// [ContentType("MyContentTypeName")]
    /// [Order(Before = "Foo", After = "Bar")]
    /// Component exporters must add the Name and Order attribute to define the order of the provider in the provider chain.
    /// </remarks>
    public interface IStructureContextSourceProvider
    {
        /// <summary>
        /// Creates a structure context source for the given text view.
        /// </summary>
        /// <param name="textView">The text view for which to create an <see cref="IStructureContextSource"/>.</param>
        /// <returns>A valid <see cref="IStructureContextSource" /> instance, or null if none could be created.</returns>
        IStructureContextSource CreateStructureContextSource(ITextView textView);
    }
}
