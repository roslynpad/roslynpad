# Morgania — the Visual Studio Editor on Avalonia
**Project specification, v0.2**

## 1. Goal

Implement the missing view/rendering layer of the Microsoft Visual Studio editor on Avalonia, on top of the open-source editor core from `microsoft/vs-editor-api` (MIT, archived 2023-11), **preserving the original type and assembly identities** so that Roslyn's editor layer (`Microsoft.CodeAnalysis.EditorFeatures`) runs against it. The end state is a fully functional, cross-platform C# editor: semantic classification, completion, quick info, signature help — the real Roslyn editor experience hosted in Avalonia. This repo will ONLY host the code of the editor, not the adapted Roslyn implementation.

**Definition of success for this repo:**
A demo app edits a file with a simple highligher, completion, and quick info on Windows, macOS, and Linux.

## 2. Non-goals (v1)

- Language services beyond C#/VB via Roslyn (no LSP host, no TextMate grammars).
- Legacy/obsolete IntelliSense surfaces: the deprecated sync QuickInfo path (`IQuickInfoSource`, `ILegacyQuickInfoSource` et al.) and legacy completion (`ICompletionSource`/`CompletionSet`) get no Morgania implementation or presenter — only the modern async APIs (`IAsyncQuickInfoSource`, `IAsyncCompletionSource`) are served.
- Binary compatibility with VSIX packages or the VS shell (`IVsTextView`, `IOleCommandTarget`, Win32 interop margins).
- Difference viewer, peek definition, CodeLens.
- Debugger integration, project system, designer surfaces — this is an editor, not an IDE.
- Pixel-perfect visual parity with VS. Behavioral and API parity is the bar; theming is ours.

## 3. Normative references and the quarantine rule

References are split into two classes with different handling rules, because the single largest project risk after version skew (§5.4) is architectural contamination from a reference that solves the same problem with a different design.

### 3.1 Normative (defines correctness)

1. **`vs-editor-api` contracts** — the definition assemblies. XML doc comments on `ITextView`, `ITextViewLine`, `IAdornmentLayer`, `ITextCaret`, etc. are the specification; where they are precise (geometry, virtual space, line semantics), precision is contractual.
2. **`vs-editor-api` core implementations** — text model, projection, classification, tagging, operations, and the modern broker logic (async completion et al.) that shipped in the repo. Consumed as vendored source; observed behavior is normative.
4. **The repo wiki specs** ("Modern Editor Commanding Revisited", "Modern ToolTip API", completion API walkthroughs) for command and presenter abstractions.

The Cocoa surfaces in the repo (`ICocoaTextView` et al.) are **ignored entirely** — not implemented, not referenced. The WPF surface is the sole contract.

### 3.2 Consultative (platform cookbook — quarantined)

1. **AvaloniaEdit** — Avalonia platform techniques only: `TextFormatter` usage, custom text sources, caret rendering, IME, scroll/pointer integration.
2. **Avalonia's own `TextBox`/`TextPresenter`** source — same role, closer to current Avalonia idioms.

**Quarantine rules (binding on all contributors, human and AI):**

- Consultative sources are never in context, open in the IDE, or in the repo during design or architecture work.
- Consultation is a separate, scoped task: one concrete platform question in, a short **platform note** out (technique, constraints, pitfalls, in our own words) checked into `docs/platform-notes/`. Implementation proceeds from the note, not the source.
- No code copied from consultative sources. AvalonEdit's idioms (mutable `TextDocument`, visual lines, height tree, element generators) answer the same questions as the VS model with an incompatible design; copied code smuggles in copied architecture.
- If a Morgania type resembles an AvalonEdit type more than the VS contract it implements, that is a design defect, found-in-review.

### 3.3 License firewall

Everything Morgania builds from is MIT/Apache-licensed source or original work. The proprietary Microsoft NuGet packages (`Microsoft.VisualStudio.CoreUtility`, `.Text.Data`, `.Text.UI`, etc. — "Microsoft Software License for Visual Studio Add-ons and Extensions": use only with VS, no redistribution, no reverse engineering) are **never referenced, decompiled, or reflected over**. This costs nothing: those packages are Microsoft's binaries of the same code whose source `vs-editor-api` carries under MIT — full source for the definition assemblies and the platform-neutral implementations. The parts the repo does *not* carry (the WPF/Cocoa view layers) are precisely what Morgania implements as original work.

