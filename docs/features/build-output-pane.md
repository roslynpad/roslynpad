# Build Output Pane

**Status: implemented** (July 2026). Sections below describe the shipped design; deltas from
the original plan are noted inline.

## 1. Goal

A dockable "Build" pane that shows MSBuild output as it streams, rendered in a read-only
Morgania editor view (`IWpfTextView`) with per-line color classification modeled on
[VSColorOutput64](https://github.com/mike-ward/VSColorOutput64)'s `ColorClassifier`, themed
correctly in both light and dark themes. A combo selects which output to show — **Restore**
(default; shows cached output when the restore cache hits) or **Compile** — and the selection
auto-switches to Compile when compilation starts.

## 2. Current state (what this builds on)

- Every build call site drains process stdout with `.LastOrDefaultAsync(ct)` purely to await
  exit ([ExecutionHost.cs](../../src/RoslynPad/Build/ExecutionHost.cs) `CompileWithMsbuild`,
  `DoRestoreAsync`); the text is accumulated but never shown. `ProcessUtil.GetStandardOutputLinesAsync`
  is already a streaming `IAsyncEnumerable<string>` — the hook exists, nothing consumes it live.
- Restore invokes `dotnet build … -getTargetResult:build -getItem:…`, so its **stdout is the
  `-getTargetResult` JSON payload**, not a human-readable log. Compile uses `-v:q`, which prints
  almost nothing. Both need command-line changes to produce output worth showing.
- Restore results are cached under `%TEMP%/roslynpad/restore/<hash>` with a `.restored` marker;
  on a hit the restore process is **skipped entirely** — there is no output unless we persist it.
- `ILViewer` + `ILClassification.cs` are the existing template for a read-only editor view over
  a custom content type with a line-based classifier.
- `ThemeClassificationFormats.Apply` treats the theme as authoritative: any classification the
  theme doesn't style gets its explicit format **cleared**. Static `ClassificationFormatDefinition`
  colors on new types are wiped on every theme application — theme mapping is mandatory, not
  optional polish.

## 3. UX

- The bottom `ResultPane` was converted from a `ToolDock` to a **`DocumentDock`** — VS Code
  panel style: pill tabs on top (`Results` | `Output` | `IL`; the output document keeps the id
  `Build`), no chrome title bar. The
  existing `DocumentControl` template + pill styles in `DockTheme.axaml` provide the look with
  no template surgery; the tabs are fixed (`CanClose/CanFloat/CanPin/CanDrag` all false, so
  they cannot be dragged into the documents pane). Content is per-document (bound to
  `CurrentOpenDocument`, like `ResultsView`).
- The **source combo** (`Restore` | `Compile`) sits at the right end of the pill row, like VS
  Code's panel actions area: the `PaneHeader.Content` attached property carries per-tab header
  content on the dock model object, and the `DocumentControl` template presents the *active*
  tab's content right-docked next to the tab strip — plain tabs contribute nothing. Defaults
  to Restore; when restore output was served from the cache, the item reads `Restore (cached)`.
  Gotcha: saved dock layouts deserialize *fresh* `Document` objects (titles serialized, attached
  properties not), so `LoadDockLayout` re-applies `Title` and `PaneHeader.Content` onto the
  restored documents from the XAML instances — otherwise the combo disappears from the second
  launch on and tab renames never stick.
- Auto-switch: when compilation starts, the selection flips to Compile; when a restore
  *actually runs* (cache miss), it flips to Restore. The user can switch freely at any time;
  streaming continues into the backing store regardless of what's displayed.
- Text streams in as the build runs, line by line. The view sticks to the tail while scrolled
  to the bottom; scrolling up disengages auto-scroll until the user returns to the bottom.
- No tab stealing: the Build tab never activates itself (Results already auto-activates on
  results; two panes fighting over activation during a single run would be worse than none).
- The view is read-only (`ViewProhibitUserInputId`, no line-number margin) but fully
  selectable/copyable, with find (`FindReplacePanel` comes free with the editor view).

## 4. Producing streamable output (`ExecutionHost`)

### 4.1 Command-line changes

- **Restore** (`DoRestoreAsync`): redirect the JSON payload off stdout with
  `-getResultOutputFile:<file>` (MSBuild 17.11+; the .NET 10 SDK is well past that — verify once
  during implementation), then read/parse the JSON from that file instead of `StandardOutput`.
  Drop `-flp:errorsonly` error parsing only if the JSON file keeps carrying what we need — the
  error-log path stays as is otherwise. Stdout becomes a normal MSBuild log; use `-v:m`.
- **Restore becomes a design-time build** (non-script path). Today "restore" fully builds a
  skeleton project just to harvest `-getItem:ReferencePathWithRefAssemblies,Analyzer`; the
  compiled output is discarded (only the top-level restore files are copied to `BuildPath`, and
  the real user compile is a separate `dotnet build`). Switch to the VS approach
  ([design-time-builds.md](https://github.com/dotnet/project-system/blob/main/docs/design-time-builds.md)):
  target `Compile` (not `Build` — its packaging steps fail without a compiled assembly) with
  `-p:DesignTimeBuild=true -p:SkipCompilerExecution=true`. Reference resolution runs before
  compilation, so `-getItem` still yields the same items; csc and copy-to-output are skipped.
  `DesignTimeBuild=true` is also the documented contract that tells custom targets in user-added
  NuGet packages to skip their expensive/output-producing work. `ProvideCommandLineArgs` is not
  needed — RoslynPad authors the csproj, so it already knows langversion/defines; references +
  analyzers are the only harvest.
- **The restore cache stays and composes with this.** The cache is agnostic to how the restore
  payload is produced — it hashes the csproj and stores whatever the invocation leaves in the
  hashed directory. With a design-time restore, the `Program.cs` = `_ = 0;` stub is no longer
  needed (it existed only so `Build`'s csc + packaging succeeded), the cache directory shrinks
  to csproj + `nuget.config` + `global.json` + `obj/` + `output.json`, and the `Program.cs`
  delete on the copy-out path disappears. Cache-hit behavior (instant open) is unchanged.
- **The design-time restore applies to scripts too**, because script compilation is unified
  with the MSBuild path (§4.5). Restore no longer produces `bin` for anyone; the `RestorePath/bin`
  copy and the `program.deps.json`/`program.runtimeconfig.json` rename dance disappear along
  with the stub source.
- **Compile** (`CompileWithMsbuild`): raise `-v:q` → `-v:m` (minimal: high-priority messages,
  project header, error/warning lines, summary — the vocabulary VSColorOutput's default patterns
  are written against). The two `-flp` warning/error file loggers are untouched; diagnostics
  parsing keeps working unchanged.
- Terminal logger is auto-disabled on redirected output; no `-tl:off` needed.

### 4.2 Host output channel: a writer factory, not events

```csharp
public enum BuildOutputSource { Restore, Compile }

// IExecutionHost
Func<BuildOutputSource, bool /*cached*/, TextWriter>? BuildOutputWriterFactory { get; set; }
```

Instead of start/line/completed events, the host asks for a phase-scoped `TextWriter` as a
phase starts producing output: obtaining the writer marks the start (the view model clears the
phase's document and switches the combo), `WriteLine` is the line stream, and `Dispose` is
completion. `DoRestoreAsync` and `CompileWithMsbuild` stream by replacing
`.LastOrDefaultAsync(ct)` with `await foreach` over `GetStandardOutputLinesAsync()`, writing
each line. Existing events (`RestoreStarted/Completed`, `CompilationErrors`) are unchanged.
If `StandardError` is non-empty at exit, its lines are appended to the same writer (the
classifier's error patterns color them naturally).

### 4.3 Restore cache

- On a cache **miss**, the streamed restore lines are also written to `output.log` in the hashed
  restore cache directory (next to the `.restored` marker).
- On a cache **hit** (`MarkerExists`, where no process runs), the host replays `output.log` —
  `BuildOutputStarted(Restore)` with a *cached* flag (either an extra event arg or a distinct
  event), the file's lines, `BuildOutputCompleted`. Missing/unreadable log (pre-feature caches)
  degrades to a single synthetic line: `Restore up to date (cached).` No auto-switch to Restore
  on a cache hit — the interesting output is the compile that follows.

### 4.4 Special paths

- **Script mode (`.csx`)** needs no special handling: with the unified compile (§4.5), scripts
  build through `dotnet build` like everything else and stream genuine MSBuild output.
- `ProcessUtil.GetStandardOutputLinesAsync` used to drop whitespace-only lines; the filter was
  removed outright (the execution protocol reads the raw stream and never used this path), so
  build output keeps its blank lines (MSBuild uses them as section separators).

### 4.5 Companion change: unified script compile (`CoreCompile` override)

Today scripts compile in-process (`CompileInProcess` → `Compiler.CompileAndSaveAssembly`,
`SourceCodeKind.Script`) while everything else goes through `dotnet build` — two compile paths,
two diagnostics channels (`SendDiagnostics` vs. `-flp` log parsing), and a restore phase forced
to produce `bin` for scripts. Unify by keeping MSBuild as the driver for both and swapping the
compiler underneath for scripts:

- The script csproj (RoslynPad authors it) redefines the **`CoreCompile` target** after the SDK
  imports, invoking a custom task — `<RoslynPadScriptCompile Sources="@(Compile)"
  References="@(ReferencePathWithRefAssemblies)" Analyzers="@(Analyzer)"
  OutputAssembly="@(IntermediateAssembly)" DefineConstants="$(DefineConstants)" … />` — from a
  small task assembly shipped with the app that hosts the existing `Compiler` logic (script
  parse options, script entry-point synthesis, pdb/embedded-source settings move there rather
  than being duplicated). The target declares `Inputs`/`Outputs`, so scripts gain incremental
  compilation. The task targets **netstandard2.0**: it loads into the MSBuild node of whatever
  SDK the platform selector pinned via `global.json`, which can be older than the app's runtime.
- Target override was chosen over name-shadowing the `Csc` task (`<UsingTask TaskName="Csc">`
  after the imports, subclassing Roslyn's public `Microsoft.CodeAnalysis.BuildTasks.Csc` to
  inherit its ~60-parameter surface and overriding `Execute()`): both work, but the target
  override has no parameter-surface or SDK-version coupling and is the standard extension point.
- Everything downstream of `CoreCompile` runs stock: reference resolution feeds in,
  `GenerateBuildDependencyFile`/runtimeconfig/copy-local produce a correct `bin` — which is what
  frees restore from producing it (§4.1).
- On .NET MSBuild the task assembly loads in its own `AssemblyLoadContext`, so it can carry its
  own Roslyn without colliding with the build node.
- The SDK doesn't glob `.csx`; the script csproj adds an explicit `<Compile Include>`.
- `CompileInProcess` and the script-only diagnostics channel are deleted; script diagnostics
  arrive via the same `-flp` error/warning logs as regular builds.

**Cost**: script compile goes from an in-process emit (~100–300ms) to an MSBuild invocation —
roughly 300–500ms warm (node reuse + incremental `CoreCompile`), 1–2s cold. Accepted in
exchange for deleting the parallel path; revisit only if quick-iteration script runs feel
noticeably worse.

## 5. View: read-only editor over a custom content type

`BuildOutputView.axaml(.cs)` — modeled on `ILViewer`:

- `CodeEditorView.CreateBuffer(mainViewModel, "", BuildOutputClassificationDefinitions.ContentType)`
  then `CreateView(isReadOnly: true, setFocus: false)`. View creation is deferred until the
  Roslyn host is initialized (same lazy pattern as `ILViewer`).
- **One view, one buffer, swapped content.** `IWpfTextView` cannot swap buffers, and two live
  editor views for one pane is waste. Each source's text lives in the view model; on source
  change (combo or auto-switch) or `DataContext` change (active document switch), the buffer is
  `Replace`d wholesale with the selected source's snapshot. While streaming, lines whose source
  matches the displayed one are appended with `buffer.Insert(snapshot.Length, …)` —
  `ViewProhibitUserInputId` blocks user edits only, not programmatic ones.
- Appends are coalesced: `Changed` wake-ups collapse into a single dispatcher post that pulls
  the accumulated new text (§8) into one buffer edit. No per-line dispatcher hops.
- Tail tracking: after an append, if the view was at the bottom before the edit,
  `ViewScroller.EnsureSpanVisible` on the last line. (Viewport height is 0 before first arrange —
  same gotcha `NavigateToSpan` handles.)

## 6. Classification

New file `src/RoslynPad/Editor/BuildOutputClassification.cs`, structured like
`ILClassification.cs`:

- `ContentTypeDefinition` — `[Name("BuildOutput")] [BaseDefinition("text")]`. MEF discovery is
  automatic (the app assembly is in `MainViewModel.CompositionAssemblies`).
- `[Export(typeof(IClassifierProvider))] [ContentType("BuildOutput")]` returning a per-buffer
  singleton classifier.
- Classifier: per line, first-matching regex wins; the whole line gets one classification span;
  fallback is `BuildText`. Patterns are compiled once, case-insensitive.

### 6.1 Classification types (from VSColorOutput's `ClassificationTypeDefinitions`)

Adopted verbatim by name: `BuildHead`, `BuildText`, `LogError`, `LogWarn`, `LogInfo`,
`LogCustom1`–`LogCustom4`. Each gets a `ClassificationTypeDefinition` export
(`[BaseDefinition("text")]`).

Deliberately **not** adopted: `FindResultsSearchTerm`, `FindResultsFilename`, `TimeStamp` —
they classify VS's Find Results window and debug-output timestamps, surfaces that don't exist
here. Easy to add later if a consumer appears.

### 6.2 Default patterns (VSColorOutput defaults, first match wins)

| Pattern | Type |
|---|---|
| `\+\+\+\>` | LogCustom1 |
| `[t\|c]sc\.exe` | BuildText |
| `(=====\|-----\|Projects build report\|Status\|Project\|Path)` | BuildHead |
| `0 error.+0 warning` | BuildHead |
| `^(\d+>)?\s*0 error\(s\)\s*$` | BuildHead |
| `^(\d+>)?\s*0 warning\(s\)\s*$` | BuildHead |
| `0 failed\|Succeeded` | BuildHead |
| `(\W\|^)^(?!.*warning\s(BC\|CS\|CA)\d+:).*((?<!/)error\|fail\|crit\|failed\|exception)[^\w\.\-\+]` | LogError |
| `(exception:\|stack trace:)` | LogError |
| `^\s+at\s` | LogError |
| `(\W\|^)(warning\|warn)\W` | LogWarn |
| `(\W\|^)(information\|info)\W` | LogInfo |
| `Could not find file` | LogError |
| `failed` | LogError |

Patterns are code data for now (no user-editable settings — add persistence later if asked).
Exact strings to be lifted from `VSColorOutput/State/Settings.cs` `DefaultPatterns()` at
implementation time.

## 7. Theming (light **and dark**)

Because `ThemeClassificationFormats.Apply` clears formats the theme doesn't style, the build
output types are themed the same way inline diagnostics are: a new
`ThemeClassificationFormats.ApplyBuildOutput(formatMap, registry)` sets explicit foregrounds
from the **current VS Code theme's own colors**, called from `CodeEditorView.ApplyTheme()` so
it runs on view creation and every theme change. No invented hex values; each type resolves
through a fallback chain of theme keys, and if no key resolves, the type is left unstyled
(inherits the editor foreground):

| Type | Theme keys (first hit wins) |
|---|---|
| BuildHead | `terminal.ansiGreen` → `gitDecoration.addedResourceForeground` |
| BuildText | *(none — inherits default foreground)* |
| LogError | `editorError.foreground` → `terminal.ansiRed` |
| LogWarn | `editorWarning.foreground` → `terminal.ansiYellow` |
| LogInfo | `editorInfo.foreground` → `terminal.ansiBlue` |
| LogCustom1 | `terminal.ansiCyan` |
| LogCustom2 | `terminal.ansiMagenta` |
| LogCustom3 | `terminal.ansiBrightMagenta` |
| LogCustom4 | `terminal.ansiBrightYellow` |

This is what makes dark themes correct for free: the keys come from the active theme
(`2026-dark` included), not from VSColorOutput's WPF light-theme constants (Red/Olive/DarkBlue…),
which are unreadable or wrong on dark backgrounds. Verify the chosen keys exist in the built-in
`2026-light`/`2026-dark` themes during implementation and adjust the chains against what those
themes actually define.

Each type additionally needs a **colorless, registration-only** `ClassificationFormatDefinition`
export (in `BuildOutputClassification.cs`): Morgania's classification format map builds its
key table from the exported definitions and only reads explicit text properties for registered
types — without the exports, the colors `ApplyBuildOutput` sets are silently never read back.
The definitions must stay colorless; any static color would be dead weight anyway (cleared on
first theme application — the theme is the single source of colors).

## 8. View model

`BuildOutputViewModel` (`src/RoslynPad/UI/ViewModels/BuildOutputViewModel.cs`), owned by
`OpenDocumentViewModel`, which assigns `executionHost.BuildOutputWriterFactory =
BuildOutput.CreateWriter`; output survives document tab switches because it lives on the
document VM:

- Two `BuildOutputDocument`s (Restore, Compile): an append-only `StringBuilder` behind a lock,
  a generation counter bumped on reset, and an `IsCached` flag (drives the `Restore (cached)`
  combo label via `DisplayName`).
- `CreateWriter(source, cached)` resets the phase's document, marshals the cached flag and
  auto-switch onto the UI thread, and returns a `TextWriter` that appends into the document.
- Consumption is **pull-based**: the document raises a payload-free `Changed` (possibly on a
  background thread); the view coalesces wake-ups and calls `ReadFrom(position)`, which
  returns only the text appended since the last pull — or the full text with `Restarted` set
  when the generation moved — so appends are incremental with no cross-thread races.
- The view binds `DataContext` to `CurrentOpenDocument` (same as `ResultsView`) and re-renders
  on `DataContext`/`SelectedDocument` change.

## 9. Dock integration

- `MainWindow.axaml`: `ResultPane` is a `DocumentDock` containing `Document`s `Results`,
  `Build` (hosting `BuildOutputView`), and `IL`.
- `MainWindow.axaml.cs` `LoadDockLayout()`: the guard requires the new types/ids
  (`DocumentDock` ResultPane, `Document` Results/Build/IL). Saved layouts from before this
  feature contain a `ToolDock`/`Tool`s, fail the casts, and fall back to the default XAML
  layout — a one-time layout reset instead of per-pane migration.
- `DockLayoutSerializer` needs no change (allow-lists are per-type; `Document` is already
  covered).

## 10. Implementation order

1. **Unified script compile** (§4.5): task assembly + `CoreCompile` override, delete
   `CompileInProcess`. Independent of the pane; unblocks the design-time restore for scripts.
2. **Plumbing**: command-line changes (design-time restore for both modes, stub-source and
   `bin`-copy removal), `-getResultOutputFile` JSON relocation, streaming events, restore-cache
   `output.log`, `ProcessUtil` whitespace-filter move. Verifiable headless (run a build, assert
   event stream).
3. **View + classification + theming**: content type, classifier, `ApplyBuildOutput`,
   `BuildOutputView`/`BuildOutputViewModel`, combo + auto-switch.
4. **Dock + polish**: pane registration, layout-migration guard, tail auto-scroll,
   `Restore (cached)` replay.

## 11. Resolved risks / notes

- **`-getResultOutputFile`** — verified working (MSBuild 18.6); the JSON lands in
  `output.json` with the same `Items` shape the parser already read.
- **Design-time restore** — verified: `dotnet msbuild -restore -interactive -t:Compile
  -p:DesignTimeBuild=true -p:SkipCompilerExecution=true -getItem:…` yields identical
  `ReferencePathWithRefAssemblies`/`Analyzer` items (`-getTargetResult` was dropped — exit code
  + the errors file logger carry success/failure), stdout is a clean streamable log, and no
  outputs are produced.
- **Unified script compile** — verified end-to-end (restore → task compile → execute → Dump)
  in both script and regular modes, including the cached-restore replay. Two bugs found and
  fixed on the way: the trailing-expression Dump rewrite needed a parenthesized receiver
  (the tree round-trips through text now — `1 + 2` re-parsed as `1 + 2.Dump()`), and
  `MsbuildLogRegex` needed to accept the four-number spans (`(line,col,endLine,endCol)`)
  `ScriptCompileTask` diagnostics log with.
- **Combo semantics on cache hit**: Restore stays the default at document open, shows replayed
  cached output, and only auto-switches to Compile when compilation starts.
- Theme key coverage varies across third-party VS Code themes; unresolved keys degrade to the
  default foreground, never to a hardcoded color (the ANSI palette has registry defaults
  mirroring VS Code's, so the common keys always resolve).
