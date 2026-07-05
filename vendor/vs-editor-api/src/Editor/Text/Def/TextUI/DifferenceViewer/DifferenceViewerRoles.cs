//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The text view roles associated with an <see cref="IDifferenceViewer"/>.
    /// </summary>
    public static class DifferenceViewerRoles
    {
        /// <summary>
        /// The text view role for any view owned by an <see cref="IDifferenceViewer"/> when the underlying difference buffer will never have a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string DiffTextViewRole = "DIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.LeftView"/> when the underlying difference buffer will never have a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string LeftViewTextViewRole = "LEFTDIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.RightView"/> when the underlying difference buffer will never have a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string RightViewTextViewRole = "RIGHTDIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.InlineView"/> when the underlying difference buffer will never have a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string InlineViewTextViewRole = "INLINEDIFF";

        /// <summary>
        /// The text view role for any view owned by an <see cref="IDifferenceViewer"/> when the underlying difference buffer supports a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string UbiquitousDiffTextViewRole = "UBIDIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.LeftView"/> when the underlying difference buffer supports a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string UbiquitousLeftViewTextViewRole = "UBILEFTDIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.RightView"/> when the underlying difference buffer supports a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string UbiquitousRightViewTextViewRole = "UBIRIGHTDIFF";

        /// <summary>
        /// The text view role for the <see cref="IDifferenceViewer.InlineView"/> when the underlying difference buffer supports a null <see cref="IDifferenceBuffer.BaseLeftBuffer"/>.
        /// </summary>
        public const string UbiquitousInlineViewTextViewRole = "UBIINLINEDIFF";
    }
}
