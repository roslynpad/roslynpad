namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Contains definitions for known <see cref="FeatureDefinition"/>s and their groupings.
    /// </summary>
    public static class PredefinedEditorFeatureNames
    {
        /// <summary>
        /// Definition of group of features that make up the core editor.
        /// </summary>
        public const string Editor = nameof(Editor);

        /// <summary>
        /// Definition of group of features that appear in a popup.
        /// </summary>
        public const string Popup = nameof(Popup);

        /// <summary>
        /// Definition of group of features that appear in an interactive popup.
        /// Descends from <see cref="Popup"/>
        /// </summary>
        public const string InteractivePopup = nameof(InteractivePopup);

        /// <summary>
        /// Definition of IntelliSense Completion.
        /// Descends from <see cref="InteractivePopup"/> and <see cref="Editor"/>
        /// </summary>
        public const string Completion = nameof(Completion);

        /// <summary>
        /// Definition of IntelliSense Completion.
        /// Descends from <see cref="InteractivePopup"/> and <see cref="Editor"/>
        /// </summary>
        public const string AsyncCompletion = nameof(AsyncCompletion);
    }
}
