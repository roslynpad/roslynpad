# Editor Integration — Deep Dives

Companion to the Editor Integration section of the root `CLAUDE.md`: the full per-feature design detail and rationale. Keep `CLAUDE.md` to pointers and invariants; when a feature's implementation changes, update its section here.

## Composition

`RoslynHost` (`src/RoslynPad/Roslyn/`) obtains its export provider from `Morgania.CodeAnalysis.Editor`'s `EditorComposition`: a single **VS-MEF** (`Microsoft.VisualStudio.Composition`) graph containing Roslyn Workspaces/Features, the recompiled EditorFeatures assembly, the Morgania editor, and the host services; `HostServices` is created over it via the internal `VisualStudioMefHostServices`. Composition rejections are logged to `composition.log` in the app directory.

## Editor-host services and refactoring dialogs

`Morgania.CodeAnalysis.Editor` contains the editor-host services: the commanding key bridge, squiggle adornments + diagnostics tagger, the suggested-actions (light bulb) controller, classification format definitions, snippet expansion, an `ImageElement` view factory, refactoring dialogs, and no-op stubs for host services that don't apply.

The refactoring dialogs (Change Signature, Extract Interface, Pick Members) are `UserControl` contents (internal `DialogView` base implementing the public `IDialogView`) shown through the public `IDialogPresenter` contract: hosts may export a presenter (the app exports `RoslynPad.Roslyn.DialogPresenter`, which shows them in the main window's `DialogHost` overlay); without one they fall back to modal windows. `DialogService` picks the presenter and bridges its async result to Roslyn's synchronous options services via a dispatcher frame.

## Block structure guides

Roslyn's structure tagger comes from the recompile itself (the upstream `StructureTaggerProvider` concrete, together with `ViewHostingControl` and `ProjectionBufferContent`, was un-excluded once the collapsed-region hint's view hosting worked on Avalonia); the package also exports `BlockStructureAdornmentManager`, which draws vertical block structure guide lines from `IStructureTag`s into the `BlockStructure` layer (guides inferred from header/outlining spans per the `IStructureTag` contract; segments skip lines with text at the guide column). The app themes them from `editorIndentGuide.background1` (registry falls back through `editorIndentGuide.background` → `editorWhitespace.foreground`) via the `"Block Structure Guide"` editor-format-map key.

## Inline diagnostics

Inline diagnostics (Error-Lens-style message pills at the end of the line) are the recompiled Roslyn `InlineDiagnostics` UI, made compilable by shims in `Morgania.CodeAnalysis.EditorFeatures/Shims/`: `Hyperlink` over Morgania's clickable `NavigationTextBlock` inline, `CrispImage`/`KnownMonikers` resolving through `ImageCatalog` (ids from public KnownImageIds docs), and a no-op `ImageThemingUtilities`. Quick info type links use the same `NavigationTextBlock` (Avalonia inlines aren't input elements; it reports its text baseline via `TextBlock.BaselineOffset` so `EmbeddedControlRun` aligns it like a run). The feature is gated by Roslyn's `EnableInlineDiagnostics` option (off by default).

The app themes the pills from `editorError`/`editorWarning` + `textLink.foreground` (`ThemeClassificationFormats.ApplyInlineDiagnostics`), and exports `INavigateToLinkService` so the diagnostic-id link opens the docs in the browser. Like VS, the pill is skipped when it would overlap the code line (needs viewport room).

## Editor-UI option persistence and .editorconfig

Editor-UI options like `EnableInlineDiagnostics` are deliberately not editorconfig options (`isEditorConfigOption: false`; taggers read them from `IGlobalOptionService` only), so they persist in the untyped `roslyn` object of `RoslynPad.json` via `SettingsOptionPersister` — an app-exported `IOptionPersisterProvider`, the same hook VS's settings store uses: `IGlobalOptionService` consults it on the first read of each option and on every set, keys are option `ConfigName`s, values round-trip through each option's editorconfig `Serializer`. The persister lives in the VS-MEF graph while app settings are MEF2, so `MainViewModel` wires `Settings` into it right after creating the host (like `NavigationBridge`); the toolbar toggle (`MainViewModel.EnableInlineDiagnostics`) is then just a global-option read/write, updating on option-changed events.

