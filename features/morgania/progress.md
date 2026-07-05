# Progress

## M0 — Cores build, convert, and probe

**State: substantially complete** (gap probe against Roslyn `EditorFeatures` still open).

- **Vendored** the MIT `microsoft/vs-editor-api` editor core into `vendor/vs-editor-api`
  (32 projects: defs, utils, impls incl. the async completion/quick info brokers,
  CodeLens and Peek defs kept per owner direction). Excluded: Cocoa hosts, FPF (the
  Mac WPF shim), samples, upstream build infra.
- **Retargeted** to net10.0 with new SDK csprojs, central package management, and a
  vendor-level `Directory.Build.props` (relaxed analyzers, upstream NoWarn baseline,
  `System.Composition.AttributedModel` for all).
- **Retyped WPF → Avalonia** per PLAN §4.2 (~40 files): geometry/media/input usings,
  `Brush`→`IBrush`, `UIElement`/`FrameworkElement`→`Control`, `ImageSource`→`IImage`,
  `DependencyObject`→`AvaloniaObject`, `ModifierKeys`→`KeyModifiers`, Dispatcher
  (`BeginInvoke`→`Post`, `CurrentDispatcher`→`UIThread`), `ResourceDictionary` from
  Avalonia.Controls. Documented divergences: `TextEffects` member omitted, brush
  Freeze/Clone dropped (immutability), marker union geometry via `GeometryGroup`,
  weak-event collection mirror uses direct subscription, WPF-shaped clipboard shim in
  EditorOperations with a host-provider seam (`ClipboardShim.cs`), `WpfHelper.cs`
  excluded from compile (Win32 popup glue, no consumers). Legacy `ImageMoniker`
  recreated from public docs (`Core/Def/Imaging/ImageMoniker.cs`).
- **MEF v1 → System.Composition** conversion done and verified (rules + gotchas in
  ADR-003).
- **Composition tests** (`tests/Morgania.CompositionTests`, MSTest.Sdk): container
  composition, export walk over every contract, singleton reference identity,
  functional content-type registry probe — all green. The test host's
  `ViewLayerStubs` doubles as the list of services Morgania.Editor must export.
- **License audit** checked in at `docs/license-audit.md` — closure is entirely MIT; no
  proprietary VS packages anywhere.

### Remaining for M0
- Rough Roslyn `EditorFeatures` gap probe (PLAN §5.4): needs a Roslyn checkout or its
  MIT NuGet binaries to diff referenced editor APIs against the vendored surface.
- CI wiring: build + tests + license-closure check on push (repo has no workflow yet).

# Dependency license audit (M0)

**Scope:** every `PackageReference` reachable (directly or transitively) from the vendored
`vendor/vs-editor-api` projects and `tests/`, enumerated from `project.assets.json` after a
full restore. PLAN §3.3 rule 2 requires every package in this closure to be MIT/Apache and
forbids referencing the proprietary `Microsoft.VisualStudio.*` binaries.

**Result: pass.** No proprietary Visual Studio package is referenced by any project.
The upstream repo's proprietary references (`Microsoft.VisualStudio.Imaging`,
`.ImageCatalog`, `.Imaging.Interop.14.0.DesignTime`, `.Utilities`) were confined to the
excluded Cocoa/VSIX projects or were vestigial; the one type actually used from them
(`ImageMoniker`) was recreated from public documentation as original code
(`Core/Def/Imaging/ImageMoniker.cs`).

| Package | Version | License | Direct / transitive |
|---|---|---|---|
| Microsoft.VisualStudio.Threading (+ .Analyzers, .Only) | 17.14.15 | MIT | direct |
| Microsoft.VisualStudio.Validation | 17.13.22 | MIT | direct |
| System.Composition (+ AttributedModel, Convention, Hosting, Runtime, TypedParts) | 10.0.9 | MIT | direct |
| StreamJsonRpc | 2.25.28 | MIT | direct (CodeLens remoting) |
| Newtonsoft.Json | 13.0.4 | MIT | direct (CodeLens remoting) |
| MessagePack (+ Annotations) | 2.5.302 | MIT | transitive (StreamJsonRpc); pinned ≥2.5.301 for GHSA-hv8m-jj95-wg3x |
| Nerdbank.Streams / Nerdbank.MessagePack | 2.13.16 / 1.2.4 | MIT | transitive (StreamJsonRpc) |
| PolyType | 1.3.1 | MIT | transitive |
| Avalonia (+ Remote.Protocol, BuildServices¹, MicroCom.Runtime) | 12.0.4 | MIT | direct (retyped UI defs) |
| Microsoft.ApplicationInsights | 2.23.0 | MIT | transitive (Threading) |
| MSTest.* / Microsoft.Testing.* / Microsoft.TestPlatform.ObjectModel / Microsoft.DiaSymReader / Microsoft.Extensions.DependencyModel / Microsoft.NET.StringTools | various | MIT | tests only |

¹ `Avalonia.BuildServices` is a build-time-only telemetry task (nothing ships from it);
builds run with `AVALONIA_TELEMETRY_OPTOUT=1`.

**Method:** union of `libraries` of type `package` across all `obj/project.assets.json`
files, plus a grep of every `.csproj` for the known-proprietary package ids. Licenses per
nuget.org metadata. Re-run after any dependency change; a CI check should regenerate and
diff this closure (tracked as M0 follow-up).

### Notes carried out of M0
- Legacy/obsolete IntelliSense surfaces are non-goals (PLAN §2): no implementations or
  presenters for `IQuickInfoSource`/`ILegacyQuickInfoSource`/legacy completion — modern
  `IAsyncQuickInfoSource`/`IAsyncCompletionSource` only.
- Host must export: `JoinableTaskContext`, `ILoggingServiceInternal`, `IToolTipService`,
  `ISmartIndentationService` (as of M1, `IEditorFormatMapService` is real); wire a real
  clipboard provider via `Morgania.Text.Operations.Implementation.Clipboard.SetProvider`.
- Builds require `AVALONIA_TELEMETRY_OPTOUT=1` (Avalonia.BuildServices crashes in
  sandboxed/offline builds) and `-m:1` under the local sandbox (msbuild node IPC).

## M1 — Read-only view

**State: complete** (acceptance: geometry tests on LTR/RTL/mixed fixtures and the
100k-line scroll budget are green; CI wiring itself is still the open M0 item).

- **Definition gap-fill** (`vendor/.../Text/Def/TextUIWpf`, Morgania-authored per PLAN
  §3.3/§5.4 — recreated from learn.microsoft.com, retyped to Avalonia per §4.2):
  `IWpfTextView`, `IWpfTextViewLine`, `IWpfTextViewLineCollection`, `IFormattedLine(Source)`,
  `TextFormattingRunProperties` (immutable, tri-state "empty" properties, derives from
  Avalonia `TextRunProperties` so it feeds the formatter directly),
  `IClassificationFormatMap(Service)`, `ITextEditorFactoryService`, `IWpfTextViewHost`,
  `IWpfTextViewMargin`, `IAdornmentLayer(Element)`, positioning/callback types.
