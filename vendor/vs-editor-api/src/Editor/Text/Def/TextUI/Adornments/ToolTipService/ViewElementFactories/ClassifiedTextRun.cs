namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// Represents a contiguous run of classified text in an <see cref="IToolTipService"/> <see cref="IToolTipPresenter"/>.
    /// </summary>
    /// <remarks>
    /// Classified text runs live in <see cref="ClassifiedTextElement"/>s and are a string, classification pair. On
    /// <see cref="IToolTipPresenter.StartOrUpdate(ITrackingSpan, System.Collections.Generic.IEnumerable{object})"/>,
    /// the classified text is converted to a platform-specific run of formatted (colorized) text via
    /// the <see cref="IViewElementFactoryService"/> and is displayed.
    /// </remarks>
    public sealed class ClassifiedTextRun
    {
        /// <summary>
        /// Creates a new run of classified text.
        /// </summary>
        /// <param name="classificationTypeName">
        /// A name indicating a <see cref="IClassificationType"/> that maps to a format that will be applied to the text.
        /// </param>
        /// <param name="text">The text rendered by this run.</param>
        /// <remarks>
        /// Classification types can be platform specific. Only classifications defined in PredefinedClassificationTypeNames
        /// are supported cross platform.
        /// </remarks>
        public ClassifiedTextRun(string classificationTypeName, string text)
            : this(classificationTypeName, text, ClassifiedTextRunStyle.Plain)
        {
        }

        /// <summary>
        /// Creates a new run of classified text.
        /// </summary>
        /// <param name="classificationTypeName">
        /// A name indicating a <see cref="IClassificationType"/> that maps to a format that will be applied to the text.
        /// </param>
        /// <param name="text">The text rendered by this run.</param>
        /// <param name="style">The style that will be applied to the text.</param>
        /// <remarks>
        /// Classification types can be platform specific. Only classifications defined in PredefinedClassificationTypeNames
        /// are supported cross platform.
        /// </remarks>
        public ClassifiedTextRun(string classificationTypeName, string text, ClassifiedTextRunStyle style)
        {
            this.ClassificationTypeName = classificationTypeName
                ?? throw new ArgumentNullException(nameof(classificationTypeName));
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.Style = style;
        }

        /// <summary>
        /// Creates a new run of classified text.
        /// </summary>
        /// <param name="classificationTypeName">
        /// A name indicating a <see cref="IClassificationType"/> that maps to a format that will be applied to the text.
        /// </param>
        /// <param name="text">The text rendered by this run.</param>
        /// <param name="style">The style that will be applied to the text.</param>
        /// <remarks>
        /// Classification types can be platform specific. Only classifications defined in PredefinedClassificationTypeNames
        /// are supported cross platform.
        /// </remarks>
        public ClassifiedTextRun(
            string classificationTypeName,
            string text,
            Action navigationAction,
            string tooltip = null,
            ClassifiedTextRunStyle style = ClassifiedTextRunStyle.Plain)
        {
            this.ClassificationTypeName = classificationTypeName
                ?? throw new ArgumentNullException(nameof(classificationTypeName));
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.Style = style;

            this.NavigationAction = navigationAction ?? throw new ArgumentNullException(nameof(navigationAction));
            this.Tooltip = tooltip;
        }

        /// <summary>
        /// The name of the classification which maps to formatting properties that will be applied to this text.
        /// </summary>
        public string ClassificationTypeName { get; }

        /// <summary>
        /// The text that will be formatted by <see cref="ClassificationTypeName"/>'s corresponding formatting.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The style that will be applied to the text.
        /// </summary>
        public ClassifiedTextRunStyle Style { get; }

        /// <summary>
        /// The text that will be displayed on the hyperlink tooltip.
        /// </summary>
        public string Tooltip { get; } = null;

        /// <summary>
        /// The navigation action for the hyperlink.
        /// </summary>
        public Action NavigationAction { get; } = null;

        /// <summary>
        /// The marker tag type associated with this run, if any (used by LSP-based features).
        /// </summary>
        public string MarkerTagType { get; set; } = null;
    }
}
