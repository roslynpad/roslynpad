namespace Microsoft.VisualStudio.Editor
{
    /// <summary>
    /// Constants for interacting with <see cref="ICommonEditorAssetService"/> and Common Editor languages.
    /// </summary>
    public static class CommonEditorConstants
    {
        /// <summary>
        /// Name used to identify all Common Editor assets.
        /// </summary>
        public const string AssetName = "TextMate";

        /// <summary>
        /// Name of the content type under from which all TextMate based languages are derived.
        /// </summary>
        public const string ContentTypeName = "code++";

        /// <summary>
        /// Name of the registry key under which new repositories for TextMate grammars can be defined.
        /// </summary>
        public const string TextMateRepositoryKey = @"TextMate\Repositories";
    }
}
