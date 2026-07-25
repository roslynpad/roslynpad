#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

using System.Composition;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

/// <summary>Opens the find/replace panel in find mode (Edit.Find).</summary>
public sealed class ShowFindCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
    : EditorCommandArgs(textView, subjectBuffer);

/// <summary>
/// Opens the find/replace panel with the replace row (Edit.Replace); on a read-only view
/// the panel degrades to find-only.
/// </summary>
public sealed class ShowReplaceCommandArgs(ITextView textView, ITextBuffer subjectBuffer)
    : EditorCommandArgs(textView, subjectBuffer);

/// <summary>
/// Routes the find/replace commands to the view's <see cref="FindReplacePanel"/>, so hosts
/// invoke find like any other editor command instead of addressing the panel directly.
/// </summary>
[Name(nameof(FindReplaceCommandHandler))]
[ContentType("text")]
[Export(typeof(ICommandHandler))]
[Shared]
public sealed class FindReplaceCommandHandler :
    ICommandHandler<ShowFindCommandArgs>,
    ICommandHandler<ShowReplaceCommandArgs>
{
    string INamed.DisplayName => nameof(FindReplaceCommandHandler);

    public CommandState GetCommandState(ShowFindCommandArgs args) => CommandState.Available;

    public bool ExecuteCommand(ShowFindCommandArgs args, CommandExecutionContext executionContext) =>
        Show(args.TextView, showReplace: false);

    public CommandState GetCommandState(ShowReplaceCommandArgs args) => CommandState.Available;

    public bool ExecuteCommand(ShowReplaceCommandArgs args, CommandExecutionContext executionContext) =>
        Show(args.TextView, showReplace: true);

    private static bool Show(ITextView textView, bool showReplace)
    {
        if (FindReplacePanel.Get(textView) is not { } panel)
        {
            return false;
        }

        panel.Show(showReplace);
        return true;
    }
}
