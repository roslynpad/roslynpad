//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    /// <summary>
    /// Represents a sequence element that consists of an adornment.
    /// </summary>
    public interface IAdornmentElement : ISequenceElement
    {
        /// <summary>
        /// Gets the width of the adornment (in logical pixels).
        /// </summary>
        double Width { get; }

        /// <summary>
        /// Gets the amount of space (in logical pixels) to reserve above top of the text for the <see cref="ITextViewLine"/>.
        /// </summary>
        double TopSpace { get; }

        /// <summary>
        /// The distance (in logical pixel)s between the top of the adornment text and the baseline of the
        /// <see cref="ITextViewLine"/>.
        /// </summary>
        /// <remarks><para>This property should be equal to <see cref="TextHeight"/> unless you plan to draw into the space between the baseline of
        /// <see cref="ITextViewLine"/> and its TextBottom.</para>
        /// <para>The size of the baseline affects the amount of space reserved for text on an <see cref="ITextViewLine"/>, which is used to
        /// determine the vertical size of the caret.</para>
        /// </remarks>
        double Baseline { get; }

        /// <summary>
        /// Gets the height of the adornment text. 
        /// </summary>
        /// <remarks><para>This affects the amount of space reserved for text on an <see cref="ITextViewLine"/>, which is used to
        /// determine the vertical size of the caret.</para></remarks>
        double TextHeight { get; }

        /// <summary>
        /// The amount of space (in logical pixels) to reserve below the bottom of the text in the <see cref="ITextViewLine"/>.
        /// </summary>
        double BottomSpace { get; }

        /// <summary>
        /// Gets the unique identifier associated with this adornment.
        /// </summary>
        /// <remarks>This ID can be passed to <see cref="ITextViewLine"/>.GetAdornmentBounds() to find the location
        /// of this adornment on a line in the view.</remarks>
        object IdentityTag { get; }

        /// <summary>
        /// Gets the unique identifier associated with the provider of the adornment.
        /// </summary>
        /// <remarks>This ID can be passed to <see cref="ITextViewLine.GetAdornmentTags"/> to find the list
        /// off adornment identity tags located on the line.</remarks>
        object ProviderTag { get; }

        /// <summary>
        /// Gets the <see cref="PositionAffinity"/> of the adornment.
        /// </summary>
        /// <remarks>This is used only when the length of the adornment element span in the source buffer is zero.</remarks>
        PositionAffinity Affinity { get; }
    }
}