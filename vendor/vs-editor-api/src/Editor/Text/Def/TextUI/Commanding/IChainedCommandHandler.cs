using System;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Commanding
{
    /// <summary>
    /// Represents a command handler that depends on behavior of following command handlers in the command execution chain
    /// formed from same strongly-typed <see cref="ICommandHandler"/>s ordered according to their [Order] attributes.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part and should be exported as the non-generic <see cref="ICommandHandler"/> with required
    /// [Name], [ContentType] attributes and optional [Order] and [TextViewRole] attributes.
    /// </remarks>
    /// <example>
    /// [Export(typeof(ICommandHandler))]
    /// [Name(nameof(MyCommandHandler))]
    /// [ContentType("text")]
    /// [Order(Before ="OtherCommandHandler")]   
    /// [TextViewRole(PredefinedTextViewRoles.Editable)]
    /// internal class MyCommandHandler : IChainedCommandHandler<MyCommandArgs>
    /// </example>
    public interface IChainedCommandHandler<T> : ICommandHandler, INamed where T : CommandArgs
    {
        /// <summary>
        /// Called to determine the state of the command.
        /// This method should never return <see cref="CommandState.Unspecified"/> as it would prevent calling following command handlers.
        /// <paramref name="nextCommandHandler"/> should be called instead. If a <see cref="IChainedCommandHandler{T}"/> handles a command
        /// it doesn't own, its <see cref="GetCommandState(T, Func{CommandState})"/> should always call <paramref name="nextCommandHandler"/>"
        /// to give a chance a <see cref="ICommandHandler"/> that owns the command to enable or disable it.
        /// </summary>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <param name="nextCommandHandler">The next command handler in the command execution chain.</param>
        /// <returns>A <see cref="CommandState"/> instance that contains information on the availability of the command.</returns>
        CommandState GetCommandState(T args, Func<CommandState> nextCommandHandler);

        /// <summary>
        /// Called to execute the command.
        /// If this implementation does not execute the command, <paramref name="nextCommandHandler"/> should be called
        /// so that other <see cref="ICommandHandler"/>s may act on this command.
        /// </summary>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <param name="nextCommandHandler">The next command handler in the command execution chain.</param>
        void ExecuteCommand(T args, Action nextCommandHandler, CommandExecutionContext executionContext);
    }
}
