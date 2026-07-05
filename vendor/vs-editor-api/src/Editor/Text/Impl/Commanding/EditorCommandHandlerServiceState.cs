using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    internal class EditorCommandHandlerServiceState
    {
        private readonly ConcurrentStack<ICommandHandler> _executingCommandHandlers = new ConcurrentStack<ICommandHandler>();

        public CommandExecutionContext ExecutionContext { get; set; }
        public INamed CommandHandlerExecutingDuringCancellationRequest { get; set; }
        public bool ExecutionHasTimedOut { get; set; }
        public EditorCommandArgs ExecutingCommand { get; }
        public bool IsExecutingTypingCommand { get; }

        public EditorCommandHandlerServiceState(EditorCommandArgs executingCommand, bool isTypingCommand)
        {
            _executingCommandHandlers = new ConcurrentStack<ICommandHandler>();
            ExecutingCommand = executingCommand ?? throw new ArgumentNullException(nameof(executingCommand));
            IsExecutingTypingCommand = isTypingCommand;
        }

        public INamed GetCurrentlyExecutingCommandHander()
        {
            if (_executingCommandHandlers.TryPeek(out ICommandHandler handler) &&
                handler is INamed namedHandler)
            {
                return namedHandler;
            }

            return null;
        }

        public void OnExecutingCommandHandlerBegin(ICommandHandler handler)
        {
            _executingCommandHandlers.Push(handler);
        }

        public void OnExecutingCommandHandlerEnd(ICommandHandler handler)
        {
            bool success = _executingCommandHandlers.TryPop(out var topCommandHandler);
            Debug.Assert(success, "Unexpectedly empty command handler execution stack.");
            Debug.Assert(handler == topCommandHandler, "Unexpected command hanlder on top of the stack.");
        }

        public void OnExecutionCancellationRequested()
        {
            CommandHandlerExecutingDuringCancellationRequest = GetCurrentlyExecutingCommandHander();
        }
    }
}
