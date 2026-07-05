// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Represents a LightBulb broker, which is globally responsible for managing <see cref="ILightBulbSession"/>s.
    /// </summary>
    /// <remarks>This is a MEF component, and should be imported as follows:
    /// [Import]
    /// ILightBulbBroker2 lightBulbBroker = null;
    /// </remarks>
    [CLSCompliant(false)]
    public interface ILightBulbBroker2 : ILightBulbBroker
    {
        /// <summary>
        /// Asynchronously gets an <see cref="ISuggestedActionCategorySet"/> containing all categories with applicable actions.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine whether any <see cref="ISuggestedAction"/>s 
        /// are associated with the current caret position.</param>
        /// <param name="cancellationToken">Cancellation token to cancel this asynchronous operation.</param>
        /// <returns>
        /// A task that returns the <see cref="ISuggestedActionCategorySet"/> of categories with applicable actions.
        /// </returns>
        Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            ITextView textView,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously gets an <see cref="ISuggestedActionCategorySet"/> containing all categories with applicable actions.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to determine whether any <see cref="ISuggestedAction"/>s 
        /// are associated with the current caret position.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint" /> in the text buffer at which to determine whether any <see cref="ISuggestedAction"/>s are
        /// associated with a given point position and span in a given <see cref="ITextView"/>.</param>
        /// <param name="trackingSpan">The <see cref="ITrackingSpan" /> in the text buffer for which to determine whether any <see cref="ISuggestedAction"/>s are 
        /// associated with a given trigger point position and span in a given <see cref="ITextView"/>.</param>
        /// <param name="cancellationToken">Cancellation token to cancel this asynchronous operation.</param>
        /// <returns>
        /// A task that returns the <see cref="ISuggestedActionCategorySet"/> of categories with applicable actions.
        /// </returns>
        Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            ITextView textView,
            ITrackingPoint triggerPoint,
            ITrackingSpan trackingSpan,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates, but doesn't expand an <see cref="ILightBulbSession"/> for a given <see cref="ITextView"/> with current caret position
        /// as a trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="applicableCategories">
        /// The <see cref="ISuggestedActionCategorySet"/> of categories with applicable actions.
        /// </param>
        /// <returns>A valid instance of <see cref="ILightBulbSession"/> or null if no <see cref="ILightBulbSession"/> can be created for
        /// given text view and caret position.</returns>
        ILightBulbSession CreateSession(
            ISuggestedActionCategorySet requestedActionCategories,
            ITextView textView,
            ISuggestedActionCategorySet applicableCategories);

        /// <summary>
        /// Creates, but doesn't expand an <see cref="ILightBulbSession"/> for a given <see cref="ITextView"/> with current caret position
        /// as a trigger point.
        /// </summary>
        /// <param name="requestedActionCategories">A set of suggested action categories requested.</param>
        /// <param name="textView">The <see cref="ITextView"/> over which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="triggerPoint">The <see cref="ITrackingPoint"/> in the text buffer at which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name="trackingSpan">The <see cref="ITrackingSpan"/> in the text buffer for which to create an <see cref="ILightBulbSession"/>.</param>
        /// <param name = "applicableCategories" >
        /// The <see cref="ISuggestedActionCategorySet"/> of categories with applicable actions.
        /// </param>
        /// <param name="trackMouse">Determines whether the session should track the mouse.</param>
        /// <returns>A valid instance of <see cref="ILightBulbSession"/> or null if no <see cref="ILightBulbSession"/> can be created for
        /// given text view and caret position.</returns>
        ILightBulbSession CreateSession(
            ISuggestedActionCategorySet requestedActionCategories,
            ITextView textView,
            ITrackingPoint triggerPoint,
            ITrackingSpan trackingSpan,
            ISuggestedActionCategorySet applicableCategories,
            bool trackMouse);
    }
}
