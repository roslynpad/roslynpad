# RoslynPad

## Project Overview

RoslynPad is a cross-platform C# editor built on Roslyn (compiler services), the Morgania editor (an Avalonia editor based on the vs-editor-api repo), and Avalonia. A single Avalonia application serves Windows, macOS, and Linux; there is no WPF code.

## Architecture

### Layer Structure (bottom to top)

```
RoslynPad.Runtime       → Injected into user scripts (.Dump() extension methods)
Morgania.Editor[.Abstractions] → The editor engine (vendored vs-editor-api) implemented on Avalonia
Morgania.CodeAnalysis.EditorFeatures → Roslyn EditorFeatures (Text+Core+CSharp) recompiled from vendor/roslyn against Morgania
Morgania.CodeAnalysis.Editor → VS-MEF composition (EditorComposition) + editor-host services (classification formats, squiggles, light bulb, glyphs, refactoring dialogs)
RoslynPad.Themes        → VS Code theme parsing
RoslynPad.Runtime.Secrets → Cross-platform secret storage
RoslynPad               → The app: entry point, Roslyn host/workspaces (Roslyn/), script compilation + execution (Build/), ViewModels/services (UI/), theme→editor mapping (Editor/)
```

The four `Morgania.*` library projects plus `RoslynPad.Themes` and `RoslynPad.Runtime.Secrets` are published as NuGet packages — each packs its `README.md`; they are listed in `docs/packages/README.md`. The demos and the app are not packable.

### Editor Integration

The app is a full editor host for the Morgania editor:

