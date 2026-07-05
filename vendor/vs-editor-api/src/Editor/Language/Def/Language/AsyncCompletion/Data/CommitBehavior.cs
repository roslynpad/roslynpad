using System;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Instructs the editor how to behave after committing a <see cref="CompletionItem"/>.
    /// </summary>
    [Flags]
#pragma warning disable CA1714 // Flags enums should have plural names
    public enum CommitBehavior
#pragma warning restore CA1714 // Flags enums should have plural names
    {
        /// <summary>
        /// Use the default behavior,
        /// that is, to propagate TypeChar command, but surpress ReturnKey and TabKey commands.
        /// </summary>
        None = 0b0000,

        /// <summary>
        /// Surpresses further invocation of the TypeChar command handlers.
        /// By default, editor invokes these command handlers to enable features such as brace completion.
        /// </summary>
        SuppressFurtherTypeCharCommandHandlers = 0b0001,

        /// <summary>
        /// Raises further invocation of the ReturnKey and Tab command handlers.
        /// By default, editor doesn't invoke ReturnKey and Tab command handlers after committing completion session.
        /// </summary>
        RaiseFurtherReturnKeyAndTabKeyCommandHandlers = 0b0010,

        /// <summary>
        /// Cancels the commit operation, does not call any other <see cref="IAsyncCompletionCommitManager.TryCommit(IAsyncCompletionSession, Text.ITextBuffer, CompletionItem, char, System.Threading.CancellationToken)"/>.
        /// Functionally, acts as if the typed character was not a commit character,
        /// allowing the user to continue working with the <see cref="IAsyncCompletionSession"/>
        /// </summary>
        CancelCommit = 0b0100,
    }
}
