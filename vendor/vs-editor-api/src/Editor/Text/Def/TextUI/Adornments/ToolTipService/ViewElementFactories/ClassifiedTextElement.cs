namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    /// <summary>
    /// Represents a block of classified text in an <see cref="IToolTipService"/> <see cref="IToolTipPresenter"/>.
    /// </summary>
    /// <remarks>
    /// Classified text is a span of text with a corresponding classification type name. On
    /// <see cref="IToolTipPresenter.StartOrUpdate(ITrackingSpan, System.Collections.Generic.IEnumerable{object})"/>,
    /// the classified text is converted to a platform-specific block of runs of formatted (colorized) text via
    /// the <see cref="IViewElementFactoryService"/> and is displayed.
    /// </remarks>
    public sealed class ClassifiedTextElement
    {
        /// <summary>
        /// Creates a new instance of classified text.
        /// </summary>
        /// <param name="runs">A sequence of zero or more runs of classified text.</param>
        public ClassifiedTextElement(params ClassifiedTextRun[] runs)
        {
            this.Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException(nameof(runs));
        }

        /// <summary>
        /// Creates a new instance of classified text.
        /// </summary>
        /// <param name="runs">A sequence of zero or more runs of classified text.</param>
        public ClassifiedTextElement(IEnumerable<ClassifiedTextRun> runs)
        {
            this.Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException(nameof(runs));
        }

        /// <summary>
        /// A sequence of classified runs of text.
        /// </summary>
        public IEnumerable<ClassifiedTextRun> Runs { get; }

        /// <summary>
        /// Creates a new element with a hyperlink.
        /// </summary>
        /// <param name="text">The text rendered by this run.</param>
        /// <param name="tooltip">The tooltip for the hyperlink.</param>
        /// <param name="navigationAction">The action to execute on navigation.</param>
        /// <returns><see cref="ClassifiedTextElement"/> containing the hyperlink.</returns>
        public static ClassifiedTextElement CreateHyperlink(string text, string tooltip, Action navigationAction)
        {
            Requires.NotNull(text, nameof(text));
            Requires.NotNull(navigationAction, nameof(navigationAction));
            return new ClassifiedTextElement(new ClassifiedTextRun("url", text, navigationAction, tooltip));
        }
    }
}
