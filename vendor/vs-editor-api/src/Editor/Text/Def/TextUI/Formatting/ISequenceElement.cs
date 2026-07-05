//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    /// <summary>
    /// Represents the basic element in a sequence of elements that compose an <see cref="ITextViewLine"/>.
    /// </summary>
    public interface ISequenceElement
    {
        /// <summary>
        /// Gets the <see cref="IMappingSpan"/> of the element.
        /// </summary>
        IMappingSpan Span { get; }

        /// <summary>
        /// Determines whether the text in the span should be rendered in the <see cref="ITextViewLine"/>.
        /// </summary>
        bool ShouldRenderText { get; }
    }
}