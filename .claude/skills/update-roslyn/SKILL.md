---
name: update-roslyn
description: Update the Roslyn version used by this repo — bump package versions in Directory.Build.props / Directory.Packages.props, correlate the private-feed version by commit ID, sync the vendor/roslyn submodule to the same commit, then build, test, and run Morgania and RoslynPad and fix fallout. Use when asked to update/bump Roslyn or the Roslyn submodule.
---

# Updating the Roslyn version

Three things must stay in sync, all pointing at the **same Roslyn commit**:

1. `RoslynVersion` in `Directory.Packages.props` — the public `Microsoft.CodeAnalysis.*` packages from nuget.org.
2. `RoslynPrivateVersion` in `Directory.Packages.props` — `Microsoft.CodeAnalysis.LanguageServer.Protocol` and `Microsoft.CodeAnalysis.Remote.Workspaces` from the dnceng `dotnet-tools` feed (not published to nuget.org). Consumed only by `src/RestoreHelper/RestoreHelper.csproj`, which pulls their DLLs for direct reference.
3. The `vendor/roslyn` submodule — its `src/EditorFeatures/{Text,Core,CSharp}` sources are recompiled by `src/Morgania.CodeAnalysis.EditorFeatures` against the public packages, so source/binary API drift breaks the build.

## Step 1 — Bump versions in props files

- `Directory.Build.props`: update the `<Version>` property (the NuGet version the Morgania packages ship as).
- `Directory.Packages.props`: update `<RoslynVersion>` to the new public version.

## Step 2 — Find the matching private version

The public and private packages both end their nuspec `<description>` with
`https://github.com/dotnet/roslyn/commit/<sha>`. Correlate on that SHA.

Get the public package's commit:

```sh
curl -sL "https://api.nuget.org/v3-flatcontainer/microsoft.codeanalysis.common/$ROSLYN_VERSION/microsoft.codeanalysis.common.nuspec" \
  | grep -o 'commit/[0-9a-f]*'
```

List candidate private versions (feed is anonymously readable; also browsable at
https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-tools/NuGet/Microsoft.CodeAnalysis.LanguageServer.Protocol/):

```sh
curl -sL "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/flat2/microsoft.codeanalysis.languageserver.protocol/index.json" \
  | python3 -c "import json,sys; [print(v) for v in json.load(sys.stdin)['versions'] if v.startswith('$ROSLYN_VERSION-')]"
```

There are many prefix matches (e.g. `5.3.0-2.26078.5` for public `5.3.0`), and multiple branches publish builds with the same version prefix — don't assume the newest matches. Narrow by date: the build number encodes an Arcade "short date" (`yy*1000 + mm*50 + dd`, so `2.26263.10` = 2026-05-13). Get the public commit's date (`git -C vendor/roslyn show -s --format=%ci <sha>`) and probe builds from that day and the days right after:

```sh
curl -sL "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/flat2/microsoft.codeanalysis.languageserver.protocol/$CANDIDATE/microsoft.codeanalysis.languageserver.protocol.nuspec" \
  | grep -o 'commit/[0-9a-f]*'
```

Set `<RoslynPrivateVersion>` in `Directory.Packages.props` to the matching version.

## Step 3 — Update the roslyn submodule

Check out the same commit SHA found in step 2 (check first — it may already be there):

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
- **Runtime load failures / `MissingMethodException` around remoting** — the private DLLs (`LanguageServer.Protocol`, `Remote.Workspaces`) were built against specific versions of `StreamJsonRpc`, `MessagePack`, `Nerdbank.Streams`, and `Microsoft.VisualStudio.Threading`; check the private nuspec's dependencies and bump those pins in `Directory.Packages.props` to match.
- **Restore errors on RestoreHelper** — it has its own `RestoreSources` (dnceng + vssdk + vs-impl + nuget.org) and suppresses NU1603/NU1605; verify the private version string is exact.
- **Missing LSP types (`Roslyn.Core` / `Roslyn.Text` namespaces, `Microsoft.CodeAnalysis.Suggestions`, code-cleanup types)** — the private packages' `lib/<tfm>` folder can change between Roslyn versions (5.3.0 shipped `lib/net8.0`; 5.6.0 ships `lib/net10.0`). RestoreHelper's `GetLibReferences` glob then matches nothing and the DLLs silently drop out of the reference list. Check `ls ~/.nuget/packages/microsoft.codeanalysis.languageserver.protocol/<ver>/lib/` and align RestoreHelper's `TargetFramework`/glob with it.

Fix issues in both the Morgania projects and RoslynPad — don't stop at a green build; both apps must run.
