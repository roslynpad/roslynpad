# RoslynPad Packages

RoslynPad's editor stack is available as NuGet packages, letting you embed the same Roslyn-powered C# editor — or the underlying language-agnostic editor — in your own Avalonia apps.

|Package Name|Description|
|------------|-----------|
|[![NuGet](https://img.shields.io/nuget/v/Morgania.CodeAnalysis.Editor.svg?style=flat-square)](https://www.nuget.org/packages/Morgania.CodeAnalysis.Editor) `Morgania.CodeAnalysis.Editor`|The Roslyn-powered C# editor for Avalonia: VS-MEF composition plus the editor-host services (classification, squiggles, light bulb, glyphs, refactoring dialogs). **Start here** — it pulls in the rest of the editor stack|
|[![NuGet](https://img.shields.io/nuget/v/Morgania.CodeAnalysis.EditorFeatures.svg?style=flat-square)](https://www.nuget.org/packages/Morgania.CodeAnalysis.EditorFeatures) `Morgania.CodeAnalysis.EditorFeatures`|Roslyn's EditorFeatures layer (Text, Core, and C#) for the Morgania editor: completion, quick info, signature help, inline rename, brace matching, and more|
|[![NuGet](https://img.shields.io/nuget/v/Morgania.Editor.svg?style=flat-square)](https://www.nuget.org/packages/Morgania.Editor) `Morgania.Editor`|The language-agnostic code editor for Avalonia, based on the [vs-editor-api](https://github.com/microsoft/vs-editor-api) repo, with an Avalonia rendering, input, and popup layer|
|[![NuGet](https://img.shields.io/nuget/v/Morgania.Editor.Abstractions.svg?style=flat-square)](https://www.nuget.org/packages/Morgania.Editor.Abstractions) `Morgania.Editor.Abstractions`|The editor contract surface (types from the [vs-editor-api](https://github.com/microsoft/vs-editor-api) repo) — reference this from editor extensions|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Themes.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Themes) `RoslynPad.Themes`|.NET port of the Visual Studio Code theme reader|
|[![NuGet](https://img.shields.io/nuget/v/RoslynPad.Runtime.Secrets.svg?style=flat-square)](https://www.nuget.org/packages/RoslynPad.Runtime.Secrets) `RoslynPad.Runtime.Secrets`|Cross-platform secret storage using OS-level encryption|

## Samples

- [Morgania.Demo](https://github.com/roslynpad/roslynpad/tree/main/src/Morgania.Demo) — the language-agnostic editor with demo language services, taggers, adornments, and completion
- [Morgania.Demo.EditorFeatures](https://github.com/roslynpad/roslynpad/tree/main/src/Morgania.Demo.EditorFeatures) — a complete Roslyn C# editor host with a minimal workspace
