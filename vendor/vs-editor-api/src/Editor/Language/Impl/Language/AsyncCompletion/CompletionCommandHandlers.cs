using System;
using System.Composition;
using System.Globalization;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.UI.Utilities;
using Microsoft.VisualStudio.Utilities;
using CommonImplementation = Microsoft.VisualStudio.Language.Intellisense.Implementation;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Reacts to the down arrow command and attempts to scroll the completion list.
    /// </summary>
    [Name(PredefinedCompletionNames.CompletionCommandHandler)]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [Export(typeof(ICommandHandler))]
    [Shared]
    public sealed class CompletionCommandHandler :
        ICommandHandler<AutomaticLineEnderCommandArgs>,
        IChainedCommandHandler<BackspaceKeyCommandArgs>,
        IDynamicCommandHandler<BackspaceKeyCommandArgs>,
        ICommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        ICommandHandler<CutCommandArgs>,
        IChainedCommandHandler<DeleteKeyCommandArgs>,
        IDynamicCommandHandler<DeleteKeyCommandArgs>,
        ICommandHandler<DownKeyCommandArgs>,
        ICommandHandler<EscapeKeyCommandArgs>,
        IDynamicCommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>,
        ICommandHandler<InvokeCompletionListCommandArgs>,
        IDynamicCommandHandler<InvokeCompletionListCommandArgs>,
        ICommandHandler<PageDownKeyCommandArgs>,
        ICommandHandler<PageUpKeyCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<RedoCommandArgs>,
        ICommandHandler<RenameCommandArgs>,
        IChainedCommandHandler<ReturnKeyCommandArgs>,
        IDynamicCommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<SaveCommandArgs>,
        ICommandHandler<SelectAllCommandArgs>,
        ICommandHandler<SurroundWithCommandArgs>,
        IChainedCommandHandler<TabKeyCommandArgs>,
        IDynamicCommandHandler<TabKeyCommandArgs>,
        ICommandHandler<ToggleCompletionModeCommandArgs>,
        ICommandHandler<ToggleCompletionListFilterCommandArgs>,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IDynamicCommandHandler<TypeCharCommandArgs>,
        ICommandHandler<UndoCommandArgs>,
        ICommandHandler<UpKeyCommandArgs>,
        ICommandHandler<WordDeleteToEndCommandArgs>,
        ICommandHandler<WordDeleteToStartCommandArgs>
    {
        [Import]
        public IAsyncCompletionBroker Broker { get; set; }

        [Import]
        public ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        [Import]
        public IEditorOperationsFactoryService EditorOperationsFactoryService { get; set; }

        [Import]
        public CompletionAvailabilityUtility CompletionAvailability { get; set; }

        string INamed.DisplayName => CommonImplementation.Strings.CompletionCommandHandlerName;

        private string chainedCommandIsBeingHandled = nameof(chainedCommandIsBeingHandled);

        /// <summary>
        /// Helper method that returns command state for commands
        /// which are available as long as the completion feature is available.
        /// </summary>
        private CommandState GetCommandStateIfCompletionIsAvailable(IContentType contentType, ITextView textView)
        {
            return CompletionAvailability.IsAvailable(contentType, textView.Roles)
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        /// <summary>
        /// Helper method that returns command state for commands
        /// which are available IF AND ONLY IF completion is active,
        /// even if the commands would be otherwise unavailable.
        /// </summary>
        private CommandState GetCommandStateIfCompletionIsActive(ITextView textView)
        {
            return Broker.IsCompletionActive(textView)
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        /// <summary>
        /// Helper method that returns command state for commands
        /// which are available when completion is either currently active, or available.
        /// This is used by commands that may trigger completion session on a specified buffer, or interact with an active completion session on another buffer
        /// </summary>
        private CommandState GetCommandStateIfCompletionIsActiveOrAvailable(IContentType contentType, ITextView textView)
        {
            return Broker.IsCompletionActive(textView) || CompletionAvailability.IsAvailable(contentType, textView.Roles)
                ? CommandState.Available
                : CommandState.Unspecified;
        }

        /// <summary>
        /// Helper method that returns command state for the suggestion mode toggle button.
        /// This command state controls not only whether the toggle button is enabled, but also if it's toggled.
        /// </summary>
        private CommandState GetCommandStateForSuggestionModeToggle(IContentType contentType, ITextView textView)
        {
            var isAvailable = CompletionAvailability.IsAvailable(contentType, textView.Roles);
            var isChecked = CompletionUtilities.GetSuggestionModeOption(textView);
            return new CommandState(isAvailable, isChecked);
        }

        /// <summary>
        /// This helper method encapsulates a pattern we use within <see cref="IChainedCommandHandler{T}"/>
        /// for executing completion logic in <paramref name="commandHandler"/>.
        ///
        /// The pattern accomplishes two objectives:
        /// 1. Don't run completion logic if completion is not available for given <paramref name="args"/>.
        /// 2. Run completion logic only once. The commanding system chains the handlers for all applicable buffers, but we are acting only once.
        /// It is ok to run completion logic only once, because it works on any buffer and performs its own mapping to available subject buffers.
        /// </summary>
        private void RunOnceIfAvailable<T>(T args, Action nextCommandHandler, Action commandHandler) where T : EditorCommandArgs
        {
            if (args.TextView.Properties.ContainsProperty(chainedCommandIsBeingHandled)
                || !GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView).IsAvailable)
            {
                nextCommandHandler();
                return;
            }

            try
            {
                args.TextView.Properties.AddProperty(chainedCommandIsBeingHandled, true);
                commandHandler();
            }
            finally
            {
                args.TextView.Properties.RemoveProperty(chainedCommandIsBeingHandled);
            }
        }

        /// <summary>
        /// Realizes the virtual space and updates session's applicable to span.
        /// We invoke this method after the session has triggered, because we don't want to act if there would be no completion.
        /// </summary>
        private void RealizeVirtualSpaceUpdateApplicableToSpan(IAsyncCompletionSessionOperations session, ITextView textView)
        {
            if (session == null // We may only act if we have internal reference to the session
                || !textView.Caret.InVirtualSpace // We only act if caret is in virtual space
                || !session.ApplicableToSpan.GetSpan(textView.TextSnapshot).IsEmpty) // We only act if the applicable to span is of zero length (at the beginning of the line)
            {
                return;
            }

            // Realize the virtual space before triggering the session by inserting nothing through the editor opertaions.
            IEditorOperations editorOperations = EditorOperationsFactoryService.GetEditorOperations(textView);
            editorOperations?.InsertText("");

            // ApplicableToSpan just grew to include the realized white space.
            // We know that ApplicableToSpan was zero length, so let's recreate a zero length span at the caret location.
            // This method executed synchronously, and therefore we know that it is safe to modify the applicable to span.
            session.ApplicableToSpan = textView.TextSnapshot.CreateTrackingSpan(
                start: textView.Caret.Position.BufferPosition.Position,
                length: 0,
                trackingMode: SpanTrackingMode.EdgeInclusive);
        }

        // ----- Command handlers:

        CommandState ICommandHandler<AutomaticLineEnderCommandArgs>.GetCommandState(AutomaticLineEnderCommandArgs args)
            => GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView);

        bool ICommandHandler<AutomaticLineEnderCommandArgs>.ExecuteCommand(AutomaticLineEnderCommandArgs args, CommandExecutionContext executionContext)
        {
            if (!GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView).IsAvailable)
                return false;

            var session = Broker.GetSession(args.TextView);
            if (session != null)
            {
                session.Commit(default, executionContext.OperationContext.UserCancellationToken);
                session.Dismiss();
                // Don't mark this command as handled, so that we can automatically end the line
            }
            return false;
        }

        CommandState IChainedCommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextCommandHandler)
           => nextCommandHandler();

        bool IDynamicCommandHandler<BackspaceKeyCommandArgs>.CanExecuteCommand(BackspaceKeyCommandArgs args)
            => Broker.IsCompletionActive(args.TextView) || Broker.IsCompletionSupported(args.SubjectBuffer.ContentType, args.TextView.Roles);

        void IChainedCommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            RunOnceIfAvailable(args, nextCommandHandler, () =>
            {
                var snapshotBeforeEdit = args.TextView.TextSnapshot;
                // Execute other commands in the chain to see the change in the buffer.
                nextCommandHandler();

                if (args.TextView.TextSnapshot == snapshotBeforeEdit)
                {
                    // Buffer has not changed. Don't invoke completion.
                    return;
                }

                var session = Broker.GetSession(args.TextView);
                var location = args.TextView.Caret.Position.BufferPosition;
                var trigger = new CompletionTrigger(CompletionTriggerReason.Backspace, snapshotBeforeEdit);

                if (session != null)
                {
                    session.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
                else
                {
                    var newSession = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);
                    newSession?.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
            });
        }

        CommandState ICommandHandler<EscapeKeyCommandArgs>.GetCommandState(EscapeKeyCommandArgs args)
            => GetCommandStateIfCompletionIsActive(args.TextView);

        bool IDynamicCommandHandler<EscapeKeyCommandArgs>.CanExecuteCommand(EscapeKeyCommandArgs args)
            => Broker.IsCompletionActive(args.TextView);

        bool ICommandHandler<EscapeKeyCommandArgs>.ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            var session = Broker.GetSession(args.TextView);
            if (session != null)
            {
                session.Dismiss();
                return true;
            }
            return false;
        }

        CommandState ICommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args)
            => GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView);

        bool IDynamicCommandHandler<InvokeCompletionListCommandArgs>.CanExecuteCommand(InvokeCompletionListCommandArgs args)
            => CompletionAvailability.IsAvailable(args.SubjectBuffer.ContentType, args.TextView.Roles);

        bool ICommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, CommandExecutionContext executionContext)
        {
            if (!GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView).IsAvailable)
                return false;

            var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, args.TextView.TextSnapshot);
            var location = args.TextView.Caret.Position.BufferPosition;
            var session = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);

            if (session is IAsyncCompletionSessionOperations sessionInternal)
            {
                RealizeVirtualSpaceUpdateApplicableToSpan(sessionInternal, args.TextView);
                location = args.TextView.Caret.Position.BufferPosition; // Buffer may have changed. Update the location.
                session.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                return true;
            }
            return false;
        }

        CommandState ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args)
            => GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView);

        bool ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, CommandExecutionContext executionContext)
        {
            if (!GetCommandStateIfCompletionIsAvailable(args.SubjectBuffer.ContentType, args.TextView).IsAvailable)
                return false;

            var snapshotBeforeEdit = args.TextView.TextSnapshot;
            var trigger = new CompletionTrigger(CompletionTriggerReason.InvokeAndCommitIfUnique, args.TextView.TextSnapshot);
            var location = args.TextView.Caret.Position.BufferPosition;
            var session = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);

            if (session is IAsyncCompletionSessionOperations sessionInternal)
            {
                RealizeVirtualSpaceUpdateApplicableToSpan(sessionInternal, args.TextView);
                location = args.TextView.Caret.Position.BufferPosition; // Buffer may have changed. Update the location.
                sessionInternal.InvokeAndCommitIfUnique(trigger, location, executionContext.OperationContext.UserCancellationToken);
                return true;
            }
            return false;
        }

        CommandState ICommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args)
            => GetCommandStateForSuggestionModeToggle(args.SubjectBuffer.ContentType, args.TextView);

        bool ICommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, CommandExecutionContext executionContext)
        {
            var toggledValue = !CompletionUtilities.GetSuggestionModeOption(args.TextView);
            CompletionUtilities.SetSuggestionModeOption(args.TextView, toggledValue);

            if (Broker.GetSession(args.TextView) is IAsyncCompletionSessionOperations sessionInternal) // we are accessing an internal method
            {
                sessionInternal.SetSuggestionMode(toggledValue);
                return true;
            }
            return false;
        }

        CommandState ICommandHandler<ToggleCompletionListFilterCommandArgs>.GetCommandState(
            ToggleCompletionListFilterCommandArgs args)
        {
            if (!Broker.IsCompletionActive(args.TextView))
                return CommandState.Unspecified;

            if (Broker.GetSession(args.TextView) is IAsyncCompletionSessionOperations2 sessionInternal &&
                sessionInternal.CanToggleFilter(args.AccessKey))
                return CommandState.Available;

            return CommandState.Unavailable;
        }

        bool ICommandHandler<ToggleCompletionListFilterCommandArgs>.ExecuteCommand(
            ToggleCompletionListFilterCommandArgs args,
            CommandExecutionContext executionContext)
        {
            if (Broker.GetSession(args.TextView) is IAsyncCompletionSessionOperations2 sessionInternal)
            {
                sessionInternal.ToggleFilter(args.AccessKey);
                return true;
            }

            return false;
        }

        CommandState IChainedCommandHandler<DeleteKeyCommandArgs>.GetCommandState(DeleteKeyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        bool IDynamicCommandHandler<DeleteKeyCommandArgs>.CanExecuteCommand(DeleteKeyCommandArgs args)
            => Broker.IsCompletionActive(args.TextView) || Broker.IsCompletionSupported(args.SubjectBuffer.ContentType, args.TextView.Roles);

        void IChainedCommandHandler<DeleteKeyCommandArgs>.ExecuteCommand(DeleteKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            RunOnceIfAvailable(args, nextCommandHandler, () =>
            {
                var snapshotBeforeEdit = args.TextView.TextSnapshot;
                // Execute other commands in the chain to see the change in the buffer.
                nextCommandHandler();

                if (args.TextView.TextSnapshot == snapshotBeforeEdit)
                {
                    // Buffer has not changed. Don't invoke completion.
                    return;
                }

                var session = Broker.GetSession(args.TextView);
                var location = args.TextView.Caret.Position.BufferPosition;
                var trigger = new CompletionTrigger(CompletionTriggerReason.Deletion, snapshotBeforeEdit);

                if (session != null)
                {
                    session.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
                else
                {
                    var newSession = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);
                    newSession?.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
            });
        }

        CommandState ICommandHandler<WordDeleteToEndCommandArgs>.GetCommandState(WordDeleteToEndCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<WordDeleteToEndCommandArgs>.ExecuteCommand(WordDeleteToEndCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<WordDeleteToStartCommandArgs>.GetCommandState(WordDeleteToStartCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<WordDeleteToStartCommandArgs>.ExecuteCommand(WordDeleteToStartCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<SaveCommandArgs>.GetCommandState(SaveCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<SelectAllCommandArgs>.GetCommandState(SelectAllCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<SelectAllCommandArgs>.ExecuteCommand(SelectAllCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<RenameCommandArgs>.GetCommandState(RenameCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<RenameCommandArgs>.ExecuteCommand(RenameCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<UndoCommandArgs>.GetCommandState(UndoCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<UndoCommandArgs>.ExecuteCommand(UndoCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<RedoCommandArgs>.GetCommandState(RedoCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<RedoCommandArgs>.ExecuteCommand(RedoCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args)
            => CommandState.Unspecified;

        bool ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, CommandExecutionContext executionContext)
        {
            Broker.GetSession(args.TextView)?.Dismiss();
            return false;
        }

        CommandState IChainedCommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();


        bool IDynamicCommandHandler<ReturnKeyCommandArgs>.CanExecuteCommand(ReturnKeyCommandArgs args)
            => Broker.IsCompletionActive(args.TextView) || Broker.IsCompletionSupported(args.SubjectBuffer.ContentType, args.TextView.Roles);

        void IChainedCommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            RunOnceIfAvailable(args, nextCommandHandler, () =>
            {
                char typedChar = '\n';

                var session = Broker.GetSession(args.TextView);
                if (session != null)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("Return: begin commit");
                    var commitBehavior = session.Commit(typedChar, executionContext.OperationContext.UserCancellationToken);
                    session.Dismiss();

                    // Mark this command as handled (don't call command handlers further down the chain)
                    // in debugger text views
                    // when RaiseFurtherReturnKeyAndTabKeyCommandHandlers is unset
                    if ((commitBehavior & CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers) == 0
                        || CompletionUtilities.IsDebuggerTextView(args.TextView)
                        || CompletionUtilities.IsImmediateTextView(args.TextView))
                    {
                        if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                            DiagnosticLogger.Add("Return: do nothing after commit", commitBehavior);
                        return;
                    }
                }

                var snapshotBeforeEdit = args.TextView.TextSnapshot;
                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Return: next handler");
                nextCommandHandler();

                if (args.TextView.TextSnapshot == snapshotBeforeEdit)
                {
                    // Buffer has not changed. Don't invoke completion.
                    return;
                }

                // Buffer has changed. Update it for when we try to trigger new session.
                var location = args.TextView.Caret.Position.BufferPosition;

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Return: try make new session");
                var trigger = new CompletionTrigger(CompletionTriggerReason.Insertion, snapshotBeforeEdit, typedChar);
                var newSession = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);
                if (newSession is IAsyncCompletionSessionOperations sessionInternal)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("Return: created new session");
                    RealizeVirtualSpaceUpdateApplicableToSpan(sessionInternal, args.TextView);
                }
                location = args.TextView.Caret.Position.BufferPosition; // Buffer may have changed. Update the location.
                newSession?.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Return: finish");
            });
        }

        CommandState IChainedCommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        bool IDynamicCommandHandler<TabKeyCommandArgs>.CanExecuteCommand(TabKeyCommandArgs args)
            => Broker.IsCompletionActive(args.TextView) || Broker.IsCompletionSupported(args.SubjectBuffer.ContentType, args.TextView.Roles);

        void IChainedCommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            RunOnceIfAvailable(args, nextCommandHandler, () =>
            {
                char typedChar = '\t';

                var session = Broker.GetSession(args.TextView);
                if (session != null)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("Tab: begin commit");

                    var commitBehavior = session.Commit(typedChar, executionContext.OperationContext.UserCancellationToken);
                    session.Dismiss();

                    // Mark this command as handled (don't call command handlers further down the chain)
                    // in debugger text views
                    // when RaiseFurtherReturnKeyAndTabKeyCommandHandlers is unset
                    if ((commitBehavior & CommitBehavior.RaiseFurtherReturnKeyAndTabKeyCommandHandlers) == 0
                        || CompletionUtilities.IsDebuggerTextView(args.TextView)
                        || CompletionUtilities.IsImmediateTextView(args.TextView))
                    {
                        if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                            DiagnosticLogger.Add("Tab: do nothing after commit", commitBehavior);
                        return;
                    }
                }
                var snapshotBeforeEdit = args.TextView.TextSnapshot;
                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Tab: next handler");
                nextCommandHandler();

                if (args.TextView.TextSnapshot == snapshotBeforeEdit)
                {
                    // Buffer has not changed. Don't invoke completion.
                    return;
                }

                // Buffer has changed. Update it for when we try to trigger new session.
                var location = args.TextView.Caret.Position.BufferPosition;

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Tab: try make new session");
                var trigger = new CompletionTrigger(CompletionTriggerReason.Insertion, snapshotBeforeEdit, typedChar);
                var newSession = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);
                if (newSession is IAsyncCompletionSessionOperations sessionInternal)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("Tab: created new session");
                }
                newSession?.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("Commit with tab: finish");
            });
        }

        CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        bool IDynamicCommandHandler<TypeCharCommandArgs>.CanExecuteCommand(TypeCharCommandArgs args)
            => CompletionAvailability.IsAvailable(args.SubjectBuffer.ContentType, args.TextView.Roles);

        void IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            RunOnceIfAvailable(args, nextCommandHandler, () =>
            {
                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("TypeChar: ", args.TypedChar);

                var view = args.TextView;
                var location = view.Caret.Position.BufferPosition;
                var initialTextSnapshot = args.SubjectBuffer.CurrentSnapshot;

                // Note regarding undo: When completion and brace completion happen together, completion should be first on the undo stack.
                // Effectively, we want to first undo the completion, leaving brace completion intact. Second undo should undo brace completion.
                // To achieve this, we create a transaction in which we commit and reapply brace completion (via nextCommandHandler).
                // Please read "Note regarding undo" comments in this method that explain the implementation choices.
                // Hopefully an upcoming upgrade of the undo mechanism will allow us to undo out of order and vastly simplify this method.

                // Note regarding undo: In a corner case of typing closing brace over existing closing brace,
                // Roslyn brace completion does not perform an edit. It moves the caret outside of session's applicable span,
                // which dismisses the session. Put the session in a state where it will not dismiss when caret leaves the applicable span.
                var sessionToCommit = Broker.GetSession(args.TextView);
                if (sessionToCommit != null)
                {
                    ((AsyncCompletionSession)sessionToCommit).IgnoreCaretMovement(ignore: true);
                }

                // BraceCompletionManager is accessible through well known property name.
                IBraceCompletionManager braceCompletionManager;
                args.TextView.Properties.TryGetProperty("BraceCompletionManager", out braceCompletionManager);
                var braceCompletionSessionsBeforeEdit = braceCompletionManager?.ActiveSessionCount;
                var snapshotBeforeEdit = args.TextView.TextSnapshot;

                // Execute other commands in the chain to see the change in the buffer. This includes brace completion.

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("TypeChar invokes nextCommandHandler...");

                nextCommandHandler();

                if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                    DiagnosticLogger.Add("...TypeChar invoked nextCommandHandler");

                var braceCompletionSessionAfterEdit = braceCompletionManager?.ActiveSessionCount;

                if (args.TextView.TextSnapshot == snapshotBeforeEdit
                    && braceCompletionSessionAfterEdit == braceCompletionSessionsBeforeEdit)
                {
                    // Buffer has not changed, and neither did state of brace completion.
                    // Don't invoke completion.
                    return;
                }

                // If brace completion just closed, we will not undo the last type char
                var dontUndoBraceCompletion = braceCompletionSessionAfterEdit < braceCompletionSessionsBeforeEdit;

                // Pass location from before calling nextCommandHandler
                // so that extenders get the same view of the buffer in both ShouldCommit and Commit
                if (sessionToCommit?.ShouldCommit(args.TypedChar, location, executionContext.OperationContext.UserCancellationToken) == true)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("TypeChar: ShouldCommit");

                    // Buffer has changed, update the snapshot
                    location = view.Caret.Position.BufferPosition;

                    // Note regarding undo: this transaction will be 1st in the undo stack
                    using (var undoTransaction = new CaretPreservingEditTransaction("Completion", view, UndoHistoryRegistry, EditorOperationsFactoryService))
                    {
                        // Undo the typechar, because that's what language service expects
                        // Note that Roslyn expects brace to be there, because it can't handle undoing brace completion
                        if (!dontUndoBraceCompletion)
                        {
                            if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                                DiagnosticLogger.Add("TypeChar: commit. roll back");
                            UndoUtilities.RollbackToBeforeTypeChar(initialTextSnapshot, args.SubjectBuffer);
                        }
                        // Now the buffer doesn't have the commit character, but may have a matching brace

                        var commitBehavior = sessionToCommit.Commit(args.TypedChar, executionContext.OperationContext.UserCancellationToken);
                        if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                            DiagnosticLogger.Add("TypeChar: commit. behavior: ", commitBehavior);

                        if (!dontUndoBraceCompletion && (commitBehavior & CommitBehavior.SuppressFurtherTypeCharCommandHandlers) == 0)
                        {
                            if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                                DiagnosticLogger.Add("TypeChar: commit. nextCommandHandler");
                            nextCommandHandler(); // Replay the key, so that we get brace completion.
                        }

                        // Complete the transaction before stopping it.
                        undoTransaction.Complete();
                    }
                }

                // Restore the default state where session dismisses when caret is outside of the applicable span.
                if (sessionToCommit != null)
                {
                    ((AsyncCompletionSession)sessionToCommit).IgnoreCaretMovement(ignore: false);
                }

                // Buffer might have changed. Update it for when we try to trigger new session.
                location = view.Caret.Position.BufferPosition;

                var trigger = new CompletionTrigger(CompletionTriggerReason.Insertion, snapshotBeforeEdit, args.TypedChar);
                var session = Broker.GetSession(args.TextView);
                if (session != null)
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("TypeChar: Update session");

                    session.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
                else
                {
                    if (DiagnosticLogger.IsLoggingEnabled(args.TextView))
                        DiagnosticLogger.Add("TypeChar: Create new session");

                    var newSession = Broker.TriggerCompletion(args.TextView, trigger, location, executionContext.OperationContext.UserCancellationToken);
                    newSession?.OpenOrUpdate(trigger, location, executionContext.OperationContext.UserCancellationToken);
                }
            });
        }

        CommandState ICommandHandler<DownKeyCommandArgs>.GetCommandState(DownKeyCommandArgs args)
            => GetCommandStateIfCompletionIsActive(args.TextView);

        bool ICommandHandler<DownKeyCommandArgs>.ExecuteCommand(DownKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            if (Broker.GetSession(args.TextView) is AsyncCompletionSession session) // we are accessing an internal method
            {
                session.SelectDown();

                // Command is handled if completion still exists.
                // Up\Down\PgUp\PgDown dismiss completion If it hasn't computed items, in which case we allow Editor to handle the command.
                return !session.IsDismissed;
            }
            return false;
        }

        CommandState ICommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args)
            => GetCommandStateIfCompletionIsActive(args.TextView);

        bool ICommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            if (Broker.GetSession(args.TextView) is AsyncCompletionSession session) // we are accessing an internal method
            {
                session.SelectPageDown();

                // Command is handled if completion still exists.
                // Up\Down\PgUp\PgDown dismiss completion If it hasn't computed items, in which case we allow Editor to handle the command.
                return !session.IsDismissed;
            }
            return false;
        }

        CommandState ICommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args)
            => GetCommandStateIfCompletionIsActive(args.TextView);

        bool ICommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            if (Broker.GetSession(args.TextView) is AsyncCompletionSession session) // we are accessing an internal method
            {
                session.SelectPageUp();

                // Command is handled if completion still exists.
                // Up\Down\PgUp\PgDown dismiss completion If it hasn't computed items, in which case we allow Editor to handle the command.
                return !session.IsDismissed;
            }
            return false;
        }

        CommandState ICommandHandler<UpKeyCommandArgs>.GetCommandState(UpKeyCommandArgs args)
            => GetCommandStateIfCompletionIsActive(args.TextView);

        bool ICommandHandler<UpKeyCommandArgs>.ExecuteCommand(UpKeyCommandArgs args, CommandExecutionContext executionContext)
        {
            if (Broker.GetSession(args.TextView) is AsyncCompletionSession session) // we are accessing an internal method
            {
                session.SelectUp();

                // Command is handled if completion still exists.
                // Up\Down\PgUp\PgDown dismiss completion If it hasn't computed items, in which case we allow Editor to handle the command.
                return !session.IsDismissed;
            }
            return false;
        }
    }
}
