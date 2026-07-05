using System;

namespace Microsoft.VisualStudio.Commanding
{
    /// <summary>
    /// Contains command handler utility extension methods.
    /// </summary>
    public static class CommandHandlerExtensions
    {
        /// <summary>
        /// Called to determine the state of the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A command handler can implement <see cref="ICommandHandler{T}"/> or
        /// <see cref="IChainedCommandHandler{T}"/>, but either way this method returns
        /// the final state of the command as returned by either this or next
        /// command handler.
        /// </para>
        /// <para>If <paramref name="commandHandler"/> implements <see cref="ICommandHandler{T}"/>,
        /// its <see cref="ICommandHandler{T}.GetCommandState(T)"/> method is called. If it returns
        /// <see cref="CommandState.Unspecified"/>, <paramref name="nextCommandHandler"/> is invoked.
        ///</para>
        ///<para>
        /// If <paramref name="commandHandler"/> implements <see cref="IChainedCommandHandler{T}"/>,
        /// its <see cref="IChainedCommandHandler{T}.GetCommandState(T, Func{CommandState})"/> method is invoked with
        /// <paramref name="nextCommandHandler"/> passed as an argument.
        ///</para>
        /// </remarks>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <param name="nextCommandHandler">The next command handler in the command execution chain.</param>
        /// <param name="commandHandler">A command handler to query the state of the command.</param>
        /// <returns>A <see cref="CommandState"/> instance that contains information on the availability of the command.</returns>
        public static CommandState GetCommandState<T>(this ICommandHandler commandHandler, T args, Func<CommandState> nextCommandHandler) where T : CommandArgs
        {
            if (commandHandler == null)
            {
                throw new ArgumentNullException(nameof(commandHandler));
            }

            if (nextCommandHandler == null)
            {
                throw new ArgumentNullException(nameof(nextCommandHandler));
            }

            if (commandHandler is ICommandHandler<T> simpleCommandHandler)
            {
                var commandState = simpleCommandHandler.GetCommandState(args);
                if (commandState.IsUnspecified)
                {
                    return nextCommandHandler();
                }

                return commandState;
            }

            if (commandHandler is IChainedCommandHandler<T> chainedCommandHandler)
            {
                return chainedCommandHandler.GetCommandState(args, nextCommandHandler);
            }

            throw new ArgumentException($"Unsupported CommandHandler type: {commandHandler.GetType()}");
        }

        /// <summary>
        /// Called to execute the command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A command handler can implement <see cref="ICommandHandler{T}"/> or
        /// <see cref="IChainedCommandHandler{T}"/>, but either way this method executes
        /// the command by either this or next command handler.
        /// </para>
        /// <para>If <paramref name="commandHandler"/> implements <see cref="ICommandHandler{T}"/>,
        /// its <see cref="ICommandHandler{T}.ExecuteCommand(T, CommandExecutionContext)"/> method is called. If it returns
        /// <c>false</c>, <paramref name="nextCommandHandler"/> is invoked.
        ///</para>
        ///<para>
        /// If <paramref name="commandHandler"/> implements <see cref="IChainedCommandHandler{T}"/>,
        /// its <see cref="IChainedCommandHandler{T}.ExecuteCommand(T, Action, CommandExecutionContext)"/> method is invoked with
        /// <paramref name="nextCommandHandler"/> passed as an argument.
        ///</para>
        /// </remarks>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <param name="nextCommandHandler">The next command handler in the command execution chain.</param>
        /// <param name="commandHandler">A command handler to execute the command.</param>
        /// <param name="executionContext">Current command execution context.</param>
        public static void ExecuteCommand<T>(this ICommandHandler commandHandler, T args, Action nextCommandHandler, CommandExecutionContext executionContext) where T : CommandArgs
        {
            if (commandHandler == null)
            {
                throw new ArgumentNullException(nameof(commandHandler));
            }

            if (nextCommandHandler == null)
            {
                throw new ArgumentNullException(nameof(nextCommandHandler));
            }

            if (commandHandler is ICommandHandler<T> simpleCommandHandler)
            {
                if (simpleCommandHandler.ExecuteCommand(args, executionContext))
                {
                    return;
                }
                else
                {
                    nextCommandHandler();
                    return;
                }
            }

            if (commandHandler is IChainedCommandHandler<T> chainedCommandHandler)
            {
                chainedCommandHandler.ExecuteCommand(args, nextCommandHandler, executionContext);
                return;
            }

            throw new ArgumentException($"Unsupported CommandHandler type: {commandHandler.GetType()}");
        }
    }
}

