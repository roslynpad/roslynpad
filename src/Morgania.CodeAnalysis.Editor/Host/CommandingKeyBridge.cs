using System.Composition;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Translates keyboard input into the Modern Commanding chain so the command handlers exported
/// by Roslyn EditorFeatures and the editor core (completion, signature help, formatting,
/// brace completion, comment toggling, …) run. In Visual Studio the shell does this
/// key-binding translation; Morgania's view only has an interim keymap that goes straight to
/// IEditorOperations, bypassing commanding, so the host provides the bridge. Handlers are
/// added as tunneling handlers to run before the view's own key handling; anything the
/// command chain does not consume falls through to the view's keymap.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class CommandingKeyBridge : IWpfTextViewCreationListener
{
    private readonly IEditorCommandHandlerServiceFactory _commandServiceFactory;
    private readonly IEditorOperationsFactoryService _operationsFactory;
    private readonly ISignatureHelpBroker _signatureHelpBroker;
    private readonly SuggestedActionsControllerFactory _suggestedActionsFactory;

    [ImportingConstructor]
    public CommandingKeyBridge(
        IEditorCommandHandlerServiceFactory commandServiceFactory,
        IEditorOperationsFactoryService operationsFactory,
        ISignatureHelpBroker signatureHelpBroker,
        SuggestedActionsControllerFactory suggestedActionsFactory)
    {
        _commandServiceFactory = commandServiceFactory;
        _operationsFactory = operationsFactory;
        _signatureHelpBroker = signatureHelpBroker;
        _suggestedActionsFactory = suggestedActionsFactory;
    }

    public void TextViewCreated(IWpfTextView textView)
    {
        var commanding = _commandServiceFactory.GetService(textView);
        var operations = _operationsFactory.GetEditorOperations(textView);
        var suggestedActions = _suggestedActionsFactory.GetOrCreate(textView);

        textView.VisualElement.AddHandler(
            InputElement.KeyDownEvent,
            (_, e) => OnKeyDown(textView, commanding, operations, _signatureHelpBroker, suggestedActions, e),
            RoutingStrategies.Tunnel);
        textView.VisualElement.AddHandler(
            InputElement.TextInputEvent,
            (_, e) => OnTextInput(textView, commanding, operations, suggestedActions, e),
            RoutingStrategies.Tunnel);
    }

    private static void OnTextInput(IWpfTextView view, IEditorCommandHandlerService commanding, IEditorOperations operations, SuggestedActionsController suggestedActions, TextInputEventArgs e)
    {
        if (view.IsClosed || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        // Typing edits the code the actions were computed for; the VS light bulb dismisses too.
        if (suggestedActions.IsOpen)
        {
            suggestedActions.Dismiss();
        }

        foreach (var typedChar in e.Text)
        {
            if (char.IsControl(typedChar))
            {
                continue;
            }

            // The innermost "handler" performs the actual insertion, like the editor-operations
            // command target does in VS; Roslyn and editor handlers wrap around it.
            commanding.Execute(
                (v, b) => new TypeCharCommandArgs(v, b, typedChar),
                () => operations.InsertText(typedChar.ToString()));
        }

        e.Handled = true;
    }

    private static void OnKeyDown(IWpfTextView view, IEditorCommandHandlerService commanding, IEditorOperations operations, ISignatureHelpBroker signatureHelpBroker, SuggestedActionsController suggestedActions, KeyEventArgs e)
    {
        if (view.IsClosed)
        {
            return;
        }

        // The suggested-actions context menu takes focus while open and handles its own
        // navigation keys (arrows, Enter, Escape, submenu expansion) natively.

        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        bool plain = e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift;

        // Commands where the bridge supplies the default editing behavior as the innermost
        // handler: always consumed.
        bool? handled = (e.Key, command, shift) switch
        {
            (Key.Enter, false, false) => Execute(commanding, (v, b) => new ReturnKeyCommandArgs(v, b), () => operations.InsertNewLine()),
            (Key.Tab, false, false) => Execute(commanding, (v, b) => new TabKeyCommandArgs(v, b), () => operations.Indent()),
            (Key.Tab, false, true) => Execute(commanding, (v, b) => new BackTabKeyCommandArgs(v, b), () => operations.Unindent()),
            (Key.Back, false, _) => Execute(commanding, (v, b) => new BackspaceKeyCommandArgs(v, b), () => operations.Backspace()),
            (Key.Delete, false, false) => Execute(commanding, (v, b) => new DeleteKeyCommandArgs(v, b), () => operations.Delete()),
            (Key.V, true, false) => Execute(commanding, (v, b) => new PasteCommandArgs(v, b), () => operations.Paste()),
            (Key.X, true, false) => Execute(commanding, (v, b) => new CutCommandArgs(v, b), () => operations.CutSelection()),
            (Key.C, true, false) => Execute(commanding, (v, b) => new CopyCommandArgs(v, b), () => operations.CopySelection()),

            // Commands with no default behavior: consumed only if a handler took them,
            // otherwise they fall through to the view's keymap.
            (Key.Space, true, false) => TryExecute(commanding, (v, b) => new InvokeCompletionListCommandArgs(v, b)),
            (Key.Space, true, true) => TryExecute(commanding, (v, b) => new InvokeSignatureHelpCommandArgs(v, b)),
            (Key.Escape, false, false) => TryExecute(commanding, (v, b) => new EscapeKeyCommandArgs(v, b)),
            // Arrows go to the command chain first (completion claims them while its session is
            // open); an active signature help session gets them next, cycling overloads the way
            // the VS shell routes arrows to the intellisense session stack. Otherwise they fall
            // through to the keymap and move the caret.
            (Key.Up, _, _) when plain => TryExecute(commanding, (v, b) => new UpKeyCommandArgs(v, b))
                || (!shift && TryCycleSignatureHelp(signatureHelpBroker, view, previous: true)),
            (Key.Down, _, _) when plain => TryExecute(commanding, (v, b) => new DownKeyCommandArgs(v, b))
                || (!shift && TryCycleSignatureHelp(signatureHelpBroker, view, previous: false)),
            (Key.PageUp, _, _) when plain => TryExecute(commanding, (v, b) => new PageUpKeyCommandArgs(v, b)),
            (Key.PageDown, _, _) when plain => TryExecute(commanding, (v, b) => new PageDownKeyCommandArgs(v, b)),
            (Key.Z, true, false) => TryExecute(commanding, (v, b) => new UndoCommandArgs(v, b)),
            (Key.Z, true, true) or (Key.Y, true, false) => TryExecute(commanding, (v, b) => new RedoCommandArgs(v, b)),
            (Key.OemQuestion, true, false) or (Key.Oem2, true, false) => TryExecute(commanding, (v, b) => new ToggleLineCommentCommandArgs(v, b)),
            (Key.D, true, true) => TryExecute(commanding, (v, b) => new FormatDocumentCommandArgs(v, b)),
            (Key.OemCloseBrackets, true, false) => TryExecute(commanding, (v, b) => new GotoBraceCommandArgs(v, b)),
            (Key.OemCloseBrackets, true, true) => TryExecute(commanding, (v, b) => new GotoBraceExtCommandArgs(v, b)),
            (Key.OemPeriod, true, false) => suggestedActions.Show(),

            _ => null,
        };

        if (handled == true)
        {
            e.Handled = true;
        }
    }

    private static bool Execute<T>(IEditorCommandHandlerService commanding, Func<ITextView, Microsoft.VisualStudio.Text.ITextBuffer, T> argsFactory, Action defaultAction)
        where T : EditorCommandArgs
    {
        commanding.Execute(argsFactory, defaultAction);
        return true;
    }

    private static bool TryExecute<T>(IEditorCommandHandlerService commanding, Func<ITextView, Microsoft.VisualStudio.Text.ITextBuffer, T> argsFactory)
        where T : EditorCommandArgs
    {
        bool fellThrough = false;
        commanding.Execute(argsFactory, () => fellThrough = true);
        return !fellThrough;
    }

    private static bool TryCycleSignatureHelp(ISignatureHelpBroker broker, IWpfTextView view, bool previous)
    {
        if (!broker.IsSignatureHelpActive(view))
        {
            return false;
        }

        // With a single overload there is nothing to cycle; let the arrow move the caret
        // (Roslyn dismisses the session when the caret leaves the argument list).
        var session = broker.GetSessions(view)[0];
        var signatures = session.Signatures;
        if (signatures.Count < 2)
        {
            return false;
        }

        var index = signatures.IndexOf(session.SelectedSignature);
        var offset = previous ? signatures.Count - 1 : 1;
        session.SelectedSignature = signatures[(index + offset) % signatures.Count];
        return true;
    }
}