Separately, the `.editorconfig` at the documents root serves real editorconfig options (severities, code style): `RoslynHost` attaches it to every project as an analyzer config document, watches it with a `FileSystemWatcher`, and on change pushes fresh text loaders into all live workspaces (`OnAnalyzerConfigDocumentTextLoaderChanged`, the same mechanism VS/LSP hosts use via `ProjectSystemProject`) — the workspace never re-reads the file on its own (loader-backed docs are snapshotted into `RecoverableTextAndVersion`).

## Classification formats and theming

The app's `Editor/` folder maps VS Code themes onto the composition (`ThemeClassificationFormats`). Classification colors come from the recompiled Roslyn `ClassificationTypeFormatDefinitions` (un-excluded from the recompile, needing only `Colors` and a `System.Windows.TextDecorations` shim); the host's `ClassificationFormats.cs` adds only the standard classifications neither Roslyn nor the editor supplies (keyword, comment, string, character, preprocessor keyword, excluded code, url) plus the editor-owned read-reference marker format and "TextView Background", all VS-light defaults. The remaining marker/tag formats (brace matching, definition/written-reference highlights, rename tracking, inline-rename highlights, conflict, preview warning) are Roslyn's own recompiled definitions, compilable via `Pen`/`DashStyle` aliases and a no-op `Freeze()` shim.

Buffer visibility tracking (taggers pause work for non-visible documents) is `AvaloniaTextBufferVisibilityTracker` in the recompile's `Stubs/`, mapping WPF `IsVisibleChanged` semantics onto visual-tree attach/detach (the dock detaches inactive document views).

The theme is authoritative: a classification the theme doesn't color has its explicit format cleared so it resolves through the base-type chain to the default foreground (VS Code semantics — no single-theme static color bleeds, e.g. no cyan parameters on a light background; `record class name` still inherits the themed `class name`). `RoslynPad.Themes` keeps the built-in vs2019 base theme's scopes in a **separate fallback trie** (`Theme.ScopeSettingsFallback`) consulted only when the theme and its includes have no match at any specificity, so a base rule can't outrank a theme rule via longest-prefix — xml doc comments render in the theme's `comment` color while Roslyn's semantic classifier still colors real cref/param references. The `Morgania.Demo.EditorFeatures` harness applies no theme and renders on a light canvas to match those VS-light defaults.

Built-in themes are the VS Code defaults under `src/RoslynPad.Themes/Themes/` (default: `2026-light`/`2026-dark`).

## Dock and scrollbar theming

The dock (Dock.Avalonia) is themed the same way: `ThemeDictionary.MapDockResources` maps VS Code colors onto Dock's semantic `Dock*Brush` resource keys, and `DockTheme.axaml` (included after `DockFluentTheme` in App.axaml) restyles the used dock controls into the rounded VS Code look (framed document card with floating tab pills, tool cards, invisible splitter gaps).

Scrollbars are VS Code-style app-wide: `ScrollBarTheme.axaml` (a Styles file included after the Fluent themes, carrying the control themes in `Styles.Resources` — assigning `Application.Resources` from XAML would replace the dictionary the `Icons` are code-merged into before XAML load) replaces the Fluent `ScrollBar` control theme with a rounded translucent thumb over a transparent track (no arrow buttons), colored from `scrollbarSlider.*` (`ThemeDictionary.MapScrollBarDefaults` fills VS Code's coded defaults for themes that don't set them); bars rest at opacity 0 and fade in via a single App.axaml style (`:pointerover` holds on every ancestor of the hovered element): `ScrollViewer:pointerover /template/ ScrollBar` for regular scroll viewers, plus `CodeEditorView:pointerover ScrollBar` / `ILViewer:pointerover ScrollBar` for the editor's margin bars — no code-behind. In Morgania, the host's right/bottom margin containers overlay the view's cell (VS Code overlay scrollbars — content flows under them), the margin `ScrollBar` subclasses take the base type's control theme via `StyleKeyOverride`, forward mouse wheel to the view, and the view auto-scrolls drag selection past the viewport edges on a timer.