Rules:

1. Vendored code comes only from the `vs-editor-api` repo (MIT). Copyright headers and the MIT license text travel with it.
2. **M0 dependency audit:** every `PackageReference` reachable from a vendored project must be MIT/Apache. Known-OSS: `Microsoft.VisualStudio.Threading`, `.Validation`, `StreamJsonRpc`, `System.Collections.Immutable` (`.Composition`/vs-mef is moot per ADR-003).
3. **Post-archive API gaps (§5.4) are recreated from public documentation (learn.microsoft.com) and MIT call sites (Roslyn `EditorFeatures`), never from decompiled proprietary binaries.** API names and signatures are functional interface facts; implementations are written fresh. The proprietary license's no-reverse-engineering clause is respected by never needing it — the documented signatures plus consumer call sites fully determine the gap surface.
4.

## 4. Naming and identity policy

**Original names are kept throughout.** Namespaces, type names, member names — including `IWpfTextView`, where "Wpf" is retained as a historical label; the name is part of the contract. This is a functional requirement (Roslyn compatibility), not aesthetics. The policy has two tiers because signatures differ in how much platform they leak:

### 4.1 Bit-compatible tier
Every assembly referenced by *platform-neutral* Roslyn editor code (`Microsoft.VisualStudio.Text.Data`, `.Logic`, `.UI`, `CoreUtility`, the definition assemblies): identical assembly identity, namespaces, types, and signatures. These leak no WPF types, so full fidelity is achievable, and it is what lets Tier-1 Roslyn run unmodified.

### 4.2 Name-preserving, signature-adapted tier
`Microsoft.VisualStudio.Text.UI.Wpf` (and our implementation behind it): all names preserved; members whose signatures leak WPF types are re-typed to Avalonia equivalents (`FrameworkElement`→`Control`, `Brush`→`IBrush`, `System.Windows.Media.TextFormatting.TextRunProperties`→Avalonia `TextRunProperties`, `Visual`→`Visual`). Binary compatibility is deliberately sacrificed here — its only significant consumer is `EditorFeatures`, which we recompile from source anyway (§5.3), and name preservation is what keeps that diff mechanical: call sites that never touch the static platform type compile without modification.

## 5. Architecture

### 5.1 Composition

**Decision (ADR-003, final): `System.Composition` (MEF v2) end-to-end.** The vendored editor core is converted from MEF v1 attributes to v2 as part of the fork. The original rationale for preserving v1 attributes was diff-minimization against upstream — and the core's upstream is archived; there is nothing to merge from, so divergence discipline buys nothing there (contrast Tier 2, §5.3, whose upstream is alive). All Morgania-authored code, the converted core, and the rewritten `EditorFeatures` exports use v2 uniformly; Roslyn's Tier-1 binaries already do. One container, no bridging.

**Conversion rules (the v1→v2 delta is small but has one silent failure mode):**

1. **Lifetime default flip — the landmine.** v1 parts are shared by default; v2 parts are non-shared by default. Every part lacking an explicit v1 `NonShared` policy gets `[Shared]` in conversion. Enforced by tests: the composition suite asserts reference-identity for a maintained list of known-singleton services, so a missed `[Shared]` is a red test, not a runtime mystery.
2. Interface metadata views → concrete metadata classes (the central ones live in CoreUtility, which we own; generate mechanically).
3. Custom metadata attributes (`[ContentType]`, `[Name]`, `[Order]`, …) retagged with v2 `MetadataAttribute`.
4. `IPartImportsSatisfiedNotification` → `[OnImportsSatisfied]` method attribute.
5. **Verify at M0:** v2 typed discovery vs. the core's internal part classes and member accessibility — v1 (and VS-MEF's v1 emulation) was lenient; v2 may not be. Any recomposition/dynamic-catalog reliance (expected: none) surfaces here too.

