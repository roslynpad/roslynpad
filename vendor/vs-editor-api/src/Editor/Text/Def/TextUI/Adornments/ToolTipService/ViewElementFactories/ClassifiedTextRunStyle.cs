namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;

#pragma warning disable CA1714 // Flags enums should have plural names
    /// <summary>
    /// The text style for a <see cref="ClassifiedTextRun"/>.
    /// </summary>
    [Flags]
    public enum ClassifiedTextRunStyle
#pragma warning restore CA1714 // Flags enums should have plural names
    {
        /// <summary>
        /// Plain text.
        /// </summary>
        Plain = 0b_0000,

        /// <summary>
        /// Bolded text.
        /// </summary>
        Bold = 0b_0001,

        /// <summary>
        /// Italic text.
        /// </summary>
        Italic = 0b_0010,

        /// <summary>
        /// Underlined text.
        /// </summary>
        Underline = 0b_0100,

        /// <summary>
        /// Use the font specified by the classification.
        /// </summary>
        UseClassificationFont = 0b_1000,
    }
}
