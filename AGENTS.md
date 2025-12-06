# RoslynPad

## Project Overview

RoslynPad is a cross-platform C# editor built on Roslyn (compiler services) and AvalonEdit. It supports Windows (WPF) and macOS/Linux (Avalonia).

## Architecture

### Layer Structure (bottom to top)

```
RoslynPad.Runtime     → Injected into user scripts (.Dump() extension methods)
RoslynPad.Roslyn      → Core Roslyn integration, workspace management
RoslynPad.Build       → Script compilation, NuGet restore, process execution
RoslynPad.Themes      → VS Code theme parsing
RoslynPad.Common.UI   → Shared ViewModels, services (platform-agnostic)
RoslynPad.Editor.*    → Code editor components (Windows/Avalonia variants)
RoslynPad.Roslyn.*    → Platform-specific Roslyn UI (glyphs, etc.)
RoslynPad/RoslynPad.Avalonia → Application entry points
```

### Platform-Specific Code Pattern

Platform code follows this pattern:
- Abstract base in `RoslynPad.Common.UI` (e.g., `MainViewModel`)
- Concrete implementations with platform suffix (e.g., `MainViewModelWindows`, `MainViewModelAvalonia`)
- `AVALONIA` preprocessor symbol distinguishes platforms in shared editor code

### Key Classes

- `RoslynHost` - Central Roslyn services host using MEF composition
- `RoslynWorkspace` - Per-document Roslyn workspace extending `Microsoft.CodeAnalysis.Workspace`
- `ExecutionHost` - Compiles and executes scripts in separate processes via JSON IPC
- `MainViewModel` - Application state and document management
- `OpenDocumentViewModel` - Individual document state, execution, NuGet integration

## Build Commands

```bash
# Build cross-platform app (default for VS Code)
dotnet build src/RoslynPad.Avalonia

# Build full solution (Windows required for WPF projects)
dotnet build RoslynPad.sln

# CI build with binary log
dotnet build -bl -c Release -m:1 RoslynPad.sln
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
- Platform-specific classes: `*Windows`, `*Avalonia` suffix
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

Relevant internal namespaces are exposed in `RoslynPad.Roslyn.csproj`.

## Testing Cross-Platform Changes

When modifying platform-agnostic code (especially in `RoslynPad.Common.UI`, `RoslynPad.Roslyn`, or `RoslynPad.Build`), verify changes compile on both WPF and Avalonia targets.

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

**Platform Packaging:**
- **Windows**: Uses WPF project (`RoslynPad`), creates `.zip` + `.appx` (Microsoft Store), updates winget manifests
- **macOS**: Uses Avalonia project, creates `.dmg` (requires `appdmg` via npm) and `.tgz`
- **Linux**: Uses Avalonia project, creates `.tgz`

**Key Details:**
- `Common.ps1` - Shared functions, reads version from `Directory.Build.props`
- Windows packages run on Windows, macOS/Linux packages run on macOS
- `dotnet publish -r <rid>` with `ContinuousIntegrationBuild=true` for reproducible builds
- Version is centrally defined as `RoslynPadVersion` in `Directory.Build.props`

## Important Files

- `Directory.Build.props` - Shared build settings, version numbers, target frameworks
- `Directory.Packages.props` - Central package version management
- `src/RoslynPad.Build/ExecutionHost.cs` - Script execution engine
- `src/RoslynPad.Roslyn/RoslynHost.cs` - Roslyn service composition
- `src/RoslynPad.Common.UI/ViewModels/` - Core application ViewModels
- `deploy/CreatePackages.ps1` - Release packaging script
