//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Creates a classifier for a given <see cref="ITextBuffer" />.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(NameSource=typeof(IClassifierProvider))]
    /// Component exporters must add at least one content type attribute to specify the
    /// content types for which the component is valid.
    /// </remarks>
    public interface IClassifierProvider
    {
        /// <summary>
        /// Gets a classifier for the given text buffer.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to classify.</param>
        /// <returns>A classifier for the text buffer, or null if the provider cannot do so in its current state.</returns>   
        IClassifier GetClassifier(ITextBuffer textBuffer);
    }
}
