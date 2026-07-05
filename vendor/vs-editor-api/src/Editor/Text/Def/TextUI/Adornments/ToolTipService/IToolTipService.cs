namespace Microsoft.VisualStudio.Text.Adornments
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Cross platform service for the creation and management of ToolTips.
    /// </summary>
    /// <remarks>
    /// This class is a MEF component part and it can be imported via the code in the example.
    /// </remarks>
    /// <example>
    /// [Import]
    /// internal IToolTipService tooltipService;
    /// </example>
    public interface IToolTipService
    {
        /// <summary>
        /// Creates a new non-visible ToolTip presenter.
        /// </summary>
        /// <param name="textView">
        /// The view that owns the tooltip.
        /// </param>
        /// <param name="parameters">
        /// Parameters to create the tooltip with. Default is mouse tracking.
        /// </param>
        /// <returns>A new non-visible <see cref="IToolTipPresenter"/>.</returns>
        IToolTipPresenter CreatePresenter(ITextView textView, ToolTipParameters parameters = null);
    }
}