- **`src/Morgania.Editor`** (assembly `Morgania.UI.Text.Wpf.View.Implementation`, matching
  the vendored IVT grants):
  - `EditorFormatMapService`/`ClassificationFormatMapService` over the composed
    `EditorFormatDefinition` exports (per-category maps, `Orderer`-based priority order,
    base-type merge with nearest-definition-wins per property).
  - `FormattedLineSource` on Avalonia `TextFormatter`: classified `ITextSource` per
    snapshot line, dense formatting runs, word wrap (one `IFormattedLine` per visual row),
    tab metrics, nominal line metrics from a reference format.
  - `FormattedLine`: the full `ITextViewLine` geometry contract over an Avalonia
    `TextLine` — character bounds (bidi leading/trailing), x↔position (containment,
    insertion, virtual), normalized span bounds, end-of-line box, surrogate-pair text
    elements, visibility state, per-line visual (draws via `TextLine.Draw`).
  - `WpfTextView` (an Avalonia `Panel`): viewport-only layout engine (anchor + fill up/down
    + gap-prevention clamps at buffer edges), `LayoutChanged`/viewport events, horizontal
    scrolling via `ViewportLeft`, wheel scrolling, adornment layers (ordered by
    `AdornmentLayerDefinition` exports), relayout on buffer/classification/format-map/option
    changes; `TextEditorFactoryService`, `ViewScroller`, role sets, a margin-less
    `IWpfTextViewHost`.
- **`src/Morgania.Demo`**: runnable Avalonia editor — dark theme, regex highlighter over
  the standard classification-type names (which the demo itself defines: see gaps),
  mixed-direction sample content, wheel scrolling. Pipeline verified headlessly
  (composition, classified formatting on rendered lines, page scrolling).
- **`tests/Morgania.GeometryTests`** (Avalonia.Headless + Skia, real shaping): x↔position
  round-trips on LTR/RTL/mixed fixtures, RTL run direction, wrap rows dense/disjoint and
  partitioning the line, dense viewport-filling layout, scroll clamping, empty/break-line
  geometry, surrogate pairs, and the perf smoke (100k-line first layout, per-frame scroll,
  random access) — 10 tests, all green (perf asserted at 2x budget for CI variance).

### M1 known divergences / deferred items
- **Tabs are incremental, not tab stops**: Avalonia's formatter advances a tab by a fixed
  `DefaultIncrementalTab` from the current x rather than to the next column stop (VS
  semantics). Pinned by a test as a documented divergence; fix belongs to the formatter
  seam (M5 conformance).
- **Golden files**: geometry acceptance is currently *invariant-based*; serialized goldens
  need an embedded test font to be machine-independent — add with CI.
- **`ViewportTop` is always 0.0**: scrolling re-anchors line tops each layout instead of
  translating the viewport. Contract-visible only through coordinate values; revisit with
  the line cache (below).
- **No formatted-line cache yet** (PLAN §5.5 keyed cache): every layout reformats the
  viewport. Perf budgets pass regardless; cache lands with M5 perf work.
- **Caret/selection are M1-minimal** (position tracking only, no rendering/blink/gestures
  — M2). `GetSpaceReservationManager` throws (M6). `LineTransformSource` is null (M5).
  `MouseHover` is never raised (M2). `ITextView2` not yet implemented (M2, with the
  multi-selection broker).
- **`IStandardClassificationService` has no vendored implementation** (VS platform code,
  never open-sourced): the standard classification *type definitions* ("keyword" etc.)
  don't exist unless a language package exports them (the demo does its own). Roslyn
  EditorFeatures consumes this service — recreate it as a Morgania-authored part when the
  Roslyn tier lands (feeds the §5.4 gap probe).
- **System.Composition reminder** (bit twice this milestone): assembly scanning silently
  skips non-public parts — every `[Export]` part in app/demo assemblies must be `public`
  (`WithPart<T>()` is the only way to register internals).

## M2 — Editing, caret, selection

**State: substantially complete** (scripted behavior tests green; IME needs manual
per-OS verification, and the mouse/keyboard dispatch is an interim path — see below).

- **`ITextView2`**: the view now exposes `MultiSelectionBroker` (created by the vendored
  `MultiSelectionBrokerFactory`), `QueuePostLayoutAction`, `TryGet*`, `InOuterLayout`,
  `MaxTextRightCoordinateChanged`. Edits relayout *synchronously* so the published line
  collection never lags the buffer.
- **Caret/selection as broker shims** (the modern VS design): `ITextCaret`/`ITextSelection`
  adapt the broker's primary selection; legacy `Caret.MoveTo` is selection-preserving (the
  documented pairing with `Selection.Select`), empty selections travel with the caret.
- **Rendering**: `SelectionLayer` (below text; all selections via normalized bidi bounds,
  active/inactive brushes) and `CaretLayer` (above text; blinking primary caret, dimmed
  secondary carets, IME preedit drawn with underline at the caret).
- **Editing input**: typing via `TextInput` → `IEditorOperations.InsertText`; an interim
  keymap dispatches arrows/word-ops/Home/End/PageUp/Dn/Backspace/Delete/Enter/Tab,
  Ctrl(Cmd)+A/C/X/V/Z/Y (undo via the buffer undo history the factory attaches), Escape;
  click/shift-click/drag and alt/cmd-click (adds a caret) — replaced by Modern Commanding
  chains + the IMouseProcessor contract in M3+ (PLAN §5.6).
- **Word structure**: Morgania-authored `ITextStructureNavigatorProvider` for "text" with
  VS word semantics (identifier/punctuation/whitespace runs; enclosing word → line →
  document) — the VS natural-language navigator was never open-sourced and the vendored
  `DefaultTextNavigator` is a degenerate per-character fallback.
- **Clipboard**: `AvaloniaClipboardBridge` installs into the M0 `ClipboardShim` seam
  (IVT added to the vendored EditorOperations impl): in-process store is authoritative;
  copied text is also pushed to the OS clipboard (async). Reading the OS clipboard for
  external paste needs an async command path — deferred.
- **IME**: `EditorTextInputMethodClient` (surrounding text, cursor rect, preedit rendered
  as provisional text in the caret layer, commit through the normal text-input path).
  Headless tests can't drive platform IMEs: round-trip of the
  M2 acceptance needs manual verification in the demo on each OS.
- **`tests/Morgania.BehaviorTests`** (13 tests, scripted from documented VS semantics):
  typing/backspace/delete around the caret, word movement, shift-extension and collapse,
  line up/down preferred column, select-all-replace, undo/redo, **multi-caret typing**,
  logical-order editing with RTL caret bounds, cut/paste, `EnsureVisible`
  scrolling, newline split, selection bounds on view lines.

