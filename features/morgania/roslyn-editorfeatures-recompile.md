# Recompiling Roslyn EditorFeatures against Morgania

## What these projects do

`Morgania.CodeAnalysis.EditorFeatures` **recompiles Roslyn's own `EditorFeatures` source** (the Text, Core, and CSharp trees, in one assembly) from the `vendor/roslyn` git submodule, but swap the Visual Studio editor dependencies (`Microsoft.VisualStudio.Text.*`) for **Morgania**, an Avalonia-based OSS reimplementation referenced from `../../../morgania/src/Morgania.Editor/Morgania.Editor.csproj`.

The goal is to consume Roslyn's editor logic without the closed-source VS editor assemblies.

## The hard constraint: never edit submodule source

**The `.cs` files under `vendor/roslyn/` must not be modified.** They are pristine upstream Roslyn source, pulled in via `<Compile Include="../../vendor/roslyn/src/EditorFeatures/**/*.cs" />`. All adaptation happens *outside* the submodule using the mechanisms below. Do not stage, commit, or hand-edit files inside `vendor/roslyn`.

## How the build is made to work

The build succeeds through a stack of tricks — understand each before changing anything:

1. **Submodule as source** — `vendor/roslyn` is a git submodule (`dotnet/roslyn`). Its `EditorFeatures` `.cs` files are pulled in with a `Compile` glob rather than referenced as a compiled assembly.
2. **Package refs replace project refs** — where upstream Roslyn projects reference sibling Roslyn projects, this build references the matching NuGet packages (`Microsoft.CodeAnalysis.*`) at `$(RoslynVersion)` from [Directory.Packages.props](../../Directory.Packages.props) instead.
3. **`IgnoresAccessChecksToGenerator`** — grants access to Roslyn's `internal` types (e.g. `Logger`) via `<IgnoresAccessChecksTo Include="..." />` items, because EditorFeatures source uses Roslyn internals not exposed in the public package surface.
4. **`IgnoresAccessChecksToExcludeTypeName`** — resolves conflicts where a type exists both in the referenced package and the recompiled source (e.g. `Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder`1`), by excluding it from the generated access-check bypass.

## Playbook: upgrading to a new Roslyn version

Do these steps in order and rebuild after each meaningful change:

1. **Bump the version.** Update `<RoslynVersion>` in [Directory.Packages.props](../../Directory.Packages.props). This drives every `Microsoft.CodeAnalysis.*` package version.
2. **Move the submodule to the matching tag.** Check out the `vendor/roslyn` submodule at the tag/commit that corresponds to the same Roslyn version (the submodule source and the NuGet packages **must** be the same version, or internal types and signatures will mismatch). Never edit files inside the submodule to fix this — check out the correct commit instead.
3. **Rebuild** `dotnet build src/Morgania.CodeAnalysis.EditorFeatures`, and fix errors using the strategies below.

## Playbook: fixing build errors after an upgrade

Prefer the least invasive fix, in this order:

1. **`internal` Roslyn member inaccessible** (`CS0122`) → add an `<IgnoresAccessChecksTo Include="AssemblyName" />` item for the owning assembly.
2. **Duplicate / ambiguous type between package and recompiled source** (`CS0433`, ambiguity) → add an `<IgnoresAccessChecksToExcludeTypeName Include="Full.Type.Name`arity" />` item to exclude it.
3. **A single upstream file is fundamentally incompatible (last resort)** → exclude it from compilation with `<Compile Remove="../../vendor/roslyn/src/EditorFeatures/.../Problem.cs" />`. If the file is still needed, copy it into this project's own source tree (outside `vendor/roslyn`), **mirroring the upstream relative path** (e.g. `src/EditorFeatures/.../Problem.cs`), and fix the copy. Prefer excluding over copying; prefer copying over editing the submodule. Document any removed/copied file with a comment in the `.csproj`.

## Additional adaptation mechanisms (in `Morgania.CodeAnalysis.EditorFeatures`)

- **`Shims/VisualStudioShims.cs`** — empty `System.Runtime.Remoting.Contexts` / `EnvDTE` /
  `TextManager.Interop` / `OLE.Interop` namespaces satisfy unused `using` directives, plus minimal
  internal stand-ins for VS shell types that only flow through Roslyn-internal interfaces
  (`__VSPROVISIONALVIEWINGSTATUS`, `System.Drawing.Icon`).
- **`Shims/WpfShims.cs`** — the WPF story: empty `System.Windows.*` namespaces + `global using`
  aliases mapping WPF type names to Avalonia (`UIElement` → `Control`, `Brush`, `Color`, `Border`,
  `Canvas`, `Line`, …), plus `WpfCompatExtensions` (C# 14 extension members) bridging API-shape
  differences (`Control.Dispatcher`, `Line.X1..Y2`, WPF-style `TryFindResource`,
  `JoinableTaskFactory.WithPriority`). This lets WPF-dependent upstream files compile unmodified.
- **`Stubs/`** — no-op stand-ins for *excluded Roslyn types* that other (compiled) vendor files
  reference, so those files compile unmodified. E.g. `CopilotGenerateDocumentationCommentManager`
  no-ops exactly like the real one does when the VS Copilot suggestion service is absent. Prefer a
  stub over excluding-and-copying the referencing file — copies rot on upgrades, stubs don't.
- **`PatchedCompile` items** — build-time text patches for vendor files whose only incompatibility
  cannot be fixed from the outside: the IgnoresAccessChecksTo publicizer makes `internal virtual`
  package members `public` in reference assemblies, so upstream `internal override`s fail with
  CS0507 and the modifier must be widened to `public`. The `GeneratePatchedCompile` target
  generates a patched copy into the intermediate directory and adds the copy to the `Compile` list;
  the original is excluded via a **static `<Compile Remove="@(PatchedCompile)" />`** (outside the
  target) so that the remove is evaluated during static item evaluation and reliably matches the
  glob-expanded items on all platforms (including Windows CI). The patch task **fails the build if
  the expected text is missing**, so upgrades can't silently drift.
- **`RestoreHelper`** — packages that exist only on the Azure DevOps feeds (not nuget.org), e.g.
  `Microsoft.CodeAnalysis.LanguageServer.Protocol` and `Microsoft.CodeAnalysis.Remote.Workspaces`,
  are restored there at `$(RoslynPrivateVersion)` and injected as raw `Reference` items via the
  `GetLibReferences` target.
- **Morgania API additions** — newer VS editor APIs that Morgania's snapshot lacks but that are
  faithful platform APIs (not VS-shell/Copilot internals) have been added to Morgania itself, e.g.
  `IAsyncCompletionItemManager2`/`CompletionList<T>`, NavigateTo interfaces,
  `IBackgroundWorkIndicatorService`, `ISuggestedActionsSource3`/`IAsyncSuggestedActionsSource`,
  `IBracePairTag`, `IContainerStructureTag`, `IndentingStyle`, `CodeLensDescriptorContext`.

## Critical: Morgania may lack newer VS editor APIs

Morgania was built from an **earlier MIT-licensed snapshot** of the Visual Studio editor. A newer Roslyn version may use VS editor APIs (types, members, overloads) that did **not** exist in that snapshot and are therefore absent from Morgania. This is **not** something to paper over with a dummy namespace or a stub — a missing real API means the recompiled feature cannot function.

If the build fails because Morgania is missing a required VS editor API, **implement the missing APIs** in Morgania.

## Guardrails

- **Never modify files under `vendor/roslyn/`.** If a fix seems to require it, use exclude-and-copy instead.
- Keep `$(RoslynVersion)` and the submodule commit in lockstep.
- Do not remove existing `IgnoresAccessChecksToExcludeTypeName` or dummy namespaces without confirming the build no longer needs them — they exist to resolve real conflicts.
- After changes, verify `Morgania.CodeAnalysis.EditorFeatures` builds, then run the demo `--smoke`.
