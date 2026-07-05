using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// What caused the completion to trigger or update.
    /// Location is not provided in this struct because Editor maps the location
    /// to an appropriate buffer for each <see cref="IAsyncCompletionSource"/>.
    /// </summary>
    [DebuggerDisplay("{Reason} {Character}")]
    public struct CompletionTrigger : IEquatable<CompletionTrigger>
    {
        /// <summary>
        /// The reason that completion action was taken.
        /// </summary>
        public CompletionTriggerReason Reason { get; }

        /// <summary>
        /// The text edit associated with the action.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// <see cref="ITextSnapshot"/> on the view's text buffer before the completion action was taken.
        /// For <see cref="CompletionTriggerReason.Backspace"/>, <see cref="CompletionTriggerReason.Deletion"/> and <see cref="CompletionTriggerReason.Insertion"/>,
        /// this is text snapshot before the edit has been made. You may use it to get higher fidelity data on text edits that led to this action.
        /// If there was no edit, or edit is unavailable, this is the <see cref="ITextSnapshot"/> at the time action happened.
        /// Take precaution when accessing this property: since this is a struct, it may be left uninitialized.
        /// </summary>
        public ITextSnapshot ViewSnapshotBeforeTrigger { get; }

        /// <summary>
        /// Creates a <see cref="CompletionTrigger"/> not associated with a text edit
        /// </summary>
        /// <param name="reason">The kind of action that triggered completion action</param>
        /// <param name="snapshotBeforeTrigger">Snapshot on the view's text buffer when action was taken</param>
        public CompletionTrigger(CompletionTriggerReason reason, ITextSnapshot snapshotBeforeTrigger) : this(reason, snapshotBeforeTrigger, default)
        { }

        /// <summary>
        /// Creates a <see cref="CompletionTrigger"/> associated with a text edit
        /// </summary>
        /// <param name="reason">The kind of action that caused completion to trigger or update</param>
        /// <param name="character">Character associated with the action</param>
        /// <param name="snapshotBeforeTrigger">Snapshot on the view's text buffer before or when action was taken</param>
        public CompletionTrigger(CompletionTriggerReason reason, ITextSnapshot snapshotBeforeTrigger, char character)
        {
            this.Reason = reason;
            this.Character = character;
            this.ViewSnapshotBeforeTrigger = snapshotBeforeTrigger ?? throw new ArgumentNullException(nameof(snapshotBeforeTrigger));
        }

        bool IEquatable<CompletionTrigger>.Equals(CompletionTrigger other) =>
            Reason.Equals(other.Reason)
            && Character.Equals(other.Character)
            && ViewSnapshotBeforeTrigger.Equals(other.ViewSnapshotBeforeTrigger);

        public override bool Equals(object other) => (other is CompletionTrigger otherTrigger) ? ((IEquatable<CompletionTrigger>)this).Equals(otherTrigger) : false;

        public static bool operator ==(CompletionTrigger left, CompletionTrigger right) => left.Equals(right);

        public static bool operator !=(CompletionTrigger left, CompletionTrigger right) => !(left == right);

        public override int GetHashCode() => Reason.GetHashCode() ^ Character.GetHashCode();
    }
}