## Code folding (outlining)

The vendored `OutliningManager` consumes `IOutliningRegionTag`s, and `Morgania.Editor`'s `StructureOutliningTaggerProvider` bridges `IStructureTag` → `IOutliningRegionTag` (collapsible multi-line tags only), so Roslyn's structure tagger drives folding with no Roslyn-side wiring.

Collapsing elides all but the region's **last character** (`OutliningElisionSupport`; extents end at a token, never a line break) — that visible character carries the clickable collapsed-form pill (`CollapsedRegionAdornmentProvider`, an intra-text adornment that replaces it) — chosen because the Avalonia text formatter cannot sequence zero-length adornment runs, and a non-empty replacement span negotiates space correctly even mid-line (`Foo(() => { … });`). Expanding re-elides regions still collapsed inside. Intra-text adornments are positioned baseline-on-baseline (tag default: bottom-on-baseline; the pill passes a centering baseline).

Resting the pointer on the pill shows the collapsed hint in the Modern ToolTip presenter with the popup background swapped for the editor's ("TextView Background") and the width cap lifted (viewport-bounded); the hint itself is upstream's recompiled WPF implementation: `StructureTaggerProvider.GetCollapsedHintForm` returns a `ViewHostingControl` (shimmed `System.Windows.Controls.ContentControl` whose `IsVisibleChanged` maps to visual-tree attach/detach — attach creates the buffer + view, detach closes the view and deletes projection spans) hosting a role-restricted (`OutliningRegionTextViewRole`) real text view over `CreateElisionBufferForTagTooltip`'s indentation-stripped projection of the hint span (capped at 1000 chars + "…"). Because it's a live view, classification comes from the view taggers and the preview recolorizes when semantic classification lands, like the editor itself.

