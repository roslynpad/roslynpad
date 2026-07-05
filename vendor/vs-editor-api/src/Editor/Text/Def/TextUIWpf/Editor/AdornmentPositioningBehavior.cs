//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.
//  AdornmentPositioningBehavior").
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Specifies the positioning of adornments in an <see cref="IAdornmentLayer"/>.
    /// </summary>
    public enum AdornmentPositioningBehavior
    {
        /// <summary>
        /// The adornment is not moved by the view; the owner positions it.
        /// </summary>
        OwnerControlled,

        /// <summary>
        /// The adornment is positioned relative to the viewport (moves with scrolling).
        /// </summary>
        ViewportRelative,

        /// <summary>
        /// The adornment is positioned relative to its visual span's text.
        /// </summary>
        TextRelative,
    }
}
