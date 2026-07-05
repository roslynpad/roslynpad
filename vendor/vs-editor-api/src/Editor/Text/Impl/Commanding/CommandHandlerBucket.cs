using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Commanding;

namespace Microsoft.VisualStudio.UI.Text.Commanding.Implementation
{
    /// <summary>
    /// Lightweight stack-like view over a readonly ordered list of command handlers.
    /// </summary>
    internal class CommandHandlerBucket
    {
        private int _currentCommandHandlerIndex;
        private readonly IReadOnlyList<Lazy<ICommandHandler, CommandHandlerMetadata>> _commandHandlers;

        public CommandHandlerBucket(IReadOnlyList<Lazy<ICommandHandler, CommandHandlerMetadata>> commandHandlers)
        {
            _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
        }

        public bool IsEmpty => _currentCommandHandlerIndex >= _commandHandlers.Count;

        public Lazy<ICommandHandler, CommandHandlerMetadata> Peek()
        {
            if (!IsEmpty)
            {
                return _commandHandlers[_currentCommandHandlerIndex];
            }

            throw new InvalidOperationException($"{nameof(CommandHandlerBucket)} is empty.");
        }

        internal Lazy<ICommandHandler, CommandHandlerMetadata> Pop()
        {
            if (!IsEmpty)
            {
                return _commandHandlers[_currentCommandHandlerIndex++];
            }

            throw new InvalidOperationException($"{nameof(CommandHandlerBucket)} is empty.");
        }
    }
}
