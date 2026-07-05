using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Commanding
{
    /// <summary>
    /// This interface marks a class that implements at least one strongly-typed
    /// <see cref="ICommandHandler{T}"/> or <see cref="IChainedCommandHandler{T}"/>.
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
    /// internal class MyCommandHandler : ICommandHandler<MyCommandArgs>
    /// </example>
    public interface ICommandHandler
    {
    }

    /// <summary>
    /// Represents a handler for a command associated with specific <see cref="CommandArgs"/>.
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
    /// internal class MyCommandHandler : ICommandHandler<MyCommandArgs>
    /// </example>
    public interface ICommandHandler<T> : ICommandHandler, INamed where T : CommandArgs
    {
        /// <summary>
        /// Called to determine the state of the command.
        /// </summary>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <returns>A <see cref="CommandState"/> instance that contains information on the availability of the command.</returns>
        CommandState GetCommandState(T args);

        /// <summary>
        /// Called to execute the command.
        /// </summary>
        /// <param name="args">The <see cref="CommandArgs"/> arguments for the command.</param>
        /// <returns>Returns <c>true</c> if the command was handled, <c>false</c> otherwise.</returns>
        bool ExecuteCommand(T args, CommandExecutionContext executionContext);
    }

    /// <summary>
    /// A command handler that can opt out of <see cref="ICommandHandler{T}.ExecuteCommand(T, CommandExecutionContext)"/>.
    /// </summary>
    internal interface IDynamicCommandHandler<T> where T : CommandArgs
    {
        /// <summary>
        /// Determines whether <see cref="ICommandHandler{T}.ExecuteCommand(T, CommandExecutionContext)"/> should be called.
        /// </summary>
        bool CanExecuteCommand(T args);
    }
}
