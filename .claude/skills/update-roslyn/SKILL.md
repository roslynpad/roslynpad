---
name: update-roslyn
description: Update the Roslyn version used by this repo — bump public package versions, sync the vendor/roslyn submodule to the package commit, then build, test, and run Morgania and RoslynPad and fix fallout. Use when asked to update/bump Roslyn or the Roslyn submodule.
---

# Updating the Roslyn version

Two things must stay in sync, both pointing at the **same Roslyn commit**:

1. `RoslynVersion` in `Directory.Packages.props` — the public `Microsoft.CodeAnalysis.*` packages from nuget.org.
2. The `vendor/roslyn` submodule — EditorFeatures plus selected LanguageServer/Remote sources are recompiled against the public packages, so source/binary API drift breaks the build.

## Step 1 — Bump versions in props files

- `Directory.Build.props`: update the `<Version>` property (the NuGet version the Morgania packages ship as).
- `Directory.Packages.props`: update `<RoslynVersion>` to the new public version.

## Step 2 — Find the matching Roslyn commit

The public package's nuspec `<description>` ends with the matching Roslyn commit. Get that SHA:

```sh
curl -sL "https://api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.common/$ROSLYN_VERSION/microsoft.codeanalysis.common.nuspec" \
  | grep -o 'commit/[0-9a-f]*'
```

## Step 3 — Update the roslyn submodule

Check out the commit SHA found in step 2 (check first — it may already be there):

```sh
git -C vendor/roslyn fetch origin <sha>
git -C vendor/roslyn checkout <sha>
```

The submodule pointer change gets committed with the rest of the update.

## Step 4 — Clean, build, test, run, fix

**Clean first — this is not optional.** IgnoresAccessChecksToGenerator caches the
publicized copies of the Roslyn assemblies under `obj/**/IgnoresAccessChecksToGenerator/`,
and its incremental up-to-date check will happily keep serving assemblies publicized from
the *old* Roslyn version. Stale leftovers produce baffling errors (missing internal types,
missing namespaces, CS1069 type-forward failures) that look like API drift but aren't.
Delete all `bin`/`obj` before the first build:

```sh
find src tests -type d \( -name obj -o -name bin \) -prune -exec rm -rf {} +
```

SDK is pinned by `global.json`.

```sh
dotnet build RoslynPad.slnx
dotnet test RoslynPad.slnx
```

If the first post-clean build shows errors in downstream projects (e.g.
`Morgania.CodeAnalysis.Editor`), rebuild once before investigating — errors from a build
where an upstream project was still broken can be stale-state artifacts that vanish on
the next pass.

Then run both apps and smoke-test editor features (completion, quick info, signature help, diagnostics/squiggles, rename, formatting; in RoslynPad also NuGet resolution and script execution):

- `src/Morgania.Demo.EditorFeatures` (and `src/Morgania.Demo`) — the Morgania editor
- `src/RoslynPad` — the RoslynPad app

### Fixing fallout

Most breakage comes from recompiling the new vendored sources:

- **Missing/changed APIs in vendored EditorFeatures code** — fix in `src/Morgania.CodeAnalysis.EditorFeatures/Shims` and `Stubs`, or add the file to the `<Compile Remove>` list in the csproj if the feature is intentionally excluded (WPF/VS-only files). New Roslyn code may also require new editor APIs in `src/Morgania.Editor`.
- **CS0507 or internal-access errors** — the IgnoresAccessChecksToGenerator publicizer is involved; the vendored code accesses internals of the public Roslyn packages, so check the publicized assembly list rather than making code changes.
- **Missing LSP/remote types** — check the explicit LanguageServer and Workspaces/Remote source includes in `Morgania.CodeAnalysis.EditorFeatures.csproj`, the linked decompiler files in `RoslynPad.csproj`, and `MorganiaMefHostServices`. Upstream may move or split these files during an upgrade.

Fix issues in both the Morgania projects and RoslynPad — don't stop at a green build; both apps must run.
