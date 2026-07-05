using System.Composition;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Handles Go To Matching Brace and its selection-extending variant over Roslyn's
/// <see cref="IBraceMatchingService"/>. In VS this command is handled by the shell's
/// text-view command filter (AbstractVsTextViewFilter), which was never open-sourced, so the
/// host provides it. Caret placement follows VS: from an open brace the caret jumps to after
/// the close brace, from a close brace to before the open brace, and the command does nothing
/// when the caret is not on a brace.
/// </summary>
[Export(typeof(ICommandHandler))]
[Shared]
[ContentType("Roslyn Languages")]
[Name("Morgania GoToMatchingBrace")]
internal sealed class GoToMatchingBraceCommandHandler :
    ICommandHandler<GotoBraceCommandArgs>,
    ICommandHandler<GotoBraceExtCommandArgs>
{
    private readonly IThreadingContext _threadingContext;
    private readonly IBraceMatchingService _braceMatchingService;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    public GoToMatchingBraceCommandHandler(
        IThreadingContext threadingContext,
        IBraceMatchingService braceMatchingService,
        IGlobalOptionService globalOptions)
    {
        _threadingContext = threadingContext;
        _braceMatchingService = braceMatchingService;
        _globalOptions = globalOptions;
    }

    public string DisplayName => "Go To Matching Brace";

    public CommandState GetCommandState(GotoBraceCommandArgs args) => CommandState.Available;

    public CommandState GetCommandState(GotoBraceExtCommandArgs args) => CommandState.Available;

    public bool ExecuteCommand(GotoBraceCommandArgs args, CommandExecutionContext executionContext) =>
        ExecuteCommand(args.TextView, args.SubjectBuffer, extendSelection: false, executionContext);

    public bool ExecuteCommand(GotoBraceExtCommandArgs args, CommandExecutionContext executionContext) =>
        ExecuteCommand(args.TextView, args.SubjectBuffer, extendSelection: true, executionContext);

    private bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, bool extendSelection, CommandExecutionContext executionContext)
    {
        if (textView.GetCaretPoint(subjectBuffer) is not { } caretPoint ||
            subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges() is not { } document)
        {
            return false;
        }

        var options = _globalOptions.GetBraceMatchingOptions(document.Project.Language);
        var cancellationToken = executionContext.OperationContext.UserCancellationToken;
        var matchingSpan = _threadingContext.JoinableTaskFactory.Run(
            () => _braceMatchingService.FindMatchingSpanAsync(document, caretPoint.Position, options, cancellationToken));

        // The caret is not on a brace: the command is still ours (our content type), there is
        // just nothing to do — same as VS.
        if (matchingSpan is not { } match)
        {
            return true;
        }

        var snapshot = subjectBuffer.CurrentSnapshot;
        int target;
        if (match.Start < caretPoint.Position)
        {
            // Caret at the close brace: land before the open brace.
            target = match.Start;
        }
        else if (match.End > caretPoint.Position)
        {
            // Caret at the open brace: land after the close brace.
            target = match.End;
        }
        else
        {
            return true;
        }

        var targetPoint = new SnapshotPoint(snapshot, target);
        if (extendSelection)
        {
            // Divergence from the VS shell's historical span tweaks: extend as a plain
            // selection from the current anchor to the jump target.
            var anchor = textView.Selection.IsEmpty
                ? new VirtualSnapshotPoint(caretPoint)
                : textView.Selection.AnchorPoint.TranslateTo(snapshot);
            textView.Selection.Select(anchor, new VirtualSnapshotPoint(targetPoint));
            textView.Caret.MoveTo(textView.Selection.ActivePoint);
        }
        else
        {
            textView.Selection.Clear();
            textView.Caret.MoveTo(targetPoint);
        }

        textView.Caret.EnsureVisible();
        return true;
    }
}
