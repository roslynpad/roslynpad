# Morgania.Demo.EditorFeatures

A standalone Avalonia editor that hosts **Roslyn's real EditorFeatures language services** on
**Morgania** (the OSS Avalonia reimplementation of the Visual Studio editor, at
`../../../morgania`). It proves the `RoslynPad.CodeAnalysis.*EditorFeatures*` recompile
end-to-end: the exact code that drives C# completion in Visual Studio — completion, signature
help, classification, smart indent, brace completion, snippets, formatting — running against an
open-source editor with no VS shell.

## The three layers

| Layer | What it is |
|---|---|
| `Morgania.CodeAnalysis.EditorFeatures[.Text/.CSharp]` | Roslyn's `vendor/roslyn` EditorFeatures sources recompiled against Morgania's editor assemblies instead of the closed-source VS editor (see `.github/instructions/roslyn-editorfeatures-recompile.instructions.md`). |
| Morgania | The editor platform: text model, `IWpfTextView` (Avalonia), classification, tagging, Modern Commanding, async completion UI, signature help UI. Its vendored `vs-editor-api` Impl projects supply the standard editor services. |
| This demo | The **host**. Everything Visual Studio's shell normally provides — MEF composition, keyboard routing, host services, colors — lives here. |

## Composition: one VS-MEF graph

`CompositionFactory` builds a single MEF graph with **VS-MEF**
(`Microsoft.VisualStudio.Composition`), because the parts mix attribute flavors exactly like in
VS itself:

- Morgania and the vendored editor are MEF **v2** (`System.Composition`).
- Roslyn's Workspaces/Features packages are v2, but the recompiled EditorFeatures assemblies
  export their editor-facing parts (taggers, command handlers, completion sources) with **v1**
  attributes (`System.ComponentModel.Composition`).

The catalog is an explicit assembly list: ~34 Morgania/vendored-editor assemblies, the Roslyn
package assemblies, the three recompiled assemblies, and the demo itself. Parts whose imports
cannot be satisfied outside VS are *rejected gracefully* and written to `composition.log` next
to the binary — currently only Peek (needs the VS Peek UI) and Pythia (VS-internal ML) are
rejected. Anything else appearing there is a bug to fix.

## Roslyn workspace wiring

`RoslynDemoHost.OpenDocument` mirrors RoslynPad's `RoslynHost`:

1. `VisualStudioMefHostServices.Create(exportProvider)` bridges the VS-MEF `ExportProvider` to
   Roslyn `HostServices`, so workspace services and language services resolve from the same
   graph as the editor parts. (The type is internal to
   `Microsoft.CodeAnalysis.Remote.Workspaces`, reached via the `IgnoresAccessChecksTo`
   publicizer; that package comes from the Azure DevOps feeds through `RestoreHelper`.)
