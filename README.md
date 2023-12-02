# RoslynPad

![RoslynPad](src/RoslynPad/Resources/RoslynPad.png)

A cross-platform C# editor based on Roslyn and AvalonEdit

## Installing

**You must also install a supported .NET SDK to allow RoslynPad to compile programs.**

| Source | |
|-|-|
| GitHub | [![Downloads](https://img.shields.io/github/downloads/aelij/RoslynPad/total.svg?style=flat-square)](https://github.com/aelij/RoslynPad/releases/latest) |
| Microsoft Store | <a href="https://www.microsoft.com/store/apps/9nctj2cqwxv0?ocid=badge"><img src="https://get.microsoft.com/images/en-us%20light.svg" height="50" alt="Microsoft Store badge logo" /></a> |
| winget | `winget install --id RoslynPad.RoslynPad` |

### Running on macOS

1. Copy the app to the `Applications` directory.
1. On the first run, right click the app on Finder and select **Open**.
   
   You will be prompted that the app is not signed by a known developer - click **Open**.

   For more information see [Open a Mac app from an unidentified developer](https://support.apple.com/guide/mac-help/mh40616).

## Packages

RoslynPad is also available as NuGet packages which allow you to use Roslyn services and the editor in your own apps.

[Code samples](https://github.com/aelij/RoslynPad/tree/main/samples)

|Package Name|Description|
|------------|-----------|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Roslyn.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Roslyn) `RoslynPad.Roslyn`|Exposes many Roslyn editor services that are currently internal|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Roslyn.Windows.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Roslyn.Windows) `RoslynPad.Roslyn.Windows`|Provides platform-specific (WPF) implementations for UI elements required by the `RoslynPad.Roslyn` package|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Roslyn.Avalonia.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Roslyn.Avalonia)` RoslynPad.Roslyn.Avalonia`|Provides platform-specific (Avalonia) implementations for UI elements required by the `RoslynPad.Roslyn` package|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Editor.Windows.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Editor.Windows) `RoslynPad.Editor.Windows`|Provides a Roslyn-based code editor using AvaloniaEdit (WPF platform) with completion, diagnostics, and quick actions|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Editor.Avalonia.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Editor.Avalonia) `RoslynPad.Editor.Avalonia`|Provides a Roslyn-based code editor using AvalonEdit (Avalonia platform) with completion, diagnostics, and quick actions|

Package versions match Roslyn's.

## Building

To build the source code, use one of the following:
* `dotnet build`
* Visual Studio 2022
* Visual Studio Code with the C# Dev Kit extension

Solutions:
* `src/RoslynPad.sln` - contains all projects (recommended only on Windows)
* `src/RoslynPad.Avalonia.sln` - contains only cross-platform projects

## Features

### Completion

![Completion](docs/Completion.png)

### Signature Help

![Signature Help](docs/SignatureHelp.png)

### Diagnostics

![Diagnostics](docs/Diagnostics.png)

### Code Fixes

![Code Fixes](docs/CodeFixes.png)
