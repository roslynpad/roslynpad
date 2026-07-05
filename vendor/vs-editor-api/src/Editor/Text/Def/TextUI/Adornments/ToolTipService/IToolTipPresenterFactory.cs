namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Proffers a platform-specific <see cref="IToolTipPresenter"/> to the IDE.
    /// </summary>
    /// <remarks>
    /// This class will always be constructed and called purely from the UI thread.
    /// Extenders can construct their own presenter and supersede the default
    /// one via MEF ordering. Presenter providers should return a new ToolTip each
    /// time they are called and should support multiple simultaneous open tips.
    /// </remarks>
    /// <example>
    /// [Export(typeof(IToolTipPresenterFactory))]
    /// [Name(nameof("super cool tooltip factory"))]
    /// [Order(Before = "default")]
    /// </example>
    public interface IToolTipPresenterFactory
    {
        /// <summary>
        /// Constructs a new instance of <see cref="IToolTipPresenter"/> for the current platform.
        /// </summary>
        /// <param name="textView">
        /// The view that owns the tooltip.
        /// </param>
        /// <param name="parameters">
        /// Parameters to create the tooltip with. Never null.
        /// </param>
        /// <returns>A <see cref="IToolTipPresenter"/> for the current platform.</returns>
        IToolTipPresenter Create(ITextView textView, ToolTipParameters parameters);
    }
}
