using System;
using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Options controlling the automatic-cancellation behavior of a background work indicator.
    /// </summary>
    public sealed class BackgroundWorkIndicatorOptions
    {
        /// <summary>
        /// Whether the indicator is canceled when the user edits the buffer.
        /// </summary>
        public bool CancelOnEdit { get; set; } = true;

        /// <summary>
        /// Whether the indicator is canceled when the view loses focus.
        /// </summary>
        public bool CancelOnFocusLost { get; set; } = true;
    }

    /// <summary>
    /// An unobtrusive indicator that background work is happening, shown near a span of text.
    /// The work is cancellable by the user, and optionally canceled automatically on edit or
    /// focus loss.
    /// </summary>
    public interface IBackgroundWorkIndicator : IDisposable
    {
        /// <summary>
        /// Canceled when the user dismisses the indicator or an auto-cancel condition triggers.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds a scope of work with its own description.
        /// </summary>
        BackgroundWorkOperationScope AddScope(string description);

        /// <summary>
        /// Temporarily suppresses the auto-cancel behaviors (cancel on edit / focus lost)
        /// until the returned value is disposed.
        /// </summary>
        IDisposable SuppressAutoCancel();
    }

    /// <summary>
    /// Creates <see cref="IBackgroundWorkIndicator"/>s for a text view.
    /// </summary>
    public interface IBackgroundWorkIndicatorService
    {
        /// <summary>
        /// Creates a background work indicator applying to the given span.
        /// </summary>
        IBackgroundWorkIndicator Create(ITextView textView, SnapshotSpan applicableToSpan, string description, BackgroundWorkIndicatorOptions options);
    }
}
