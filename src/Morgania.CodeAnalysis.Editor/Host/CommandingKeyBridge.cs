using System.Composition;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
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
///
/// Chords are platform-idiomatic: <see cref="PlatformHotkeyConfiguration"/> supplies the
/// OS command modifier (Cmd on macOS, Ctrl elsewhere) and the word-action modifier (Option
/// on macOS, Ctrl elsewhere), so Ctrl and Cmd are distinct keys rather than synonyms. The
/// headless platform reports Ctrl on every OS, which keeps scripted runs portable.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("Roslyn Languages")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[method: ImportingConstructor]
internal sealed class CommandingKeyBridge(
    IEditorCommandHandlerServiceFactory commandServiceFactory,
    IEditorOperationsFactoryService operationsFactory,
    ISignatureHelpBroker signatureHelpBroker,
    SuggestedActionsControllerFactory suggestedActionsFactory) : IWpfTextViewCreationListener
{
    public void TextViewCreated(IWpfTextView textView)
    {
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration
            ?? throw new InvalidOperationException("The Avalonia platform is not initialized.");

        var bindings = new ViewBindings(
            textView,
            commandServiceFactory.GetService(textView),
            operationsFactory.GetEditorOperations(textView),
            signatureHelpBroker,
            suggestedActionsFactory.GetOrCreate(textView),
            hotkeys);

        textView.VisualElement.AddHandler(
            InputElement.KeyDownEvent,
            (_, e) => bindings.OnKeyDown(e),
            RoutingStrategies.Tunnel);
        textView.VisualElement.AddHandler(
            InputElement.TextInputEvent,
            (_, e) => bindings.OnTextInput(e),
            RoutingStrategies.Tunnel);
    }

    /// <summary>The key chords for one text view, closed over its commanding chain.</summary>
    private sealed class ViewBindings(
        IWpfTextView view,
        IEditorCommandHandlerService commanding,
        IEditorOperations operations,
        ISignatureHelpBroker signatureHelpBroker,
        SuggestedActionsController suggestedActions,
        PlatformHotkeyConfiguration hotkeys)
    {
        private readonly KeyModifiers _commandModifiers = hotkeys.CommandModifiers;
        private readonly KeyModifiers _wordModifiers = hotkeys.WholeWordTextActionModifiers;

        public void OnKeyDown(KeyEventArgs e)
        {
            if (view.IsClosed)
            {
                return;
            }

            // The suggested-actions context menu takes focus while open and handles its own
            // navigation keys (arrows, Enter, Escape, submenu expansion) natively.

            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            var chord = e.KeyModifiers & ~KeyModifiers.Shift;

            // In a read-only view editing chords are suppressed here and blocked again by
            // the keymap; navigation, copy, and go-to commands stay live.
            bool readOnly = view.Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId);

            // Completion is Ctrl+Space on every platform: on macOS the command modifier
            // belongs to Spotlight, and Mac editors use Ctrl+Space as well.
            if ((e.Key, chord, shift) is (Key.Space, KeyModifiers.Control, false))
            {
                e.Handled = !readOnly && TryRun(static (v, b) => new InvokeCompletionListCommandArgs(v, b));
                return;
            }

            // Word deletes take the platform word-action modifier (Option on macOS,
            // Ctrl elsewhere); other word-modified keys fall through to the keymap.
            if (chord == _wordModifiers && !shift && !readOnly && e.Key is Key.Back or Key.Delete)
            {
                e.Handled = e.Key == Key.Back
                    ? Run(static (v, b) => new WordDeleteToStartCommandArgs(v, b), () => operations.DeleteWordToLeft())
                    : Run(static (v, b) => new WordDeleteToEndCommandArgs(v, b), () => operations.DeleteWordToRight());
                return;
            }

            // Any other chord outside the platform command modifier (bare Alt on Windows,
            // bare Ctrl on macOS, combinations, …) is not ours; leave it to the keymap.
            bool meta = chord == _commandModifiers;
            if (chord != KeyModifiers.None && !meta)
            {
                return;
            }

            bool? handled = (e.Key, meta, shift) switch
            {
                // Keys where the bridge supplies the default editing behavior as the innermost
                // handler, like the editor-operations command target does in VS: always consumed.
                (Key.Enter, meta: false, shift: false) when !readOnly => Run(static (v, b) => new ReturnKeyCommandArgs(v, b), () => operations.InsertNewLine()),
                (Key.Tab, meta: false, shift: false) when !readOnly => Run(static (v, b) => new TabKeyCommandArgs(v, b), () => operations.Indent()),
                (Key.Tab, meta: false, shift: true) when !readOnly => Run(static (v, b) => new BackTabKeyCommandArgs(v, b), () => operations.Unindent()),
                (Key.Back, meta: false, shift: _) when !readOnly => Run(static (v, b) => new BackspaceKeyCommandArgs(v, b), () => operations.Backspace()),
                (Key.Delete, meta: false, shift: false) when !readOnly => Run(static (v, b) => new DeleteKeyCommandArgs(v, b), () => operations.Delete()),
                (Key.V, meta: true, shift: false) when !readOnly => Run(static (v, b) => new PasteCommandArgs(v, b), () => operations.Paste()),
                (Key.Insert, meta: false, shift: true) when !readOnly => Run(static (v, b) => new PasteCommandArgs(v, b), () => operations.Paste()),
                (Key.X, meta: true, shift: false) when !readOnly => Run(static (v, b) => new CutCommandArgs(v, b), () => operations.CutSelection()),
                (Key.C, meta: true, shift: false) => Run(static (v, b) => new CopyCommandArgs(v, b), () => operations.CopySelection()),
                (Key.Insert, meta: true, shift: false) => Run(static (v, b) => new CopyCommandArgs(v, b), () => operations.CopySelection()),

                // Commands with no default behavior: consumed only if a handler took them,
                // otherwise they fall through to the view's keymap.
                (Key.Space, meta: true, shift: true) when !readOnly => TryRun(static (v, b) => new InvokeSignatureHelpCommandArgs(v, b)),
                (Key.Escape, meta: false, shift: false) => TryRun(static (v, b) => new EscapeKeyCommandArgs(v, b)),
                // Arrows go to the command chain first (completion claims them while its session is
                // open); an active signature help session gets them next, cycling overloads the way
                // the VS shell routes arrows to the intellisense session stack. Otherwise they fall
                // through to the keymap and move the caret.
                (Key.Up, meta: false, shift: false) => TryRun(static (v, b) => new UpKeyCommandArgs(v, b)) || TryCycleSignatureHelp(previous: true),
                (Key.Up, meta: false, shift: true) => TryRun(static (v, b) => new UpKeyCommandArgs(v, b)),
                (Key.Down, meta: false, shift: false) => TryRun(static (v, b) => new DownKeyCommandArgs(v, b)) || TryCycleSignatureHelp(previous: false),
                (Key.Down, meta: false, shift: true) => TryRun(static (v, b) => new DownKeyCommandArgs(v, b)),
                (Key.PageUp, meta: false, shift: _) => TryRun(static (v, b) => new PageUpKeyCommandArgs(v, b)),
                (Key.PageDown, meta: false, shift: _) => TryRun(static (v, b) => new PageDownKeyCommandArgs(v, b)),
                (Key.Z, meta: true, shift: false) when !readOnly => TryRun(static (v, b) => new UndoCommandArgs(v, b)),
                (Key.Z, meta: true, shift: true) or (Key.Y, meta: true, shift: false) when !readOnly => TryRun(static (v, b) => new RedoCommandArgs(v, b)),
                // The `/` key surfaces as either OemQuestion or Oem2 depending on layout.
                (Key.OemQuestion or Key.Oem2, meta: true, shift: false) when !readOnly => TryRun(static (v, b) => new ToggleLineCommentCommandArgs(v, b)),
                (Key.D, meta: true, shift: true) when !readOnly => TryRun(static (v, b) => new FormatDocumentCommandArgs(v, b)),
                (Key.OemCloseBrackets, meta: true, shift: false) => TryRun(static (v, b) => new GotoBraceCommandArgs(v, b)),
                (Key.OemCloseBrackets, meta: true, shift: true) => TryRun(static (v, b) => new GotoBraceExtCommandArgs(v, b)),
                (Key.F12, meta: false, shift: false) => TryRun(static (v, b) => new GoToDefinitionCommandArgs(v, b)),
                (Key.F12, meta: true, shift: false) => TryRun(static (v, b) => new Microsoft.CodeAnalysis.Editor.Commanding.Commands.GoToImplementationCommandArgs(v, b)),
                // Suggested actions: command+; everywhere, because on macOS Cmd+. never
                // reaches the app — AppKit hardwires it as the cancel key equivalent
                // (NSResponder binds it to cancelOperation: alongside Escape) and consumes
                // it in the key-equivalent phase. Ctrl+. still works on Windows/Linux.
                (Key.OemSemicolon, meta: true, shift: false) when !readOnly => suggestedActions.Show(),
                (Key.OemPeriod, meta: true, shift: false) when !readOnly => suggestedActions.Show(),

                _ => null,
            };

            if (handled == true)
            {
                e.Handled = true;
            }
        }

        public void OnTextInput(TextInputEventArgs e)
        {
            if (view.IsClosed || string.IsNullOrEmpty(e.Text) ||
                view.Options.GetOptionValue(DefaultTextViewOptions.ViewProhibitUserInputId))
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

        private bool Run<T>(Func<ITextView, ITextBuffer, T> argsFactory, Action defaultAction)
            where T : EditorCommandArgs
        {
            commanding.Execute(argsFactory, defaultAction);
            return true;
        }

        private bool TryRun<T>(Func<ITextView, ITextBuffer, T> argsFactory)
            where T : EditorCommandArgs
        {
            bool fellThrough = false;
            commanding.Execute(argsFactory, () => fellThrough = true);
            return !fellThrough;
        }

        private bool TryCycleSignatureHelp(bool previous)
        {
            if (!signatureHelpBroker.IsSignatureHelpActive(view))
            {
                return false;
            }

            // With a single overload there is nothing to cycle; let the arrow move the caret
            // (Roslyn dismisses the session when the caret leaves the argument list).
            var session = signatureHelpBroker.GetSessions(view)[0];
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
}