- `RoslynHost` (`src/RoslynPad/Roslyn/`) obtains its export provider from `Morgania.CodeAnalysis.Editor`'s `EditorComposition`: a single **VS-MEF** (`Microsoft.VisualStudio.Composition`) graph containing Roslyn Workspaces/Features, the recompiled EditorFeatures assembly, the Morgania editor, and the host services; `HostServices` is created over it via the internal `VisualStudioMefHostServices`. Composition rejections are logged to `composition.log` in the app directory.
- `Morgania.CodeAnalysis.Editor` contains the editor-host services: the commanding key bridge, squiggle adornments + diagnostics tagger, the suggested-actions (light bulb) controller, classification format definitions, snippet expansion, an `ImageElement` view factory, refactoring dialogs, and no-op stubs for host services that don't apply. The refactoring dialogs (Change Signature, Extract Interface, Pick Members) are `UserControl` contents (internal `DialogView` base implementing the public `IDialogView`) shown through the public `IDialogPresenter` contract: hosts may export a presenter (the app exports `RoslynPad.Roslyn.DialogPresenter`, which shows them in the main window's `DialogHost` overlay); without one they fall back to modal windows. `DialogService` picks the presenter and bridges its async result to Roslyn's synchronous options services via a dispatcher frame. Roslyn's structure tagger comes from the recompile itself (the upstream `StructureTaggerProvider` concrete, together with `ViewHostingControl` and `ProjectionBufferContent`, was un-excluded once the collapsed-region hint's view hosting worked on Avalonia); the package also exports `BlockStructureAdornmentManager`, which draws vertical block structure guide lines from `IStructureTag`s into the `BlockStructure` layer (guides inferred from header/outlining spans per the `IStructureTag` contract; segments skip lines with text at the guide column). The app themes them from `editorIndentGuide.background1` (registry falls back through `editorIndentGuide.background` → `editorWhitespace.foreground`) via the `"Block Structure Guide"` editor-format-map key.
- Inline diagnostics (Error-Lens-style message pills at the end of the line) are the recompiled Roslyn `InlineDiagnostics` UI, made compilable by shims in `Morgania.CodeAnalysis.EditorFeatures/Shims/`: `Hyperlink` over Morgania's clickable `NavigationTextBlock` inline, `CrispImage`/`KnownMonikers` resolving through `ImageCatalog` (ids from public KnownImageIds docs), and a no-op `ImageThemingUtilities`. Quick info type links use the same `NavigationTextBlock` (Avalonia inlines aren't input elements; it reports its text baseline via `TextBlock.BaselineOffset` so `EmbeddedControlRun` aligns it like a run). The feature is gated by Roslyn's `EnableInlineDiagnostics` option (off by default). Editor-UI options like this are deliberately not editorconfig options (`isEditorConfigOption: false`; taggers read them from `IGlobalOptionService` only), so they persist in the untyped `roslyn` object of `RoslynPad.json` via `SettingsOptionPersister` — an app-exported `IOptionPersisterProvider`, the same hook VS's settings store uses: `IGlobalOptionService` consults it on the first read of each option and on every set, keys are option `ConfigName`s, values round-trip through each option's editorconfig `Serializer`. The persister lives in the VS-MEF graph while app settings are MEF2, so `MainViewModel` wires `Settings` into it right after creating the host (like `NavigationBridge`); the toolbar toggle (`MainViewModel.EnableInlineDiagnostics`) is then just a global-option read/write, updating on option-changed events. Separately, the `.editorconfig` at the documents root serves real editorconfig options (severities, code style): `RoslynHost` attaches it to every project as an analyzer config document, watches it with a `FileSystemWatcher`, and on change pushes fresh text loaders into all live workspaces (`OnAnalyzerConfigDocumentTextLoaderChanged`, the same mechanism VS/LSP hosts use via `ProjectSystemProject`) — the workspace never re-reads the file on its own (loader-backed docs are snapshotted into `RecoverableTextAndVersion`). The app themes the pills from `editorError`/`editorWarning` + `textLink.foreground` (`ThemeClassificationFormats.ApplyInlineDiagnostics`), and exports `INavigateToLinkService` so the diagnostic-id link opens the docs in the browser. Like VS, the pill is skipped when it would overlap the code line (needs viewport room). The app's `Editor/` folder maps VS Code themes onto the composition (`ThemeClassificationFormats`). The dock (Dock.Avalonia) is themed the same way: `ThemeDictionary.MapDockResources` maps VS Code colors onto Dock's semantic `Dock*Brush` resource keys, and `DockTheme.axaml` (included after `DockFluentTheme` in App.axaml) restyles the used dock controls into the rounded VS Code look (framed document card with floating tab pills, tool cards, invisible splitter gaps). Built-in themes are the VS Code defaults under `src/RoslynPad.Themes/Themes/` (default: `2026-light`/`2026-dark`).
- Code folding (outlining): the vendored `OutliningManager` consumes `IOutliningRegionTag`s, and `Morgania.Editor`'s `StructureOutliningTaggerProvider` bridges `IStructureTag` → `IOutliningRegionTag` (collapsible multi-line tags only), so Roslyn's structure tagger drives folding with no Roslyn-side wiring. Collapsing elides all but the region's **last character** (`OutliningElisionSupport`; extents end at a token, never a line break) — that visible character carries the clickable collapsed-form pill (`CollapsedRegionAdornmentProvider`, an intra-text adornment that replaces it) — chosen because the Avalonia text formatter cannot sequence zero-length adornment runs, and a non-empty replacement span negotiates space correctly even mid-line (`Foo(() => { … });`). Expanding re-elides regions still collapsed inside. Intra-text adornments are positioned baseline-on-baseline (tag default: bottom-on-baseline; the pill passes a centering baseline). Resting the pointer on the pill shows the collapsed hint in the Modern ToolTip presenter with the popup background swapped for the editor's ("TextView Background") and the width cap lifted (viewport-bounded); the hint itself is upstream's recompiled WPF implementation: `StructureTaggerProvider.GetCollapsedHintForm` returns a `ViewHostingControl` (shimmed `System.Windows.Controls.ContentControl` whose `IsVisibleChanged` maps to visual-tree attach/detach — attach creates the buffer + view, detach closes the view and deletes projection spans) hosting a role-restricted (`OutliningRegionTextViewRole`) real text view over `CreateElisionBufferForTagTooltip`'s indentation-stripped projection of the hint span (capped at 1000 chars + "…"). Because it's a live view, classification comes from the view taggers and the preview recolorizes when semantic classification lands, like the editor itself. Two editor-level contracts make such preview views behave: a view without `Interactive` in its roles takes no user input (not focusable, hidden caret, pointer/key/wheel ignored — `WpfTextView.AllowsUserInput`; upstream's `ZoomLevel × 0.75` is likewise inert because zoom is `Zoomable`-gated), and `WpfTextView.MeasureOverride` answers an unconstrained measure (a popup sizing to content) with the full text extent — the viewport-driven layout only formats visible lines, so anything reading the extent after one layout pass (`SizeToFit`) under-measures. Text hover never fires over the pill: `GetBufferPositionFromXCoordinate(textOnly: true)` returns null for spans replaced by space-negotiating adornments (adornments own that ground), which keeps quick info (e.g. Roslyn's close-brace hover on the kept `}`) from double-popping. The outlining margin (`PredefinedMarginNames.Outlining`, left container after LineNumber) draws VS Code-style chevrons: collapsed always, expanded on margin hover; click toggles (innermost region on the line). Ctrl/Cmd+M starts a two-key chord in `CommandingKeyBridge` — +M toggle region, +L toggle all, +O collapse to definitions — dispatched to the vendored `OutliningCommandHandler`. The app themes the chevrons from `editorGutter.foldingControlForeground` and the pill from `editor.foldPlaceholderForeground` via the `"Outlining Margin"`/`"Collapsed Text Adornment"` format-map keys (`ThemeClassificationFormats.ApplyOutlining`), and `CodeEditorView.NavigateToSpan` expands folds containing the target.
- Glyphs come from the image catalog: `Morgania.CodeAnalysis.Editor`'s `Glyphs.axaml` is generated by `Resources/Glyphs/generate-glyphs.py`, and `ImageCatalog` resolves moniker names/`ImageId`s and adapts icon colors to the theme background. The completion popups (quick info, signature help, completion) read their palette from the `"Intellisense Popup"` editor-format-map key (`PopupFormatNames`), which `ThemeClassificationFormats.ApplyPopup` fills from the VS Code theme.
- `DocumentView` creates an `ITextBuffer` with the CSharp content type, opens the Roslyn document over `buffer.AsTextContainer()`, and hosts the `ITextViewHost` control. Workspace-applied changes (code fixes, formatting) round-trip through minimal buffer edits.
- Navigation: F12 / Cmd+F12 (and the editor context menu, `EditorContextMenu`) dispatch `GoToDefinition`/`GoToImplementation` through the commanding chain; multi-result searches surface in `StreamingFindUsagesPresenter`'s picker menu at the caret — all view-layer concerns in `Morgania.CodeAnalysis.Editor`. Read-only views (`ViewProhibitUserInputId`) suppress editing chords in `CommandingKeyBridge`, disable editing context-menu items, and hide the light bulb. Navigation *policy* is the app's: `src/RoslynPad/Roslyn/Navigation/` exports `IDocumentNavigationService`/`ISymbolNavigationService` at `ServiceLayer.Host` (overriding the package's no-op stubs), bridged to `MainViewModel` through `NavigationBridge`/`INavigationHost` because the UI lives in the separate MEF2 container. Metadata symbols go through `IMetadataAsSourceFileService`: Source Link / PDB sources first (the app's `SourceLinkService` downloads portable PDBs from msdl/nuget symbol servers and Source Link files over HTTP, cached under `%TMP%/roslynpad/symbols` — the piece Roslyn otherwise delegates to the debugger), else ILSpy decompilation via the app's `DecompilationService` wrapper over the `Microsoft.CodeAnalysis.LanguageServer.Protocol` assembly (referenced but not part-scanned). Results open as read-only tabs (`MetadataDocumentViewModel`/`MetadataDocumentView`) whose buffers register into the metadata-as-source workspace via `TryAddDocumentToWorkspace`, so classification, quick info, and further go-to navigation work inside them. Both `DocumentView` and `MetadataDocumentView` host the editor through the shared `CodeEditorView` control (buffer/view creation, font + theme wiring, span navigation). While an async navigation is in flight (decompilation, symbol/Source Link downloads), Morgania's `BackgroundWorkIndicatorService` shows a VS Code-style indeterminate progress bar at the top of the view, themed via the `"Background Work Indicator"` editor-format-map key from `progressBar.background`.
- `Morgania.Demo.EditorFeatures` is a self-contained harness for the same integration (`--smoke` runs headless and exercises classification/completion).

### Key Classes

- `RoslynHost` - Central Roslyn services host using VS-MEF composition
- `RoslynWorkspace` - Per-document Roslyn workspace extending `Microsoft.CodeAnalysis.Workspace`
- `ExecutionHost` - Compiles and executes scripts in separate processes via JSON IPC
- `MainViewModel` - Application state and document management
- `OpenDocumentViewModel` - Individual document state, execution, NuGet integration

## Build Commands

```bash
# Build the app
dotnet build src/RoslynPad

# Build full solution
dotnet build RoslynPad.slnx

# CI build with binary log
dotnet build -bl -c Release -m:1 RoslynPad.slnx

# Headless editor smoke test
dotnet run --project src/Morgania.Demo.EditorFeatures -- --smoke

# Morgania test suites (not in the slnx; run each the same way)
dotnet test tests/Morgania.IntellisenseTests   # also: BehaviorTests, CompositionTests, GeometryTests
```

Requires **.NET 10 SDK** (see `global.json`). Also install .NET 8 SDK for LTS library targets.

## Dependency Injection

Uses **System.Composition (MEF2)**, not Microsoft.Extensions.DependencyInjection:

```csharp
// Export service
[Export(typeof(IMyService)), Shared]
public class MyService : IMyService { }

// Constructor injection
[Export(typeof(MyViewModel)), Shared]
[method: ImportingConstructor]
public class MyViewModel(IMyService service) : NotificationObject { }
```

The `[Shared]` attribute indicates singleton lifetime.

## Key Conventions

### Naming
- Private fields: `_camelCase`
- Static fields: `s_camelCase`

### ViewModels
- Inherit from `NotificationObject` (provides `INotifyPropertyChanged`, error tracking)
- Use `SetProperty(ref field, value)` for property change notification
- Commands via `ICommandProvider.Create()` / `CreateAsync()`

### Roslyn Internal APIs

RoslynPad relies heavily on `IgnoresAccessChecksToGenerator`, which generates `[assembly: IgnoresAccessChecksTo("AssemblyName")]` attributes for Roslyn assemblies. This allows accessing non-public members and features that aren't part of Roslyn's public API surface.

**Benefits:**
- Enables consuming internal Roslyn features required for rich editor functionality
- Allows integration with internal workspace and completion APIs

**Trade-offs:**
- **Roslyn version upgrades can break the build** - internal APIs may change, be renamed, or removed between versions
- When upgrading Roslyn packages, expect to fix compilation errors where internal members have changed
- Requires careful testing after any Roslyn version bump

Relevant internal namespaces are exposed via `IgnoresAccessChecksTo` items in `Morgania.CodeAnalysis.EditorFeatures.csproj`, `Morgania.CodeAnalysis.Editor.csproj`, `Morgania.Demo.EditorFeatures.csproj`, and `RoslynPad.csproj`. The recompiled EditorFeatures assembly additionally grants `InternalsVisibleTo` to `Morgania.CodeAnalysis.Editor`, the demo host, and the app (`RoslynPad`) for the Roslyn-internal host interfaces they implement.

## Runtime Assembly

`RoslynPad.Runtime` is injected into executed user scripts. It:
- Multi-targets `net8.0;netstandard2.0` for broad compatibility
- Provides `.Dump()` extension methods
- Has no external dependencies
- Communicates with host via JSON over stdout/stdin

## Packaging & Deployment

Scripts in `deploy/` handle release packaging:

```bash
# From deploy/ directory - builds platform packages
./CreatePackages.ps1    # Runs on current OS, creates packages for that platform

# NuGet publishing (for library packages)
./PushNuGet.ps1         # Packs and pushes to nuget.org
```

**Platform Packaging** (all from `RoslynPad`):
- **Windows**: creates `.zip` + `.appx` (Microsoft Store), updates winget manifests
- **macOS**: creates `.dmg` (requires `appdmg` via npm) and `.tgz`
- **Linux**: creates `.tgz`

**Key Details:**
- `Common.ps1` - Shared functions, reads version from `Directory.Build.props`
- Windows packages run on Windows, macOS/Linux packages run on macOS
- `dotnet publish -r <rid>` with `ContinuousIntegrationBuild=true` for reproducible builds
- Version is centrally defined as `RoslynPadVersion` in `Directory.Build.props`

## Important Files

- `Directory.Build.props` - Shared build settings, version numbers, target frameworks
- `Directory.Packages.props` - Central package version management
- `src/Morgania.CodeAnalysis.Editor/EditorComposition.cs` - VS-MEF editor composition
- `src/RoslynPad/Build/ExecutionHost.cs` - Script execution engine
- `src/RoslynPad/Roslyn/RoslynHost.cs` - Roslyn host over the editor composition
- `src/RoslynPad/UI/ViewModels/` - Core application ViewModels
- `deploy/CreatePackages.ps1` - Release packaging script

## Maintenance

- Make sure to update this document when making significant changes to the repo.