### M2 known divergences / deferred items
- Key/mouse handling is direct-dispatch to `IEditorOperations`, not the Modern Commanding
  chain (`IEditorCommandHandlerService`) or `IMouseProcessor` chain yet; box selection has
  broker support but no drag gesture. Overwrite mode, drag-drop, and double-click
  word-select are not wired.
- External-app paste (async OS clipboard read) not wired; copy *to* the OS clipboard works.
- `ITextCaret` preferred-x capture (`captureHorizontalPosition`) is approximated through
  the broker's presentation properties.

## M3 — Adornments and tagging

**State: substantially complete** (acceptance met: the demo adds an intra-text color
swatch and brace-highlight markers through contract APIs only; divergences below).

- **Def gap-fill**: `IntraTextAdornmentTag` (Control-typed, derives from the vendored
  cross-platform tag so shared machinery consumes both) and
  `IWpfTextViewCreationListener`, recreated per PLAN §3.3/§5.4.
- **`TextAndAdornmentSequencerFactoryService`** (Morgania-authored; the VS sequencer was
  never open-sourced): sequences a line into text and adornment elements from the view's
  `SpaceNegotiatingAdornmentTag` aggregation; `SequenceChanged` invalidates the line source.
- **Space negotiation in the formatter** (PLAN §5.5 "designed in at M2/M3"): adornment
  elements become `EmbeddedAdornmentRun`s (Avalonia `DrawableTextRun`) consuming their
  replaced span, so the formatter reserves their width/height; `FormattedLine` answers
  `GetAdornmentBounds`/`GetAdornmentTags`/`GetExtendedCharacterBounds`, and
  `DefaultLineTransform` carries the adornments' top/bottom space (applied per row).
- **`IntraTextAdornmentSupport`** (Morgania-authored): bridges intra-text tags to
  space-negotiating tags (sizes from the measured control or explicit metrics) and
  positions the adornment controls over the reserved space after every layout.
- **Adornment layers**: standard `AdornmentLayerDefinition` exports (Selection,
  CurrentLineHighlighter, TextMarker, Text, Squiggle, Intra Text Adornment, Caret, …);
  definition order decides stacking, with before-"Text" layers rendering under the text;
  undeclared layer names throw per contract.
- **Creation listeners**: `IWpfTextViewCreationListener` exports are invoked at view
  creation, filtered by `[ContentType]`/`[TextViewRole]` metadata (new concrete
  `ContentTypeAndTextViewRoleMetadata` view).
- **Demo (acceptance)**: `ColorSwatchTaggerProvider` replaces `#RRGGBB` literals with a
  swatch (space negotiated by the editor), `BraceHighlightListener` marks matching braces
  on the TextMarker layer via `GetTextMarkerGeometry` — both against contract APIs only.
- **Tests**: intra-text space negotiation (line width, adornment bounds/tags, shifted
  following text, extended character bounds) and layer ordering/add/remove/contract
  exceptions — 15 behavior tests green; 29 green overall.

### M3 known divergences / deferred items
- **Zero-length (purely inserted) intra-text adornments are not negotiated** — only
  adornments replacing a non-empty span. Zero-length support needs sequence-coordinate
  formatting (buffer↔x through the element stream), planned with the M5 formatter seam.
- Adornment positioning refresh for `TextRelative` elements tracks the span's leading
  edge only (full VS positioning matrix — vertical-only variants — with M5).