What this gives up: VS-MEF's serialized composition cache and upfront graph validation. Mitigations: v2 discovery is lightweight (measure before caring), and a CI smoke test walks every export so composition errors fail the build rather than the first `GetExport` at runtime. Residual property, for free: a uniformly v2-attributed codebase is still consumable by a VS-MEF host (it reads both dialects) if anyone wants the cache — host choice survives without Morgania depending on it.

### 5.2 The Roslyn tier model

Removed

### 5.3 Version skew — accepted, deferred, bounded

`vs-editor-api` froze in November 2023; Roslyn did not. **Decision: track latest Roslyn anyway and absorb the drift in the Tier-2 fork and Tier-0 forward-patches**, on the judgment that the editor *contract* surface has been substantially stable since the archive — most post-2023 editor work consumes existing APIs rather than adding them. The matched-set pin (≈ VS 17.8 era) was considered and rejected: it would freeze the C# feature level and trade a one-time gap analysis for permanent staleness.

The drift is handled as an explicit work item, not background anxiety: at M7 entry, a **gap analysis** diffs the editor APIs `EditorFeatures` actually references against the vendored core's surface (a reference-assembly diff plus a compile pass — cheap to produce). Each gap is resolved by forward-patching the vendored core (preferred; the definition packages on NuGet document the missing signatures) or by adapting the fork. If the gap report is large enough to threaten the thesis, that is the decision point to fall back to an older Roslyn — made from data, not preemptively. An early, rough version of the same compile-probe runs at M0 to size the risk before the view layer consumes months.

### 5.4 Rendering pipeline

```
ITextSnapshot ──► line transform sources ──► sequencing (incl. IntraTextAdornmentTag
                                              space negotiation, elision)
            ──► Avalonia TextFormatter (custom ITextSource per line)
            ──► FormattedLine : IWpfTextViewLine   (geometry queries + draw)
            ──► line cache (keyed: snapshot version × viewport × format-map version)
            ──► layered visual tree: text layer / adornment layers ([Order]) /
                                     caret layer / selection per VS semantics
```

Principles:

- **The view renders snapshots.** All layout is a pure function of (snapshot, viewport, format map, line transforms). Mutable document state in the view layer is by definition a defect — the anti-AvalonEdit invariant.
- **`ITextViewLine` geometry is contractual** and golden-tested (§7): character bounds, x↔position mapping, virtual space, `TextTop/Bottom/Baseline`, word-wrap line identity.
- **Space negotiation is designed in at M2**, not retrofitted: `IntraTextAdornmentTag`s become embedded objects in the text source so the formatter reserves space.
- **Bidi is first-class.** No LTR assumptions anywhere in the geometry layer; mixed-direction fixtures in the corpus from M1. Avalonia text-stack edge cases get a seam interface and upstream issues.

### 5.5 Input

- **Commanding:** Modern Commanding (`ICommandHandler<T>` chains over `EditorCommandArgs`) with an Avalonia keymap. This is also what Tier-1 Roslyn ships its command handlers against, so it is the integration surface, not just a choice.
- **IME:** Avalonia `TextInputMethodClient` against the view; composition spans as provisional text with underline adornment. Platform notes per §3.2.
- **Mouse:** processor chain per contract; VS selection gestures including box/multi-selection via the core's multi-selection broker.

## 6. Milestones

A milestone is done when its acceptance criteria pass in CI, not when it demos well.

**M0 — Cores build, convert, and probe.** Vendored editor core compiles on net8.0 and is converted to MEF v2 (§5.2), core tests pass; Tier-1 Roslyn binaries compose with the converted core in a `System.Composition` host; the §5.2 verification items (internal-type discovery, no recomposition reliance) and the rough gap probe (§5.4) are executed; the §3.3 license audit confirms no proprietary package is referenced by any vendored or Morgania project. *Accept:* green CI including export-walk and singleton-identity tests; gap-probe report produced and triaged; dependency-license report checked in.

**M1 — Read-only view.** Classified snapshot rendering via `IClassificationFormatMap`; scrolling; viewport-only formatting; word wrap. *Accept:* golden geometry tests on LTR/RTL/mixed fixtures; 100k-line scroll within frame budget.

**M2 — Editing, caret, selection.** Caret (blink, virtual space, `EnsureVisible`), VS selection semantics incl. multi-selection, editing via `IEditorOperations`, IME composition, text-source design includes embedded-object support. *Accept:* scripted behavior tests from documented VS semantics.

