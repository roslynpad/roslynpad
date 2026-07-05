// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using Microsoft.VisualStudio.Imaging.Interop;

    /// <summary>
    /// Associates a LightBulb control icon for a suggested action category.
    /// </summary>
    /// <remarks> 
    /// <see cref="SuggestedActionCategoryToIconDefinition"/>s are associations between
    /// <see cref="SuggestedActionCategoryDefinition"/>s and <see cref="ImageMoniker"/>s
    /// that define an icon for a particular <see cref="ISuggestedActionCategory"/>. Icons
    /// are inheritable and icons defined for base categories will be displayed for their
    /// children as well, unless the child defines its own icon. Icon definitions are joined
    /// with category definitions on the Name attribute. The category's precedence determines
    /// which category's icon is displayed in the LightBulb control.
    /// </remarks>
    /// <code>
    /// internal sealed class Components
    /// {
    ///     [Export]
    ///     [Name("New Category Definition")]
    ///     private SuggestedActionCategoryToIconDefinition anySuggestedActionCategoryToIconDefinition
    ///         = new SuggestedActionCategoryToIconDefinition(KnownMonikers.IntellisenseLightBulb);
    /// }
    /// </code>
    [CLSCompliant(false)]
    public sealed class SuggestedActionCategoryToIconDefinition
    {
        /// <summary>
        /// Creates a new instance with the specified <see cref="ImageMoniker"/>.
        /// </summary>
        /// <remarks>
        /// When a LightBulb session is created, the highest precedence category
        /// based upon MEF Ordering of the <see cref="SuggestedActionCategoryDefinition"/>s
        /// with applicable <see cref="ISuggestedAction2"/>s will have its icon displayed in
        /// the LightBulb control.
        /// </remarks>
        /// <param name="imageMoniker">The <see cref="ImageMoniker"/> of the icon to display.</param>
        public SuggestedActionCategoryToIconDefinition(ImageMoniker imageMoniker)
        {
            this.ImageMoniker = imageMoniker;
        }

        /// <summary>
        /// Gets the <see cref="ImageMoniker"/> to associate with the named <see cref="ISuggestedActionCategory"/>.
        /// </summary>
        public ImageMoniker ImageMoniker { get; set; }
    }
}
