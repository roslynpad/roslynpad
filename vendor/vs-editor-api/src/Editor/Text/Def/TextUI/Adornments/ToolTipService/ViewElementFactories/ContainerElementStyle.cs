namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;

#pragma warning disable CA1714 // Flags enums should have plural names
    /// <summary>
    /// The layout style for a <see cref="ContainerElement"/>.
    /// </summary>
    [Flags]
    public enum ContainerElementStyle
#pragma warning restore CA1714 // Flags enums should have plural names
    {
        /// <summary>
        /// Contents are end-to-end, and wrapped when the control becomes too wide.
        /// </summary>
        Wrapped = 0b_0000,

        /// <summary>
        /// Contents are stacked vertically.
        /// </summary>
        Stacked = 0b_0001,

        /// <summary>
        /// Additional padding above and below content.
        /// </summary>
        VerticalPadding = 0b_0010
    }
}
