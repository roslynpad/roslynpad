using System.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(DuplicateSelectionCommandHandler))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Shared]
    public sealed class DuplicateSelectionCommandHandler : ICommandHandler<DuplicateSelectionCommandArgs>
    {
        [Import]
        public IEditorOperationsFactoryService OperationsFactory { get; set; }

        public string DisplayName => Strings.DuplicateSelectionCommandHandlerName;

        public bool ExecuteCommand(DuplicateSelectionCommandArgs args, CommandExecutionContext context)
        {
            IEditorOperations3 ops = (IEditorOperations3)OperationsFactory.GetEditorOperations(args.TextView);
            return ops.DuplicateSelection();
        }

        public CommandState GetCommandState(DuplicateSelectionCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
