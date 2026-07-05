# Morgania.Editor

The Morgania code editor for Avalonia, based on the
[vs-editor-api](https://github.com/microsoft/vs-editor-api) repo: the editor
engine — text model, projection, classification, tagging, completion,
editor operations, multi-caret — paired with a Morgania-authored Avalonia layer
for rendering, input, and popups. It implements the contracts in
[Morgania.Editor.Abstractions](https://www.nuget.org/packages/Morgania.Editor.Abstractions).

The editor is language-agnostic — it knows nothing about C# or Roslyn. For a
Roslyn-powered C# editor with everything pre-wired, start at
[Morgania.CodeAnalysis.Editor](https://www.nuget.org/packages/Morgania.CodeAnalysis.Editor)
instead.

## What's inside

- The vendored editor implementation: text buffers and snapshots, projection,
  undo, classification and tagging machinery, brace completion, editor
  operations, completion session brokers, and cross-platform multi-caret.
- The Avalonia view layer: text view and view host, caret/selection/adornment
  layers, margins, IME support, clipboard bridging, completion, quick info, and signature help.

## Usage

The editor is a set of MEF parts; the host composes them (here with
`System.Composition`) and supplies a few host services — a `JoinableTaskContext`
bound to the UI thread, an `ISmartIndentationService`, and an
`ILoggingServiceInternal` (a no-op implementation is fine):

```csharp
var container = new ContainerConfiguration()
    .WithAssemblies(
    [
        // The abstractions assembly carries parts too (option/format definitions).
        Assembly.Load("Morgania.Editor.Abstractions"),
        Assembly.Load("Morgania.Editor"),
    ])
    .WithAssembly(typeof(HostServices).Assembly) // your host/language parts
    .CreateContainer();

// Create a buffer and a view:
var buffer = container.GetExport<ITextBufferFactoryService>()
    .CreateTextBuffer("Hello, Morgania!", contentType);
var editorFactory = container.GetExport<ITextEditorFactoryService>();
var view = editorFactory.CreateTextView(buffer);
var viewHost = editorFactory.CreateTextViewHost(view, setFocus: true);
// viewHost.HostControl is an Avalonia control — place it anywhere.
```

For a complete, runnable example — including demo language services, taggers,
adornments, outlining, and completion — see the
[Morgania.Demo](https://github.com/roslynpad/roslynpad/tree/main/src/Morgania.Demo)
project.
