# RoslynPad

<img src="docs/roslynpad.svg" height="100" alt="RoslynPad" />

A cross-platform C# editor powered by Roslyn and the Morgania editor - an Avalonia editor based on the [vs-editor-api](https://github.com/microsoft/vs-editor-api) repo.

![RoslynPad](docs/screenshots/roslynpad.webp)

## Installing

**You must also install a supported [.NET SDK](https://aka.ms/dotnet) to allow RoslynPad to compile programs.**

| Source | |
|-|-|
| GitHub | [![Downloads](https://img.shields.io/github/downloads/aelij/RoslynPad/total.svg?style=flat-square)](https://github.com/aelij/RoslynPad/releases/latest) |
| Microsoft Store | <a href="https://www.microsoft.com/store/apps/9nctj2cqwxv0?ocid=badge"><img src="https://get.microsoft.com/images/en-us%20light.svg" height="30" alt="Microsoft Store badge logo" /></a> |
| winget | `winget install --id RoslynPad.RoslynPad` |
| Homebrew | `brew install --cask roslynpad` |

## Packages

See [Packages](docs/packages/README.md) for more information.

## Building

To build the source code, use one of the following:
* `dotnet build`
* Visual Studio Code with the C# extension
* Visual Studio 2026 (Windows only)

## Features

### Completion

![Completion](docs/screenshots/completion.webp)

### Signature Help

![Signature Help](docs/screenshots/signature-help.webp)

### Quick Info

![Quick Info](docs/screenshots/quick-info.webp)

### Diagnostics

![Diagnostics](docs/screenshots/diagnostics.webp)

### Code Fixes

![Code Fixes](docs/screenshots/actions.webp)

### NuGet Packages

![NuGet Packages](docs/screenshots/nuget.webp)

### Document Management

![Document Management](docs/screenshots/documents.webp)

### Dump Results

![Dump Results](docs/screenshots/dump.webp)