- **Correction to the M1 composition note**: `WithAssembly` scanning *does* discover
  internal parts of the scanned assembly itself (the M2/M3 test hosts rely on it); the
  demo's earlier failure was the missing standard classification-type definitions, not
  part visibility. Cross-assembly consumption of non-public contracts is still subject to
  accessibility (ADR-003 rule 2's cascades).

## M4 — Margins and host

**State: substantially complete** (acceptance met: margins are MEF-discovered, ordered,
and removable; the demo is a recognizable editor with line numbers and scrollbars).

- **Def gap-fill**: `IWpfTextViewMarginProvider` (per PLAN §3.3/§5.4) with a concrete
  `MarginProviderMetadata` view ([Name]/[MarginContainer]/[Order]/[ContentType]/[TextViewRole]).
- **`WpfTextViewHost`** (real implementation replacing the M1 stub): the view surrounded by
  the four margin containers; each container's child margins are discovered per
  `[MarginContainer]`, filtered by content type/roles, stacked in `[Order]` definition
  order; `GetTextViewMargin` resolves recursively, unknown names return null.
- **Standard margins** (Morgania-authored providers):
  - Line numbers (right-aligned per snapshot line, format-map typeface, option-driven via
    `DefaultTextViewHostOptions.LineNumberMarginId` — vendored default is off, hosts opt in).
  - Vertical scrollbar implementing `IVerticalScrollBar` over a line-linear `IScrollMap`
    (elisions expanded; the outlining-aware map lands with projection support in M5);
    two-way sync with the view's layout.
  - Horizontal scrollbar (ViewportLeft ↔ thumb; hidden under word wrap).
  - Glyph margin strip (discoverable and ordered; glyph factory providers attach in a
    later milestone).
- **Render-pass invariant** (regression from the field: `InvalidOperationException:
  Visual was invalidated during the render pass` on the demo's first frame): every
  `Render` override — and anything it calls — must be strictly read-only. Concretely:
  read lines via `ITextView2.TryGetTextViewLines` (never `TextViewLines`, which forces a
  layout → `InvalidateVisual` inside the compositor), and never create the selection
  broker from a render path (`WpfTextView.ExistingBroker` is the non-creating accessor;
  the broker's layer-invalidation subscriptions live in the `MultiSelectionBroker`
  getter, not in the layers' `Render`). Applied to the selection/caret layers, the
  line-number margin, and the vertical scrollbar sync.
- **Drawable runs need run properties**: `EmbeddedAdornmentRun` (M3) now carries the
  surrounding text's `TextRunProperties` — Avalonia's `TextLineImpl.GetBaselineOffset`
  throws `ArgumentOutOfRangeException` on a null-`Properties` drawable run in the draw
  pass, which the layout/geometry suites never reach (only a real frame render does).
- **Tests**: discovery/ordering (Glyph before LineNumber in the Left container),
  option-driven removal, contract nulls, scroll-map round-trips, and rendering tests
  that drive Avalonia's *real* compositor pass headlessly (first frame before any text
  layout is published — mutation-verified to reproduce the field crash — plus a full
  frame with margins, adornments, carets, and selections) — 33 tests green overall.

### M4 known divergences / deferred items
- Glyph factories (`IGlyphFactoryProvider`/`IGlyphTag`) not yet wired; the margin strip is
  present and discoverable.
- The scroll map is line-linear (no outlining/elision compression) until projection
  support (M5); `IVerticalScrollBar` coordinates derive from it. *(Closed in M5.)*
- Margin visual theming is minimal (single brush line numbers, default ScrollBar theme).

## M5 — Conformance and hardening

**State: complete** (zoom, line transforms, tab stops, formatted-line cache,
elision/outlining via projection, and the extension-recompile conformance suite).

- **Line transform sources**: `ILineTransformSourceProvider` exports are discovered per
  view (content type/role scoped), aggregated with `LineTransform.Combine` on top of each
  line's default (adornment-driven) transform, and applied at row placement, where the
  y-position and placement direction (`Top` filling downward, `Bottom` filling upward)
  are known. `IWpfTextView.LineTransformSource` returns the aggregate.
- **Zoom** (resolves PLAN open question #3 as *transform*, not font scaling): a render
  transform over the view — geometry answers stay in logical text-rendering coordinates
  and no reformat happens on zoom. The `ZoomLevel` option is the source of truth (the
  property routes through it), clamped to `MinZoomLevel`/`MaxZoomLevel`, gated on the
  `Zoomable` role; Ctrl(Cmd)+wheel steps by `ZoomConstants.ScalingFactor` when
  `EnableMouseWheelZoom` is set. The viewport is the arranged size in logical units, the
  line-number margin draws at the zoomed size, and the host clips the scaled view to its
  cell. Found along the way: a background-less `Panel` is invisible to Avalonia
  hit-testing, so the view now sets a real transparent background (pointer and wheel
  input over unstyled views used to fall through).
- **VS tab stops** on top of Avalonia's fixed-increment formatter: each tab outside an
  adornment span becomes a drawable run reaching the next TabSize × ColumnWidth boundary
  (documented divergences: tabs inside reordered bidi runs, and wrapped rows compute
  stops from the paragraph start).
- **Formatted-line cache** (PLAN §5.5): rows of the published collection are reusable
  while the line source is unchanged (the source is a pure function of snapshot × format
  map × options × wrap width, so identity is the cache key); steady-state scrolling
  *translates* lines (`TextViewLineChange.Translated`) instead of reformatting them.
- **Elision/outlining via projection**:
  - `ITextViewModelProvider` exports are discovered at view creation (content type/role
    scoped) — the contract seam through which projection view models arrive.
    `ElisionTextViewModelProvider` gives every `Structured` view an elision buffer as its
    visual buffer (one-to-one until something collapses).
  - `FormattedLine` maps each visual row down to (possibly disjoint) edit-buffer segments
    through the buffer graph; identity view models keep the plain-arithmetic fast path.
    Extents cover the hidden text (a row rendering a collapsed region is *longer* than
    its visible text), hidden positions render at the collapse point, and
    `GetTextElementSpan` answers a hidden region — including elided text between the last
    visible character and the mapped line break — as one element, so the caret jumps
    collapsed regions as a unit.
  - The layout iterates visual-buffer lines (the anchor maps through
    `GetNearestPointInVisualSnapshot`); the sequencer maps top spans down per visible
    segment; classification runs per visible edit segment and lands at visual offsets.
  - `OutliningElisionSupport` (a creation listener) connects the vendored outlining
    manager to the view's elision buffer: `RegionsCollapsed` → `ElideSpans`,
    `RegionsExpanded` → `ExpandSpans`. The manager tracks regions; the view layer owns
    the projection.
  - The vertical scrollbar's scroll map is visual-line based (`AreElisionsExpanded` is
    false): collapsed regions shrink the scrollable range (closes the M4 deferral).
- **Demo** (a capability tour): brace-block outlining regions with Ctrl(Cmd)+M
  fold-at-caret, Ctrl+wheel zoom with a live readout in the title, Alt+Z word wrap,
  Alt+click multi-caret, color swatches, brace markers, bidi text, real tabs, line
  numbers and scrollbars. Post-M5 enrichment covers the remaining seams: a demo
  `ILineTransformSourceProvider` pads the block-separator lines (line transforms made
  visible), a highlight-word listener marks every occurrence of the word at the caret on
  its own adornment layer (`ITextStructureNavigator` + `ITextSearchService`), and a
  bottom status margin (`IWpfTextViewMarginProvider`) shows live Ln/Col/selection/caret
  count/zoom plus the gesture cheat sheet. The headless smoke drives the full pipeline:
  compose, render frames, verify line-transform spacing and highlight adornments and the
  margin, scroll ten pages, collapse/expand a region, zoom — all through real compositor
  passes.
- **Field fix (caret events)**: `MultiSelectionBroker` construction forces the view's
  initial layout (the selection transformer captures its preferred x-coordinate from the
  view lines); a `LayoutChanged` handler reading `Caret.Position` re-entered the lazy
  broker getter, and the outer creation then overwrote the reentrantly created broker —
  leaving the caret/selection shims subscribed to a discarded instance, so
  `Caret.PositionChanged` never fired. The getter now keeps the reentrant instance
  (VS's `GetOrCreateSingletonProperty` semantics). Mutation-verified regression:
  `CaretPositionChangedFiresWhenALayoutHandlerReadsTheCaretDuringBrokerCreation`.
- **Field fix (zoom-out clipping)**: below 100% the logical viewport is *larger* than the
  arranged slot, and everything sized or clipped to the slot in logical (pre-zoom)
  coordinates shrank with the render transform — the view's own `ClipToBounds`, the
  slot-arranged layer children, and the panel background — leaving the bottom of the
  window unpainted while the unzoomed line-number margin ran on. Now the view never
  clips itself (the host's decorator clips in screen coordinates), the layers arrange to
  the logical viewport (slot ÷ scale, which the transform maps exactly onto the slot),
  and the background is a viewport-sized layer child instead of the panel's `Background`
  (which only paints `Bounds`). Mutation-verified regression (pixel-samples a rendered
  frame at 50% zoom): `ZoomingOutFillsTheEntireViewSlot`.
- **Field fix (endless scroll past the end)**: two halves. The fill-downward loop
  compared positions against the snapshot length, so a buffer ending in a line break
  never got its final empty line formatted — with the end of the buffer never in the
  layout, no clamp could engage and the view scrolled endlessly. The end check is now
  flag-based (`EndsAtEndOfVisualBuffer` + last row). And the repositioning rule itself
  was pin-to-bottom (no over-scroll at all, contra VS); it now clamps with the last
  line's top at the viewport top — the VS over-scroll limit: the last line stays
  visible, blank space below is allowed. Mutation-verified against both halves:
  `ScrollingPastTheEndClampsWithTheLastLineAtTheTop`.
- **Conformance suite** (the M5 acceptance): `tests/Morgania.ExtensionConformance` is a
  representative WPF editor extension — the canonical text-adornment, view-tagger, and
  margin sample shapes — that references the contract (Def) projects *only*; no Morgania
  implementation assembly appears in its graph. Its compiling at all is half the
  assertion; `ExtensionConformanceTests` composes it into the real editor and drives it
  (its exported adornment layer populates on layout, its margin resolves in the host's
  Bottom container, its tags flow through the view aggregator). The documented mechanical
  diff from real-VS source: `Microsoft.VisualStudio.*`→`Morgania.*` namespaces, WPF
  visuals→Avalonia (§4.2), MEF v1→v2 attributes (field exports become properties).
- **Tests**: 43 green across the three suites. The elision view model is active for
  *every* Structured view, so the entire pre-M5 corpus now runs through the visual↔edit
  mapping — the identity fast path is exercised by everything, the mapping path by the
  outlining tests (joined lines, hidden-position geometry, element-span jumps,
  collapse/expand round-trip).

## M6 — IntelliSense presenters

**State: complete** (acceptance met: a scripted fake language service drives
completion/Quick Info/signature help through the real brokers headlessly —
`tests/Morgania.IntellisenseTests`, 7 tests).

- **Space reservation** (`src/Morgania.Editor/Editor/SpaceReservation.cs`, closes the M1
  `GetSpaceReservationManager` deferral): named managers from
  `SpaceReservationManagerDefinition` exports ([Name]/[Order], imported by the factory like
  adornment layers; unknown names throw per contract), a per-view stack refreshed
  asynchronously after layout/scroll/zoom (`QueueSpaceReservationStackRefresh`), and a
  default popup agent that hosts content in the top-level's `OverlayLayer` — outside the
  view's render transform, so popups never scale with zoom; the anchor rect maps through
  `TranslatePoint`, so they land on the (possibly zoomed) text. Placement per
  `PopupStyles` (below/above, left/right, justify, closest, dismiss-on-mouse-leave), each
  agent avoiding the geometry earlier managers reserved; `IsMouseOverViewOrAdornments` and
  `HasAggregateFocus` aggregate over the stack. The "completion"/"signaturehelp"/
  "quickinfo" definitions are exported by Morgania.Intellisense in that order.
- **MouseHover** (closes the M1/M2 deferral): handlers declare their delay via
  `[MouseHover(delay)]` (default 150ms); when the pointer rests, handlers fire in delay
  order off a dispatcher timer, movement restarts the cycle — this is what lets the
  vendored `QuickInfoController` trigger hover Quick Info.
- **`src/Morgania.Intellisense`** (assembly `Morgania.UI.Language.Intellisense.Implementation`):
  - `IViewElementFactoryService` over ordered `IViewElementFactory` exports
    ([TypeConversion] pairs, extenders supersede by [Order]); default factories:
    `ClassifiedTextElement` → classified `TextBlock` runs (colors from the view's
    classification format map, per-run bold/italic/underline/classification-font),
    `ContainerElement` → stacked/wrapped panels (recursive), `ImageElement` → fixed-size
    placeholder (image catalogs are a host concern), object → ToString(), Control
    passthrough. Navigation runs render in link style but aren't click-hit-tested
    (Avalonia inlines expose no per-run input) — documented divergence.
  - `IToolTipService` (real; the demo/test stubs are gone) with ordered
    `IToolTipPresenterFactory` exports and a default presenter per the Modern ToolTip
    spec: single-use, converts content through the view element factories, popups through
    the "quickinfo" reservation manager, mouse-tracking dismissal per `ToolTipParameters`
    (keep-open callback honored, buffer change dismisses, `IToolTipPresenter2`).
  - **Async completion presenter** (`ICompletionPresenterProvider`,
    [Name(DefaultCompletionPresenter)][ContentType("any")], one presenter per view): item
    list with pattern-highlighted spans and suffixes, filter toggles raising
    `FiltersChanged` with the full updated state set, soft selection rendered as outline
    vs. filled selection, suggestion-mode row (`DisplayTextWhenEmpty` fallback), selected
    row scrolled into view, mouse selection/commit events; popup on the "completion"
    manager anchored to the applicable span. The vendored `AsyncCompletionBroker` picks it
    up and drives Open/Update/Close.
  - **Signature help broker + session** (Morgania-authored per PLAN §3.3/§5.4 — the repo
    carries only the Def contracts): ordered content-type-scoped
    `ISignatureHelpSourceProvider`s augment the session, `GetBestMatch` chain selects,
    caret-tracking sessions dismiss when the caret leaves the selected signature's
    applicability span, buffer changes recalculate, dismissal disposes sources; presenter
    from ordered `IIntellisensePresenterProvider`s, hosted through the presenter-declared
    reservation manager. The default `IPopupIntellisensePresenter` renders the selected
    signature with the current parameter bolded, an "N of M" overload indicator, and
    signature/parameter docs, positioned above the span (flipping below when completion
    holds that space).
- **Demo**: a toy language service (`DemoIntellisense.cs`) — word completion over the
  document + keywords (Ctrl+Space; arrows/PageUp/Dn navigate via
  `IAsyncCompletionSessionOperations`, Tab/Enter commit, Esc dismisses, typing refilters),
  Quick Info on hover (through the vendored controller + MouseHover), signature help on
  '(' or Ctrl+Shift+Space with Up/Down overload cycling — wired with tunneling handlers
  until Modern Commanding lands (PLAN §5.6). The headless smoke drives all three through
  the real brokers and checks the popups host in the overlay.
- **Tests** (`tests/Morgania.IntellisenseTests`, own composition host with the *real*
  tooltip service): popup agent overlay hosting + geometry, manager name validation,
  presenter unit render (items/filters/soft selection/suggestion row/FiltersChanged),
  broker-driven completion end-to-end (prefix filtering, popup, commit replacing the
  applicable span, dismissal), suggestion mode (soft-selected), Quick Info end-to-end
  (visible state, classified content rendered in the popup, dismissal), signature help
  end-to-end (best-match selection, overload indicator, selection change, caret-tracking
  dismissal + source disposal). 50 tests green overall (11 geometry + 28 behavior + 4
  composition + 7 intellisense).

### M6 known divergences / deferred items
- Popups host in the window's `OverlayLayer` (need a themed window; they clip to the
  window) rather than OS popup windows — acceptable under PLAN §2's no-pixel-parity rule.
- `ClassifiedTextRun.NavigationAction` is not invocable (no per-inline hit testing).
- Completion/signature-help keyboard handling is demo-side until Modern Commanding
  (PLAN §5.6); the vendored `CompletionCommandHandlers` are not yet dispatched.
- Test-environment note: the vendored Quick Info session `Debug.Assert`s its computation
  is off the JTC main thread; the headless session's UI thread is a *thread-pool* thread,
  so vs-threading's `await TaskScheduler.Default` completes inline there and the assert
  fires spuriously. The test host downgrades assert failures to console output; no
  product code is affected.

## Post-M6 field fixes

Three field-reported bugs, each with a mutation-verified repro test
(`tests/Morgania.IntellisenseTests/FieldBugRegressionTests.cs`, driven through real
headless window input):

- **Quick Info never showed on hover in the demo.** Root cause: the view factory only
  invoked `IWpfTextViewCreationListener` exports; the vendored
  `QuickInfoTextViewCreationListener` (which wires `MouseHover` to the async Quick Info
  broker) and `BraceCompletionManagerFactory` export the plain
  `ITextViewCreationListener` contract and never ran. The factory now imports and invokes
  both flavors with the same content-type/role scoping. (The M6 acceptance had passed
  because it drove `TriggerQuickInfoAsync` directly, bypassing the hover pipeline.)
- **Double-click now selects the word under the pointer** (`ClickCount == 2` maps onto
  the vendored `IEditorOperations.SelectCurrentWord`); word-by-word drag extension stays
  deferred with the §5.6 mouse-processor work.
- **A click past the end of a line no longer lands in virtual space.** VS treats virtual
  space as opt-in (`DefaultTextViewOptions.UseVirtualSpaceId`, default off); the mouse
  path now clamps to the line end unless the option is on. Keyboard paths already honored
  it via the vendored editor operations.

Test-infrastructure note discovered along the way: `HeadlessUnitTestSession.Dispatch`
only awaits async bodies through its `Func<Task<TResult>>` overload — a bare `Func<Task>`
binds to the synchronous generic overload and the body is abandoned at its first await,
making tests pass vacuously. `IntellisenseTestHost.RunAsync(Func<Task>)` wraps bodies to
return a value; awaits inside the body let the session's dispatcher loop fire real
`DispatcherTimer`s (the hover cycle runs unmodified in the hover repro test).

53 tests green overall (11 geometry + 28 behavior + 4 composition + 10 intellisense).

### M5 known divergences / deferred items
- Zero-length (purely inserted) space-negotiating adornments are still not negotiated
  (M3 note stands); the collapsed-region "…" hint adornment (VS's
  CollapsedAdornmentProvider) is deferred with them — collapse hides text without a
  placeholder glyph.
- The vendored outlining *commands* (Ctrl+M chords, expand-on-edit inside a collapsed
  region) are not wired; the demo binds its own Ctrl+M toggle.
- Reused elided rows translate their segment boundaries pointwise across edit snapshots
  (`SetSnapshot`); exact gaps re-derive on the next reformat.
- The recompile acceptance is demonstrated with Morgania-authored, contract-only
  extension sources written in the canonical VSSDK sample shapes; recompiling an actual
  third-party WPF extension end-to-end remains a nice-to-have until one with a suitable
  license is selected.

## M6 — RoslynPad runs on Morgania

Morgania + the recompiled EditorFeatures replaced AvalonEdit as RoslynPad's editor:

- **Removed**: the WPF app (`src/RoslynPad`), `RoslynPad.Editor.Windows`,
  `RoslynPad.Editor.Avalonia`, `RoslynPad.Roslyn.Windows`, and both REPL samples.
  RoslynPad is exclusively Avalonia now; Windows packaging uses the Avalonia app.
- **`RoslynHost` composes with VS-MEF** (`Microsoft.VisualStudio.Composition`), mixing
  v1/v2 attribute flavors like VS; `HostServices` comes from the internal
  `VisualStudioMefHostServices` over the shared `ExportProvider`. Rejections are logged
  to `composition.log` (4 expected: Pythia, Peek, and two app-DI parts).
- **`RoslynPad.Roslyn` trimmed** to host/workspace management plus RoslynPad-specific
  services (directive completion, file-based programs, rename helper, LanguageServices
  dialog view models, glyphs, tagged-text classification). The duplicated editor-feature
  layer (brace matching, code fixes/refactorings services, diagnostics plumbing,
  formatting/indentation/quick info/signature help/snippets/structure) is deleted —
  those features now come from the vendored EditorFeatures.
- **The demo's host layer moved into the app** (`src/RoslynPad/Editor/`):
  commanding key bridge, squiggles, light bulb controller, classification definitions,
  LSP snippet expander, host stubs — plus a new `ThemeClassificationFormats` that maps
  VS Code themes onto the classification format map (successor of the AvalonEdit
  `ThemeClassificationColors`).
- **`DocumentView`** opens the Roslyn document over `ITextBuffer.AsTextContainer()` and
  hosts `ITextViewHost`; workspace-applied changes round-trip as minimal buffer edits
  (`RoslynWorkspace.ApplyDocumentTextChanged` defers to the buffer when a handler is
  subscribed).

Deferred/known gaps: find/replace UI (AvalonEdit's search panel is gone; the Edit menu
entries are currently no-ops), folding margin, brace-match highlight rendering,
completion-item glyphs, and jump-to-error-line. ILViewer still uses AvaloniaEdit (plain
text + xshd highlighting only).

## M7 — Glyphs, popup theming, popup polish, multi-caret gestures

Closes several of the M6 gaps and editor-parity issues:

- **VS 2026 image-catalog glyphs**: `Glyphs.axaml` (RoslynPad.Roslyn.Avalonia) is now
  generated from the Visual Studio 2026 Image Library by
  `Resources/Glyphs/generate-glyphs.py`, keyed by moniker name. `ImageCatalog` resolves
  moniker names and `ImageId`s (the id→name map mirrors Roslyn's
  `Extensions.KnownImageIds`) and adapts icon colors to the theme background with a port
  of VS's `ImageThemingUtilities` luminosity transform (`ThemeBackground` set from
  `DocumentView.ApplyTheme`). `GlyphExtensions` maps the RoslynPad `Glyph` enum to
  moniker names (mirrors Roslyn's `GetVsImageData`).
- **Completion + quick info icons**: the app exports an `ImageElement → Control`
  `IViewElementFactory` ordered before Morgania's placeholder factory, so quick info
  symbol glyphs and any other `ImageElement` render from the catalog. Morgania's
  `CompletionPresenter` now renders each item's `CompletionItem.Icon` through
  `IViewElementFactoryService` (with a fixed-size slot for icon-less items so the text
  column stays aligned).
- **Light bulb imagery**: `SuggestedActionsController` shows the real VS icons —
  `IntellisenseLightBulbError` for error fixes, `IntellisenseBulb` for code/style fixes,
  `Screwdriver` for refactorings — instead of the placeholder circles.
- **Popup palette** (`PopupFormatNames`, Morgania.Editor): the default quick info,
  signature help, and completion presenters read their colors (background, foreground,
  border, selection, match highlight, deemphasized text) from the editor format map key
  `"Intellisense Popup"`, falling back to the previous hardcoded dark palette.
  `ThemeClassificationFormats.ApplyPopup` feeds the VS Code theme's widget colors
  (`editorWidget.*`, `list.activeSelection*`, `focusBorder`, `list.highlightForeground`,
  `list.deemphasizedForeground`) into it, so the popups follow the theme.
- **Wheel scroll over popups**: `PopupAgent` forwards unhandled `PointerWheelChanged`
  from its content to `WpfTextView.HandleMouseWheel` (extracted from
  `OnPointerWheelChanged`), so scrolling keeps working when the pointer rests on a quick
  info tip or signature help popup (the completion list still consumes the wheel via its
  own ScrollViewer).
- **Multi-caret gestures** (the broker + vendored EditorOperations already did the heavy
  lifting): Alt(Option)+Shift+click/drag makes a box (column) selection anchored at the
  press point (`IMultiSelectionBroker.SetBoxSelection`); Ctrl/Cmd+Alt+Up/Down adds a
  caret on the neighboring line for every current selection (`TransformSelection` +
  `AddSelectionRange`; the broker merges duplicates). Escape already collapsed to a
  single caret. Alt/Cmd+click add-caret was already in place.

Verification: full `RoslynPad.slnx` build clean; demo `--smoke` passes (now also checks
Ctrl+Alt+Down + multi-caret typing through the commanding bridge); all `tests/Morgania.*`
suites green (the completion-presenter test now composes the presenter's new
imports); a headless render of the glyph grid confirmed the light/dark imagery; the app
composed on the desktop with only the 4 expected MEF rejections. Note: 10 MSB3243
MessagePack 3.1.6/3.1.7 warnings on solution-level builds pre-date this work
(RestoreHelper raw refs vs the 3.1.7 pin).

Remaining M6 gaps: find/replace UI, folding margin, jump-to-error-line; ILViewer still
uses AvaloniaEdit. (Brace-match highlight closed below.)

## M7 follow-up — popup polish + box-selection fixes

Feedback round on M7:

- **Quick info icon spacing**: Morgania's `ContainerElement` factory no longer clobbers
  a child's horizontal margin (it only applies the container's vertical padding), and
  both `ImageElement` factories (the app's catalog-backed one and Morgania's placeholder)
  give inline icons a 6px right margin + centered vertical alignment, so the quick info
  symbol glyph clears the signature text.
- **Completion filter icons**: `CompletionPresenter` renders each filter toggle as its
  `CompletionFilter.Image` through `IViewElementFactoryService` (display text moves to
  the tooltip, as in VS; text remains the fallback for image-less filters), with a
  separator after the `CompletionExpander`. Roslyn's filters flow through the same
  Glyph → ImageId pipeline as item icons, incl. `ExpandScope` for the expander.
- **Box selection survives typing** (was: collapsed to a single caret after one
  character). Root cause in the legacy shims, not the gestures: after a box edit,
  `EditorOperations.InsertText` re-selects via `ITextSelection.Select` +
  `ITextCaret.MoveTo`, and both routed to `IMultiSelectionBroker.SetSelection`, which
  breaks the box. Per VS semantics, `TextSelection.Select` now reshapes the box while
  box mode is active, `TextCaret.MoveTo` moves the insertion point without breaking the
  box, and `TextSelection.AnchorPoint/ActivePoint/Start/End/IsReversed` answer the box's
  corners (not the primary per-line selection) in box mode — `EditorOperations`
  reconstructs the whole box from those after the edit.
- **Box selection through virtual space** (was: the box clamped to line end on short
  lines). Pointer-driven box gestures now always allow virtual space
  (`GetBufferPositionFromViewPoint(allowVirtualSpace)` — VS behavior; the UseVirtualSpace
  option still gates plain clicks), and `SelectionLayer` draws the virtual segment of a
  box span from the endpoints' caret coordinates (text bounds only cover real
  characters). Typing over a virtual-space box materializes the padding whitespace
  (vendored `GetWhitespaceForVirtualSpace`).
- **Glyph mapping generated from Roslyn** (was: a hand-maintained id → moniker
  dictionary in `ImageCatalog` plus a hand-written Glyph → moniker switch in
  `GlyphExtensions`): new `GlyphImageIds` (RoslynPad.Roslyn) bridges to Roslyn's
  `Extensions.GetVsImageData` and reflects the id → moniker table straight from the
  constants of Roslyn's private `Extensions.KnownImageIds` (their names *are* the
  image-catalog monikers — the same names `generate-glyphs.py` keys `Glyphs.axaml` by).
  `GlyphExtensions.GetImageName` reduces to `GlyphImageIds.GetImageName` plus the one
  RoslynPad-specific case (`AddReference`, which Roslyn maps to no image), and
  `ImageCatalog` copies `GlyphImageIds.ImageNames` (only `ExpandScope`, which Roslyn
  emits without a Glyph, stays explicit). Reflection over the private class fails loudly
  on a Roslyn upgrade that moves it. This surfaced a
  real bug: the mirrored `Glyph` enum had drifted from Roslyn's — one `Operator` member
  vs Roslyn's four `Operator*`, so every positional cast past it was off by 3 (e.g.
  dialog members showed `PropertyInternal` imagery for public properties). The enum is
  realigned (breaking: `Glyph.Operator` → `Glyph.OperatorPublic/…`; added
  `Glyph.Copilot` → `SparkleNoColor`).

