namespace Microsoft.VisualStudio.Language.Intellisense
{
    using System;

    /// <summary>
    /// Arguments for the <see cref="IAsyncQuickInfoSession.StateChanged"/> event.
    /// </summary>
    public sealed class QuickInfoSessionStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="QuickInfoSessionStateChangedEventArgs"/>.
        /// </summary>
        /// <param name="oldState">The state before the transition.</param>
        /// <param name="newState">The state after the transition.</param>
        public QuickInfoSessionStateChangedEventArgs(QuickInfoSessionState oldState, QuickInfoSessionState newState)
        {
            this.OldState = oldState;
            this.NewState = newState;
        }

        /// <summary>
        /// The state before the transition.
        /// </summary>
        public QuickInfoSessionState OldState { get; }

        /// <summary>
        /// The state after the transition.
        /// </summary>
        public QuickInfoSessionState NewState { get; }
    }
}
