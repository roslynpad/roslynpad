# Morgania.CodeAnalysis.EditorFeatures

Roslyn's EditorFeatures layer built for the Morgania editor.

This is the glue between Roslyn's language services and the editor: it connects
`SourceText` to editor text buffers and provides the editor-facing features for C#.

## What's inside

- **Text bridging** — `SourceText`/`ITextBuffer` adapters
  (`ITextSnapshot.AsText()`, `SourceTextContainer` over buffers), so Roslyn
  documents can be opened directly over editor buffers.
- **Completion** — async completion (with snippets), quick info, and signature
  help sources backed by Roslyn.
- **Editing features** — brace matching and completion, automatic and smart
  indentation, formatting commands, rename tracking, inline rename sessions,
  string copy/paste, raw-string and interpolation handling.
- **Navigation and analysis** — go to definition, document highlighting,
  reference highlighting, navigation bar items, structure/outlining tags, and the
  pull-diagnostics tagging infrastructure.
- **Suggested actions** — the code-fix/refactoring engine behind the light bulb,
  including preview changes.

## Usage

Everything here is MEF parts plus extension methods — there is no entry point to call. The parts must be composed into the same VS-MEF graph as the Morgania editor and Roslyn's own Workspaces/Features layers.

Most hosts should use
[Morgania.CodeAnalysis.Editor](https://www.nuget.org/packages/Morgania.CodeAnalysis.Editor),
whose `EditorComposition` builds exactly that graph and adds the editor-host
services (classification formats, squiggles, light bulb, glyphs) this assembly
expects. See that package's README for a getting-started walkthrough, and the
[Morgania.Demo.EditorFeatures](https://github.com/roslynpad/roslynpad/tree/main/src/Morgania.Demo.EditorFeatures)
project for a complete, runnable host.
