// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Editor.Implementation
{
    using Microsoft.VisualStudio.Commanding;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Text.BraceCompletion;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Composition;

    /// <summary>
    /// Passes commands to the IBraceCompletionManager found in the
    /// property bag of the view.
    /// </summary>
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType("any")]
    [Name("BraceCompletionCommandHandler")]
    [Shared]
    public class BraceCompletionCommandHandler :
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<ReturnKeyCommandArgs>,
        IChainedCommandHandler<TabKeyCommandArgs>,
        IChainedCommandHandler<BackspaceKeyCommandArgs>,
        IChainedCommandHandler<DeleteKeyCommandArgs>
    {
        #region IChainedCommandHandler<TypeCharCommandArgs> Members

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            if (Enabled(args.TextView))
            {
                char typedChar = args.TypedChar;

                // handle closing braces if there is an active session
                if ((Manager(args.TextView).HasActiveSessions && Manager(args.TextView).ClosingBraces.IndexOf(typedChar) > -1)
                            || Manager(args.TextView).OpeningBraces.IndexOf(typedChar) > -1)
                {
                    bool handledCommand = false;
                    Manager(args.TextView).PreTypeChar(typedChar, out handledCommand);

                    if (handledCommand)
                    {
                        return;
                    }

                    nextHandler();

                    Manager(args.TextView).PostTypeChar(typedChar);

                    return;
                }
            }
            nextHandler();
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (Enabled(args.TextView) && Manager(args.TextView).HasActiveSessions)
            {
                bool handledCommand = false;

                Manager(args.TextView).PreReturn(out handledCommand);

                if (handledCommand)
                {
                    return;
                }

                nextCommandHandler();

                Manager(args.TextView).PostReturn();

                return;
            }
            nextCommandHandler();
        }

        public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(TabKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (Enabled(args.TextView) && Manager(args.TextView).HasActiveSessions)
            {
                bool handledCommand = false;

                Manager(args.TextView).PreTab(out handledCommand);

                if (handledCommand)
                {
                    return;
                }

                nextCommandHandler();

                Manager(args.TextView).PostTab();

                return;
            }
            nextCommandHandler();
        }

        public CommandState GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(BackspaceKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (Enabled(args.TextView) && Manager(args.TextView).HasActiveSessions)
            {
                bool handledCommand = false;

                Manager(args.TextView).PreBackspace(out handledCommand);

                if (handledCommand)
                {
                    return;
                }

                nextCommandHandler();

                Manager(args.TextView).PostBackspace();

                return;
            }
            nextCommandHandler();
        }

        public CommandState GetCommandState(DeleteKeyCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(DeleteKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            if (Enabled(args.TextView) && Manager(args.TextView).HasActiveSessions)
            {
                bool handledCommand = false;

                Manager(args.TextView).PreDelete(out handledCommand);

                if (handledCommand)
                {
                    return;
                }

                nextCommandHandler();

                Manager(args.TextView).PostDelete();

                return;
            }
            nextCommandHandler();
        }

        public string DisplayName => "BraceCompletionCommandHandler";

        #endregion

        #region Private Helpers

        private bool Enabled(ITextView view)
        {
            return Manager(view)?.Enabled ?? false;
        }

        private IBraceCompletionManager Manager(ITextView view)
        {
            if (view.Properties.TryGetProperty("BraceCompletionManager", out IBraceCompletionManager _manager))
                return _manager;

            return null;
        }

        #endregion
    }
}
