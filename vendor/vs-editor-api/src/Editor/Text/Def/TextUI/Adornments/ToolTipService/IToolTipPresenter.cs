namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// A platform-specific ToolTip implementation.
    /// </summary>
    /// <remarks>
    /// This type is proffered to the IDE via an <see cref="IToolTipPresenterFactory"/> and is
    /// always constructed and called purely on the UI thread. Each <see cref="IToolTipPresenter"/>
    /// is a single-use object that is responsible for converting the given content to
    /// into platform-specific UI elements and displaying them in a popup UI.
    /// </remarks>
    public interface IToolTipPresenter
    {
        /// <summary>
        /// Invoked upon dismissal of the ToolTip's popup view.
        /// </summary>
        /// <remarks>
        /// This event should be fired regardless of the reason for the popup's dismissal.
        /// </remarks>
        event EventHandler Dismissed;

        /// <summary>
        /// Constructs a popup containing a platform-specific UI representation of <paramref name="content"/>.
        /// </summary>
        /// <remarks>
        /// This method can be called multiple times to refresh the content and applicableToSpan.
        /// </remarks>
        /// <param name="applicableToSpan">The span of text for which the tooltip is kept open.</param>
        /// <param name="content">
        /// A platform independent representation of the tooltip content. <see cref="IToolTipPresenter"/>s
        /// should use the <see cref="IViewElementFactoryService"/> to convert <paramref name="content"/>
        /// to platform specific UI elements.
        /// </param>
        void StartOrUpdate(ITrackingSpan applicableToSpan, IEnumerable<object> content);

        /// <summary>
        /// Dismisses the popup and causes <see cref="Dismissed"/> to be fired.
        /// </summary>
        void Dismiss();
    }
}
