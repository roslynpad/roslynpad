# Morgania.Editor.Abstractions

The Morgania editor's contract surface, based on the
[vs-editor-api](https://github.com/microsoft/vs-editor-api) repo.

## What's inside

- **Text model** — `ITextBuffer`, `ITextSnapshot`, spans and tracking points,
  projection buffers, differencing, undo (`Microsoft.VisualStudio.Text.*`).
- **Editor views** — `ITextView`, formatted lines, adornments, margins, outlining,
  multi-caret selection (`Microsoft.VisualStudio.Text.Editor.*`).
- **Classification and tagging** — `IClassifier`, `ITagger<T>`, format and
  classification-type definitions.
- **Language services** — completion, quick info, signature
  help, suggested actions, structure/outlining, navigation
  (`Microsoft.VisualStudio.Language.*`).
- **Commanding and operations** — `ICommandHandler`, editor commands and options,
  `IEditorOperations`.
- **MEF part contracts** — the attributes and metadata types used to export editor
  extensions, plus built-in option and format definitions.

## Usage

Reference this package from libraries that *extend* the editor — they need only the
contracts. The implementation lives in the
[Morgania.Editor](https://www.nuget.org/packages/Morgania.Editor) package, which a
host application composes together with its extensions via MEF.

For a Roslyn-powered C# editor with everything pre-wired, start at
[Morgania.CodeAnalysis.Editor](https://www.nuget.org/packages/Morgania.CodeAnalysis.Editor).
