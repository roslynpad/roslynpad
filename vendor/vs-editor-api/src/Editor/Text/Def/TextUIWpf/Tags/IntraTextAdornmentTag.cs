//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Tagging.IntraTextAdornmentTag").
//  UIElement becomes Control per PLAN §4.2. Derives from the vendored cross-platform
//  tag so the shared sequencing machinery consumes both shapes.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Avalonia.Controls;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// A tag that provides an adornment rendered in the flow of the text, with space
    /// negotiated by the formatter (PLAN §5.5).
    /// </summary>
    public class IntraTextAdornmentTag : XPlatIntraTextAdornmentTag
    {
        /// <summary>
        /// Initializes a tag whose adornment is measured during formatting (the desired
        /// size of the control determines the negotiated space).
        /// </summary>
        public IntraTextAdornmentTag(Control adornment, AdornmentRemovedCallback removalCallback, PositionAffinity? affinity = null)
            : base(adornment, Wrap(adornment, removalCallback), affinity)
        {
        }

        /// <summary>
        /// Initializes a tag with explicit space metrics.
        /// </summary>
        public IntraTextAdornmentTag(
            Control adornment,
            AdornmentRemovedCallback removalCallback,
            double? topSpace,
            double? baseline,
            double? textHeight,
            double? bottomSpace,
            PositionAffinity? affinity)
            : base(adornment, Wrap(adornment, removalCallback), topSpace, baseline, textHeight, bottomSpace, affinity)
        {
        }

        /// <summary>
        /// Gets the adornment control.
        /// </summary>
        public new Control Adornment => (Control)base.Adornment;

        private static XPlatAdornmentRemovedCallback Wrap(Control adornment, AdornmentRemovedCallback removalCallback)
            => removalCallback is null ? null : (tag, _) => removalCallback(tag, adornment);
    }
}
