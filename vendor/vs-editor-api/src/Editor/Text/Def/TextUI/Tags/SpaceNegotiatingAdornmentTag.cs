//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Represents a tag for a space-negotiating adornment. The tag is used to provide space
    /// for positioning the adornment in a view.
    /// </summary>
    /// <remarks>
    /// The units used in properties like <see cref="Width"/> and <see cref="TopSpace"/> are those used in the presentation technology.
    /// </remarks>
    public class SpaceNegotiatingAdornmentTag : ITag
    {
        /// <summary>
        /// Gets the width of the adornment.
        /// </summary>
        public double Width { get; private set; }

        /// <summary>
        /// Gets the amount of space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/>.
        /// </summary>
        public double TopSpace { get; private set; }

        /// <summary>
        /// Gets the baseline of the space-negotiating adornment.
        /// </summary>
        public double Baseline { get; private set; }

        /// <summary>
        /// Gets the height of the text portion of the space-negotiating adornment.
        /// </summary>
        public double TextHeight { get; private set; }

        /// <summary>
        /// Gets the amount of space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the botton of the <see cref="ITextViewLine"/>.
        /// </summary>
        public double BottomSpace { get; private set; }

        /// <summary>
        /// Gets the <see cref="PositionAffinity"/> of the space-negotiating adornment.
        /// </summary>
        /// <remarks>
        /// This property is only used for adornments that do not replace text.
        /// An adornment does not replace text if its tag has a zero-length span on the view's text buffer.
        /// </remarks>
        public PositionAffinity Affinity { get; private set; }

        /// <summary>
        /// Gets a unique object associated with the space-negotiating adornment, which is used by <see cref="ITextViewLine"/>.GetAdornmentBounds.
        /// </summary>
        public object IdentityTag { get; private set; }

        /// <summary>
        /// Gets a unique object that identifies the provider of the adornment.
        /// </summary>
        /// <remarks>
        /// This object is used to get adornments by calling <see cref="ITextViewLine.GetAdornmentTags"/>.
        /// </remarks>
        public object ProviderTag { get; private set; }

        /// <summary>
        /// Initializes a new instance of a <see cref="SpaceNegotiatingAdornmentTag"/> with the specified properties.
        /// </summary>
        /// <param name="width">The width of the tag in pixels.</param>
        /// <param name="topSpace">The space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/>.</param>
        /// <param name="baseline">The baseline of the space-negotiating adornment.</param>
        /// <param name="textHeight">The height in pixels of the text portion of the space-negotiating adornment.</param>
        /// <param name="bottomSpace">The space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the botton of the <see cref="ITextViewLine"/>.</param>
        /// <param name="affinity">The <see cref="PositionAffinity"/> of the space-negotiating adornment.</param>
        /// <param name="identityTag">A unique object associated with the space-negotiating adornment, used by <see cref="ITextViewLine"/>.GetAdornmentBounds.</param>
        /// <param name="providerTag">A unique object identifying the provider of the adornment, used by <see cref="ITextViewLine.GetAdornmentTags"/>).</param>
        public SpaceNegotiatingAdornmentTag(double width, double topSpace, double baseline, double textHeight, double bottomSpace, PositionAffinity affinity, object identityTag, object providerTag)
        {
            this.Width = width;
            this.TopSpace = topSpace;
            this.Baseline = baseline;
            this.TextHeight = textHeight;
            this.BottomSpace = bottomSpace;
            this.Affinity = affinity;
            this.IdentityTag = identityTag;
            this.ProviderTag = providerTag;
        }
    }
}