**M3 — Adornments and tagging.** Adornment layers (all three positioning modes), tag aggregation, text markers, intra-text adornments with space negotiation. *Accept:* demo extension adds an intra-text color swatch and brace-highlight markers using only contract APIs.

**M4 — Margins and host.** `IWpfTextViewHost`, line numbers, scrollbars as margins (incl. `IVerticalScrollBar` mapping), glyph margin. *Accept:* margins MEF-discovered, ordered, removable; demo is a recognizable editor.

**M5 — Conformance and hardening.** Zoom, line transforms, elision/outlining via projection, perf pass, public conformance suite. *Accept:* a WPF editor extension runs after recompile with zero or mechanical-only diffs.

**M6 — Completion presenters.** The broker/session logic exists in the vendored core; Morgania supplies the UI: async completion presenter (list, filters, soft selection, suggestion mode per the wiki walkthrough), Modern ToolTip presenter + classified-text content primitives (which gives Quick Info), signature help presenter. *Accept:* a scripted fake language service drives completion/QI/sig-help through the real brokers headlessly; presenter behavior matches the wiki specs.

## 7. Testing and performance

- **Golden geometry tests:** serialized `ITextViewLine` geometry answers diffed against goldens per fixture — the contract-fidelity backstop and bidi safety net.
- **Behavioral tests:** caret, selection, word wrap, virtual space — headless (Avalonia headless platform), scripted from documented VS behavior.
- **Composition tests:** the full standard catalog (converted core + Morgania) composes with zero errors, an export-walk smoke test touches every export (compensating for v2's lazy failure mode), and reference-identity assertions cover the known-singleton service list (the §5.2 lifetime-flip guard) — all from M0 onward.
- **Perf budgets (CI, headless render timing):** 100k-line file < 500 ms to first frame; steady-state scroll < 16 ms/frame; keypress→caret p99 < 10 ms; completion popup open p99 < 50 ms after broker response.

## 8. Risks

| Risk | Exposure | Mitigation |
|---|---|---|
| Avalonia `TextFormatter` gaps (bidi edges, embedded-object metrics) | M1–M3, M6 | Seam over the formatter; fixtures early; upstream issues |
| Tier-2 fork drifts unmergeable | M7+ | Divergence log in CI; upstream structure preserved; M8 merge rehearsal |
| Intra-text adornment complexity | M2–M3 | Designed into the text source at M2 |
| Lifetime regressions from MEF v1→v2 conversion (shared→non-shared default) | M0–M2 | Blanket `[Shared]` rule + singleton-identity tests (§5.2, §7) |
| `Microsoft.*` identity distribution | release |
| Vendored core rot | M0+ | Own the fork; aggressive retarget at M0 |
| IME correctness across platforms | M2 | Platform notes; per-OS manual matrix |
| Scope creep toward an IDE | continuous | §2 binding; M8 is the only pressure valve |
| Reference contamination | continuous | §3.2 quarantine + §9 protocol + review check |
| License contamination (proprietary NuGet binaries, decompiled signatures) | continuous | §3.3 firewall; M0 dependency audit in CI; gap work sourced from docs + MIT call sites only |

## 9. AI-assisted development protocol

- **Design sessions:** context = this spec, relevant contracts with doc comments. Consultative sources prohibited.
- **Platform-question sessions:** separate session; one question + the consultative source in; a platform note out. The note is the only artifact that crosses back.
- **Implementation sessions:** spec section + contracts + platform notes + existing Morgania code; definition-of-done is the milestone's tests.
- **Standing review check:** "does this implement the VS contract's semantics, or an AvalonEdit idiom wearing its name?"
- ADRs are one page, binding, and read by every session; decisions are not relitigated ambiently.

## 10. Open questions (resolve by ADR, not by drift)

2. If M0 finds v2 typed discovery rejecting the core's internal part classes (§5.2 item 5): widen accessibility in the fork vs. convention-based registration — pick whichever keeps the conversion mechanical.
3. Zoom implementation (transform vs. font-size scaling) — decide after M1 perf data.
4. Minimum Avalonia version and whether to track previews for text-stack fixes.