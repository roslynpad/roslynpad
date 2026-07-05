using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.CodeCleanUp
{
    /// <summary>
    /// Represents an execution context for fixers setup by the command handling system
    /// </summary>
    public interface ICodeCleanUpExecutionContext
    {
        /// <summary>
        /// Gets a context of executing potentially long running operation on the UI thread, which
        /// enables shared two way cancellability and wait indication
        /// </summary>
        IUIThreadOperationContext OperationContext { get; }

        /// <summary>
        /// Gets fix identifiers which are enabled
        /// </summary>
        FixIdContainer EnabledFixIds { get; }
    }
}
