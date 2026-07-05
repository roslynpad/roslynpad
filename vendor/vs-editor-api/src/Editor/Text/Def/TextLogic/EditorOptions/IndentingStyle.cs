namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// The automatic indentation style applied to new lines in the editor.
    /// </summary>
    public enum IndentingStyle
    {
        /// <summary>
        /// No automatic indentation.
        /// </summary>
        None,

        /// <summary>
        /// New lines are indented to match the previous line.
        /// </summary>
        Block,

        /// <summary>
        /// New lines are indented based on language semantics.
        /// </summary>
        Smart,
    }
}