Two editor-level contracts make such preview views behave: a view without `Interactive` in its roles takes no user input (not focusable, hidden caret, pointer/key/wheel ignored — `WpfTextView.AllowsUserInput`; upstream's `ZoomLevel × 0.75` is likewise inert because zoom is `Zoomable`-gated), and `WpfTextView.MeasureOverride` answers an unconstrained measure (a popup sizing to content) with the full text extent — the viewport-driven layout only formats visible lines, so anything reading the extent after one layout pass (`SizeToFit`) under-measures.

Text hover never fires over the pill: `GetBufferPositionFromXCoordinate(textOnly: true)` returns null for spans replaced by space-negotiating adornments (adornments own that ground), which keeps quick info (e.g. Roslyn's close-brace hover on the kept `}`) from double-popping.

The outlining margin (`PredefinedMarginNames.Outlining`, left container after LineNumber) draws VS Code-style chevrons: collapsed always, expanded on margin hover; click toggles (innermost region on the line). Ctrl/Cmd+M starts a two-key chord in `CommandingKeyBridge` — +M toggle region, +L toggle all, +O collapse to definitions — dispatched to the vendored `OutliningCommandHandler`. The app themes the chevrons from `editorGutter.foldingControlForeground` and the pill from `editor.foldPlaceholderForeground` via the `"Outlining Margin"`/`"Collapsed Text Adornment"` format-map keys (`ThemeClassificationFormats.ApplyOutlining`), and `CodeEditorView.NavigateToSpan` expands folds containing the target.

## Glyphs and intellisense popups

Glyphs come from the image catalog: `Morgania.CodeAnalysis.Editor`'s `Glyphs.axaml` is generated by `Resources/Glyphs/generate-glyphs.py`, and `ImageCatalog` resolves moniker names/`ImageId`s and adapts icon colors to the theme background. The completion popups (quick info, signature help, completion) read their palette from the `"Intellisense Popup"` editor-format-map key (`PopupFormatNames`), which `ThemeClassificationFormats.ApplyPopup` fills from the VS Code theme.

## Buffer and view hosting

`DocumentView` creates an `ITextBuffer` with the CSharp content type, opens the Roslyn document over `buffer.AsTextContainer()`, and hosts the `ITextViewHost` control. Workspace-applied changes (code fixes, formatting) round-trip through minimal buffer edits.

## Clipboard

The OS clipboard is the single source of truth — no in-process store, no WPF-shaped shim. `TextViewClipboardExtensions` (Morgania.Editor) holds the whole Avalonia integration: `EditorOperations.CopyToClipboard` pushes fire-and-forget through the internal `SetClipboardText` extension (text plus the line/box cut-copy tags as application data formats, so they round-trip through the OS), and because `IEditorOperations.Paste` is synchronous while Avalonia's clipboard is async-only (X11 paste is IPC with the selection owner), every paste entry point (`CommandingKeyBridge`, `EditorContextMenu`, the view keymap) routes through the public `PasteFromClipboardAsync`, which fetches the clipboard of the view's TopLevel, primes the view's property bag with a `PendingClipboardPaste` snapshot, runs the synchronous dispatch, then clears it — `EditorOperations.Paste`/`CanPaste` read only that primed property.

## Find/replace

`FindReplacePanel` (Morgania.Editor, original — in VS the find UI belongs to the shell) floats over the view's top-right corner for every interactive "text" view, built on `ITextSearchService2`/`ITextSearchNavigator3`. It owns its view-level chords as six rebindable `KeyGesture?` properties (`ShowGesture`, `ShowReplaceGesture`, `FindNextGesture`, `FindPreviousGesture`, `ReplaceNextGesture`, `ReplaceAllGesture`; null unbinds) with platform defaults derived from `PlatformHotkeyConfiguration.CommandModifiers` (Cmd+F/Cmd+Alt+F/Cmd+G vs Ctrl+F/Ctrl+H/F3; Alt+R/Alt+A everywhere) — the replace buttons' tooltips follow the gesture; Enter/Shift+Enter/Escape are fixed panel-internal keys. On a read-only view (`ViewProhibitUserInputId`) the panel is find-only: `Show` hides the replace row and `ReplaceNext`/`ReplaceAll` refuse (the option only gates view input, not buffer edits).

Programmatic invocation is commanding like any other editor feature: Morgania-original `ShowFindCommandArgs`/`ShowReplaceCommandArgs` + `FindReplaceCommandHandler` route to the panel. In the app, `CodeEditorView` is the single place that touches the panel type — it pushes `KeyBindings.Service` gestures into the panel per view (user rebinds genuinely replace defaults) and exposes `InvokeFindReplace` (command dispatch, like `InvokeRename`); the macOS native menu's Cmd+F/Cmd+Alt+F key equivalents are consumed by AppKit before Avalonia key routing, so `MainWindow` routes menu Find/Replace to the `CodeEditorView` containing the focused element (metadata tabs and the IL viewer included), falling back to `ActiveContent`'s `FindRequested`/`FindReplaceRequested` events. The panel is themed from VS Code `editorWidget.*`/`input.*` colors via the `"Find Replace"` editor-format-map key (`ThemeClassificationFormats.ApplyFindReplace`).

## Navigation

F12 / Cmd+F12 (and the editor context menu, `EditorContextMenu`) dispatch `GoToDefinition`/`GoToImplementation` through the commanding chain; multi-result searches surface in `StreamingFindUsagesPresenter`'s picker menu at the caret — all view-layer concerns in `Morgania.CodeAnalysis.Editor`. Read-only views (`ViewProhibitUserInputId`) suppress editing chords in `CommandingKeyBridge`, disable editing context-menu items, and hide the light bulb.

Navigation *policy* is the app's: `src/RoslynPad/Roslyn/Navigation/` exports `IDocumentNavigationService`/`ISymbolNavigationService` at `ServiceLayer.Host` (overriding the package's no-op stubs), bridged to `MainViewModel` through `NavigationBridge`/`INavigationHost` because the UI lives in the separate MEF2 container.

Metadata symbols go through `IMetadataAsSourceFileService`: Source Link / PDB sources first (the app's `SourceLinkService` downloads portable PDBs from msdl/nuget symbol servers and Source Link files over HTTP, cached under `%TMP%/roslynpad/symbols` — the piece Roslyn otherwise delegates to the debugger), else ILSpy decompilation via the app's `DecompilationService` wrapper over the vendored Roslyn decompiler sources. Results open as read-only tabs (`MetadataDocumentViewModel`/`MetadataDocumentView`) whose buffers register into the metadata-as-source workspace via `TryAddDocumentToWorkspace`, so classification, quick info, and further go-to navigation work inside them. Both `DocumentView` and `MetadataDocumentView` host the editor through the shared `CodeEditorView` control (buffer/view creation, font + theme wiring, span navigation).

While an async navigation is in flight (decompilation, symbol/Source Link downloads), Morgania's `BackgroundWorkIndicatorService` shows a VS Code-style indeterminate progress bar at the top of the view, themed via the `"Background Work Indicator"` editor-format-map key from `progressBar.background`.

## Inline rename

Roslyn's type-in-buffer rename, without the (excluded, WPF) flyout UI — `RenameCommandHandler` tolerates the missing adornment, so the session is keyboard-driven: F2 (`CommandingKeyBridge` and the context menu) starts it, typing edits the field with linked spans propagating to every reference, Enter commits with conflict resolution, Escape cancels — all through the already-bridged command chain (completion is feature-disabled during a session).

The session's plumbing is fully recompiled: the editor-level `UndoManagerServiceFactory` (over Morgania's undo history, which `TextEditorFactoryService` creates per buffer), the restored rename highlight-tag definitions (`ApplyInlineRename` paints the field with VS's green rename wash — VS Code has no in-buffer-rename color — light `#D3F8D3` / dark `#2B4B2B`; the `inline rename field` classification is cleared per theme like any unthemed classification), and `ITextBufferAssociatedViewService` — which tracks buffer↔view association via `ITextViewConnectionListener`, raised by Morgania's `TextEditorFactoryService` on view creation/close (buffer graphs are fixed per view, so `TextViewLifetime` is the only connection reason).

The package's no-op `IDocumentNavigationService` stub exports at `ServiceLayer.Editor`: Roslyn ships a Features-layer default under the same contract, and two same-layer workspace services throw on resolution.

The app's toolbar button and menu raise `OpenDocumentViewModel.RenameRequested`, which `DocumentView` routes to `CodeEditorView.InvokeRename` (focus + `RenameCommandArgs` through the command chain).

## Trailing-expression diagnostics

`ExecutionHost` rewrites a trailing bare expression (last global expression statement with a missing semicolon, `BuildCode.FindTrailingExpression`) into a `.Dump()` call before compiling, so the editor workspace's CS1002 (missing semicolon) and CA1806 (ignored result) on that statement are noise. Compiler errors are `NotConfigurable` — no pragma, editorconfig severity, `SpecificDiagnosticOptions`, or `DiagnosticSuppressor` can hide them (`AnalyzerDriver.ApplyProgrammaticSuppressionsCore` excludes errors outright) — so the app overrides the one seam every consumer (squiggle tagger, inline diagnostics, light bulb) reads from: `TrailingExpressionDiagnosticsFilter` (`src/RoslynPad/Roslyn/`) exports `IDiagnosticAnalyzerService` as a workspace-service factory at `ServiceLayer.Host`, wraps the default `DiagnosticAnalyzerService`, and drops exactly those two diagnostics by span (the missing `SemicolonToken`'s zero-width span / the statement expression's span), leaving e.g. a missing semicolon inside a nested lambda visible. There is no other ID-based diagnostic filtering: the former `DiagnosticsSquiggles.DisabledDiagnostics` static and `RoslynHost.DisabledDiagnostics` (which hid `#r`/`#load`-related CS1701/CS1702/CS7011/CS8097 in the script era) were removed as redundant under the file-based-app format.

## Demo harness

`Morgania.Demo.EditorFeatures` is a self-contained harness for the same integration (`--smoke` runs headless and exercises classification/completion).
