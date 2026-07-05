namespace Microsoft.VisualStudio.Text.Adornments
{
    public interface IToolTipPresenter2 : IToolTipPresenter
    {
        /// <summary>
        /// Gets a value indicating whether the mouse pointer is located over this interactive Quick Info content,
        /// including any parts that are out of the Quick Info visual tree (such as popups).
        /// </summary>
        bool IsMouseOverAggregated { get; }
    }
}
