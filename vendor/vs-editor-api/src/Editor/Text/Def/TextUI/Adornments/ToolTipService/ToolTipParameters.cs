namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Determines behavior for a <see cref="IToolTipPresenter"/>.
    /// </summary>
    public sealed class ToolTipParameters
    {
        private readonly Func<bool> keepOpenFunc;

        /// <summary>
        /// Default options for a mouse tracking tooltip.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is readonly")]
        public static readonly ToolTipParameters Default = new ToolTipParameters();

        /// <summary>
        /// Creates a new instance of <see cref="ToolTipParameters"/>.
        /// </summary>
        /// <param name="trackMouse">
        /// If true, dismisses the tooltip when the mouse leaves the applicable span.
        /// </param>
        /// <param name="ignoreBufferChange">
        /// If true, and if the tooltip is mouse tracking, does not dismiss when the buffer changes.
        /// </param>
        /// <param name="keepOpenFunc">
        /// A callback function that determines wehther or not to keep open the tooltip
        /// in mouse tracking sessions, despite the mouse being outside the tooltip.
        /// </param>
        public ToolTipParameters(
            bool trackMouse = true,
            bool ignoreBufferChange = false,
            Func<bool> keepOpenFunc = null)
        {
            if (trackMouse && ignoreBufferChange)
            {
                throw new ArgumentException($"{nameof(ignoreBufferChange)} can only be true if {nameof(trackMouse)} is false");
            }

            this.TrackMouse = trackMouse;
            this.IgnoreBufferChange = ignoreBufferChange;
            this.keepOpenFunc = keepOpenFunc;
        }

        /// <summary>
        /// Gets whether or not the tooltip can be dismissed by the mouse leaving the
        /// applicable span.
        /// </summary>
        public bool TrackMouse { get; }

        /// <summary>
        /// Gets whether or not the tooltip is closed when the buffer changes.
        /// </summary>
        public bool IgnoreBufferChange { get; }

        /// <summary>
        /// Gets whether or not the tooltip should stay open even if the
        /// mouse is outside of the tip.
        /// </summary>
        public bool KeepOpen => this.keepOpenFunc?.Invoke() ?? false;
    }
}