Verification: solution build clean (0 warnings); demo `--smoke` passes; all
`tests/Morgania.*` suites green (55 tests), including two new box-selection behavior
tests (typing keeps a caret per line; virtual space on short lines + whitespace
materialization); headless harness confirms the generated id table resolves the
KnownImageIds constants to the same images as the moniker path (18 spot checks).
- **Light bulb placement (VS rules — never over code)**: the bulb goes over the caret
  line's leading whitespace when the 16px icon fits there, else onto an adjacent blank
  (or whitespace-only) view line — above preferred — and otherwise into the glyph
  margin. To reach the margin, Morgania's `WpfTextViewHost` now registers itself in the
  view's property bag under `typeof(IWpfTextViewHost)` (the VS convention), and the
  controller lazily mounts a `Canvas` into the (otherwise empty) glyph margin's `Border`.
  The adornment-layer vs margin attachment switches dynamically as the caret moves and
  the view relayouts.
- **Completion description pane** (VS's tooltip beside the list): `CompletionPresenter`
  fetches the selected item's documentation from the item's own source
  (`CompletionItem.Source.GetDescriptionAsync`, the VS presenter's pattern) after a 150ms
  debounce, renders it through the view element factories (so Roslyn's classified
  runs/images colorize), and shows it in a popup-palette-styled box beside the list.
  `PopupAgent` grew `AdjunctContent` for this: secondary content placed top-aligned
  beside the popup — left when it fits, else right, else clamped — reserved together
  with the popup's geometry, sharing its wheel forwarding, and hidden/removed with it.
  Selection moves re-fetch (cancelling in-flight requests); failures or empty
  descriptions just keep the pane hidden. Covered by a new IntellisenseTests test
  (shows docs for the selected item, follows selection, disappears on close).

## Brace matching — highlight + Go To Matching Brace

Closes the M6 "brace-match highlight" gap. Roslyn's brace matching was already fully
compiled in the recompile (`IBraceMatchingService` in Features,
`BraceHighlightingViewTaggerProvider` in EditorFeatures — it tags both braces with
`BraceHighlightTag`, a `TextMarkerTag` of type `"brace matching"`, on every caret move);
what was missing is the presentation layer VS never open-sourced, so the host provides
it (same rule as squiggles), mirrored in RoslynPad and the EditorFeatures demo:

- **`TextMarkerAdornmentManager`**: a generic `ITextMarkerTag` renderer — view tag
  aggregator + full redraw on layout/tag/format-map changes, marker geometry via
  `GetTextMarkerGeometry` on the predefined TextMarker layer (below text). Colors come
  from the editor format map entry named by the tag's `Type` per the
  `MarkerFormatDefinition` contract (FillId/BorderId, falling back to the
  Background brush/color keys of classification-shaped entries); types with no entry
  draw nothing, like VS. This also renders any future `TextMarkerTag` producer
  (e.g. reference highlighting) once its format entry exists.
- **`BraceMatchingMarkerFormat`** (`[Name("brace matching")]`, in ClassificationFormats):
  the static fallback palette; Roslyn's own `BraceMatchingTypeFormatDefinitions` stays
  excluded (WPF). In the app, `ThemeClassificationFormats.ApplyBraceMatching` overrides
  it from the VS Code theme (`editorBracketMatch.background`/`.border`) on every theme
  change, next to `ApplyPopup`.
- **`GoToMatchingBraceCommandHandler`** (`ICommandHandler<GotoBraceCommandArgs>` +
  `GotoBraceExtCommandArgs`): in VS this command lives in the closed-source
  `AbstractVsTextViewFilter`. Reimplemented over `IBraceMatchingService.
  FindMatchingSpanAsync` (JTF-blocked, `GetBraceMatchingOptions`), with VS caret
  semantics: from an open brace land *after* the close brace, from a close brace land
  *before* the open brace, no-op when the caret is not on a brace. The Ext variant
  extends a plain selection from the current anchor to the jump target (documented
  divergence from the VS shell's historical span tweaks). The key bridges map
  Ctrl(Cmd)+] / Ctrl(Cmd)+Shift+] to the two commands.

Verification: solution build clean; all Morgania suites green (56 tests); demo `--smoke`
gained a brace step — caret on the for-loop `{` waits for both `"brace matching"` tags,
asserts the markers are actually drawn on the TextMarker layer, then presses Ctrl+]
through the real key chain and asserts the caret lands after the matching `}`.