2. A `DemoWorkspace` (plain `Workspace` subclass) gets one C# project (preview language
   version, nullable enabled, framework references taken from `TRUSTED_PLATFORM_ASSEMBLIES`)
   and one document. Solution-level analyzer references to the compiler + Features assemblies
   supply the workspace's "host analyzers" — the source of compiler and IDE diagnostics for
   the pull-diagnostics taggers (the same set RoslynPad's `RoslynHost` registers).
3. The document is opened over the **editor buffer's own text container**
   (`buffer.AsTextContainer()`), which gives automatic buffer → workspace sync; the reverse
   direction (`ApplyDocumentTextChanged`, used by code fixes and formatting) is applied to the
   buffer as minimal text edits.

The buffer's content type is `"CSharp"`, registered by the recompiled EditorFeatures — that
content type is what routes every editor extension point (taggers, completion sources, command
handlers) to Roslyn.

## What the host provides (the VS-shell stand-ins)

| File | Role |
|---|---|
| `CommandingKeyBridge.cs` | The VS shell's keyboard → Modern Commanding translation. Tunneling Avalonia handlers turn `TextInput` into `TypeCharCommandArgs` (per char) and `KeyDown` into typed command args, with `IEditorOperations` as the innermost handler. Keys with no default behavior are consumed only if a handler takes them, otherwise they fall through to Morgania's keymap. Also routes Up/Down to an active signature help session (cycling overloads) when commanding doesn't consume them, and gives an open suggested-actions popup first claim on Up/Down/Enter/Tab/Escape — the VS shell's completion-stack routing. |
| `DiagnosticsSquiggleTaggerProvider.cs` | The classic diagnostics squiggle tagger. VS gets squiggles through LSP pull diagnostics, so upstream EditorFeatures no longer ships an `IErrorTag` tagger — but the pull-diagnostics tagging machinery (`AbstractDiagnosticsTaggerProvider`, which asks `IDiagnosticAnalyzerService` for compiler/analyzer diagnostics per kind) survives and runs in-proc. This subclass stands in for the deleted upstream tagger, mapping severities to `PredefinedErrorTypeNames`. |
| `SquiggleAdornmentManager.cs` | Draws the squiggly underlines: an `IErrorTag` view aggregator redrawn into the predefined Squiggle adornment layer on layout/tag changes, colored per error type (error red, warning yellow, suggestion blue). VS's squiggle renderer lives in the closed-source editor implementation. |
| `SuggestedActionsController.cs` | The VS light bulb stand-in. Gathers actions from the composed `ISuggestedActionsSourceProvider`s through the streaming `IAsyncSuggestedActionsSource` collector protocol (per-priority collectors matching Roslyn's exported orderings) and shows them as a native context menu at the caret, with nested action groups ("Suppress or configure issues", …) as cascading submenus that handle their own keyboard/mouse navigation. Invoking an action calls Roslyn's `EditorSuggestedAction.Invoke(IUIThreadOperationContext)`, which applies the code action through `CodeActionEditHandlerService` back into the buffer. Also owns the light bulb margin icon: caret moves/edits schedule a debounced `ISuggestedActionsSource2.GetSuggestedActionCategoriesAsync` query, and the resulting category is drawn as a clickable adornment at the caret line's left edge — `ErrorFix` (VS error bulb; red), `CodeFix`/`StyleFix` (yellow bulb), `Refactoring` (screwdriver; gray). Colored circles stand in until the real VS icons are added. |
| `HostExports.cs` | `JoinableTaskContext` (created on the UI thread, installs the Avalonia synchronization context), `ISmartIndentationService` (drives `ISmartIndentProvider`, i.e. Roslyn's smart indent), a no-op `ILoggingServiceInternal`. |
| `HostStubs.cs` | No-op/minimal implementations of Roslyn-internal host services that VS normally exports, without which VS-MEF would reject the completion/suggested-actions parts: `IStreamingFindUsagesPresenter`, `IIndentationManagerService`, `ISuggestedActionCategoryRegistryService`, `IPreviewFactoryService`, `ICodeDefinitionWindowService`. (Internal interfaces are reachable via `InternalsVisibleTo` from the recompiled assemblies.) |
| `LspSnippetExpander.cs` | `ILanguageServerSnippetExpander` — commits Roslyn's snippet completion items (`foreach`, `if`, `prop`, …). Parses the only two constructs Roslyn's `RoslynLSPSnippetConverter` emits (`$0`, `${n:placeholder}`), inserts the text, and selects the first placeholder. In VS this goes through the closed-source LSP client, which the recompile excludes. |
| `StandardClassificationDefinitions.cs` | The 16 `PredefinedClassificationTypeNames` classification types (keyword, comment, string, …). VS's StandardClassification implementation was never open-sourced. |
| `ClassificationFormats.cs` | Avalonia `ClassificationFormatDefinition` exports mapping Roslyn's classification types to VS dark-theme colors (keyword `#569CD6`, string `#CE9178`, class name `#4EC9B0`, …). These feed the editor view *and* the signature-help popup, which classifies through the same format map. |

## Startup flow (`App.cs`)

1. Build the composition; resolve `JoinableTaskContext` on the UI thread so it captures the
   main thread.
2. Create a `"CSharp"` text buffer with `SampleCode`, open the Roslyn document over it.
3. Create the Morgania text view (line numbers, brace completion enabled), set dark default
   text properties on its classification format map, wrap it in a `Window`.
4. In smoke mode, fail fast if any `ClassificationTypeNames` constant does not resolve through
   the semantic classification layer (a miss would crash Roslyn's taggers later), then run the
   smoke test.

## Running

```sh
# GUI (run from a real desktop session; agent shells fail with a CVDisplayLink error)
dotnet run --project src/Morgania.Demo.EditorFeatures

# Headless smoke test (Avalonia.Headless), exits 0 on success
dotnet run --project src/Morgania.Demo.EditorFeatures -- --smoke
```

`SmokeTest.cs` drives the real pipeline and prints one line per check:

1. **Classification** — waits for semantic tags (`class name`, `method name`, …) through a view
   tag aggregator.
2. **Completion** — triggers the async completion broker at `Console.` and requires `WriteLine`
   among the items.
3. **Return key** — executes `ReturnKeyCommandArgs` through `IEditorCommandHandlerService`
   (format → completion → brace-completion handlers → editor operations with Roslyn smart
   indent) and asserts a line was inserted.
4. **Brace completion** — types `(` through the command chain and expects the auto-closed `)`.
5. **Snippet commit** — selects the `foreach` *snippet* item (identified via
   `CompletionItemData` + `SnippetCompletionItem.IsSnippet`, not the keyword item) in a real
   session, commits it, asserts expansion and that the view **repaints** the inserted lines
   once classification arrives (watches `LayoutChanged` for the lines landing in
   `NewOrReformattedLines` with their classification present — pull-based `GetTags` cannot
   detect stale paint).
6. **Signature help** — invokes `InvokeSignatureHelpCommandArgs` inside `Console.WriteLine(`,
   asserts an active broker session with the overload list, that the popup's signature line is
   classified (keyword color on `void`), and that raising real Up/Down key events cycles the
   overloads.
7. **Diagnostics** — inserts a misspelled `Conosle.WriteLine(...)` statement and waits for the
   pull-diagnostics pipeline: an error tag (`CS0103`) over the identifier through a view tag
   aggregator, and a drawn adornment in the Squiggle layer.
8. **Quick fix** — waits for the `ErrorFix` light bulb icon at the misspelled line, opens the
   suggested-actions popup with a real Ctrl+. key event, asserts the list includes
   `Change 'Conosle' to 'Console'` (among fixes, refactorings, and expanded nested groups),
   invokes it via Enter through the bridge, and waits for the buffer edit to land, the error
   squiggles to clear, and the light bulb to stop showing `ErrorFix`.

## Keyboard bindings (bridge)

| Key | Command |
|---|---|
| typing | `TypeCharCommandArgs` per character |
| Enter / Tab / Shift+Tab / Backspace / Delete | return, tab, back-tab, backspace, delete |
| Ctrl/Cmd+Space | invoke completion |
| Ctrl/Cmd+Shift+Space | invoke signature help |
| Ctrl/Cmd+. | suggested actions (quick fixes + refactorings; the menu handles its own keys) |
| Up/Down (popup open) | completion selection, or signature-help overload cycling |
| Escape | dismiss |
| Ctrl/Cmd+X / C / V | cut / copy / paste |
| Ctrl/Cmd+Z, Ctrl/Cmd+Shift+Z or Ctrl/Cmd+Y | undo / redo |
| Ctrl/Cmd+/ | toggle line comment |
| Ctrl/Cmd+Shift+D | format document |

## What works / known gaps

Working end-to-end: semantic classification, async completion (with filtering and snippet
expansion), signature help (classified popup, overload cycling, auto-trigger on `(` and `,`),
diagnostics squiggles (compiler + IDE analyzer diagnostics via the pull-diagnostics taggers),
suggested actions (quick fixes and refactorings, applied back to the buffer, with a
severity-aware light bulb icon at the caret line), smart indent, brace completion (with
closing-brace highlight), automatic formatting on `;`/`}`, comment toggling, undo/redo.

Known gaps, by design of what's composed:

- **Minimal light bulb** — placeholder colored circles instead of the VS error-bulb /
  yellow-bulb / screwdriver icons, no action previews (`IPreviewFactoryService` is a no-op),
  no fix-all flavors, no per-action icons.
- **Go-to-definition / find-references** compute but present into the no-op
  `IStreamingFindUsagesPresenter`.
- **No linked editing / tab stops in snippets** — the expander inserts text and selects the
  first placeholder only.