## Caret theming

The caret layer's colors were hardcoded to the dark palette (invisible-ish on light
themes). Following the popup-palette pattern: `CaretFormatNames` (Morgania.Editor,
public) names the editor-format-map entries — the VS Fonts and Colors items
"Caret (Primary)" / "Caret (Secondary)" — and `CaretLayer` resolves its brushes from
them (ForegroundBrush/ForegroundColor keys), falling back to the previous dark palette;
an unset secondary derives from the primary, dimmed. `WpfTextView` now takes the view's
`IEditorFormatMap` (factory imports `IEditorFormatMapService`) and refreshes the caret
on `FormatMappingChanged`. In the app, `ThemeClassificationFormats.ApplyCaret` feeds the
theme's `editorCursor.foreground`; the bundled themes don't define it, so the fallback
mirrors VS Code's coded defaults — black on light themes, #AEAFAD on dark. Not bound to
OS values: no cross-platform "OS caret color" exists (Windows XORs, macOS follows app
appearance), and VS/VS Code both treat the caret as theme-owned.
Mutation-verified regression (pixel-samples the caret in a rendered frame after setting
the format-map entry): `CaretColorFollowsTheEditorFormatMap`. 57 tests green overall.

## Find/replace panel

The Morgania editor had the search *engine* (vendored `TextSearchService`,
`TextSearchNavigator`) but no UI — in VS the find control belongs to the shell, so
Morgania supplies an original one. `FindReplacePanel` (Morgania.Editor, public) floats
over the top-right of the view per VS placement: find box, VS-image-library icon
buttons (FindNext/FindPrevious/ReplaceNext/ReplaceAll/chevron — the drawings carried
over from the pre-Morgania `SearchReplacePanel` theme, kept with their halo layers),
Aa / ab / .* option toggles, a match count, and a collapsible replace row.

- **Placement**: an `IWpfTextViewCreationListener` creates the panel per interactive
  "text" view (`FindReplacePanel.Get(view)` retrieves it from the property bag); on
  first `Show` it attaches through `WpfTextViewHost.AddViewOverlay` — a sibling of the
  view's cell in the host grid, so it sits outside the zoom render transform (never
  scales) and is unaffected by scrolling.
- **Behavior**: next/previous/replace through `ITextSearchNavigator3` (wrap-around,
  current-result semantics: first ReplaceNext selects, second replaces); ReplaceAll is
  one `ITextEdit` over `FindAllForReplace` (single undo step, regex substitutions
  expanded). Viewport matches highlight on the panel's own `FindReplaceHighlight`
  adornment layer (after Selection, before TextMarker), current match distinct.
  Enter/Shift+Enter navigate, Escape closes (also from the editor), Alt+R/Alt+A
  replace. Match count capped at 1000; invalid regex reported in the status text.
- **Theming**: `FindReplaceFormatNames` ("Find Replace" editor-format-map entry, popup
  palette pattern) with built-in dark defaults. In the app,
  `ThemeClassificationFormats.ApplyFindReplace` feeds the VS Code theme
  (`editorWidget.*`, `input.*`, `editor.findMatch*`) — with `#RRGGBBAA` theme colors
  converted (Avalonia parses 8-digit hex as `#AARRGGBB`).
- **App wiring**: `DocumentView` subscribes the (previously dead) `FindRequested` /
  `FindReplaceRequested` view-model events (native menu Find/Replace) and adds
  keybindings — Find Ctrl+F/Cmd+F, Replace Ctrl+H/Cmd+Alt+F, F3/Shift+F3 (Cmd+G),
  plus the pre-existing Alt+R/Alt+A replace commands. The demo binds
  Ctrl+F/Ctrl+H/F3.

Verification: solution build clean; all suites green (62 tests) including five new
behavior tests (per-view creation, find-next wrap in both directions,
selection-seeded show + highlight lifecycle, replace-next select-then-replace,
replace-all single-version edit); Skia-headless frame capture confirmed the floating
panel, icons, highlights, and count render correctly over a dark editor.
