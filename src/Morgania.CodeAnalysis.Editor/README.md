# Morgania.CodeAnalysis.Editor

The Roslyn-powered C# editor for Avalonia, built on the Morgania editor
(`Morgania.Editor`) and the Morgania Roslyn EditorFeatures
(`Morgania.CodeAnalysis.EditorFeatures`).

The package contains the editor-host layer — everything a functioning Roslyn editor
needs beyond the editor platform itself:

- **`EditorComposition`** — builds the single VS-MEF graph shared by Roslyn and the editor
  (Roslyn Workspaces/Features, the Morgania editor, the Morgania Roslyn EditorFeatures, and this
  assembly's services). Returns the `CompositionConfiguration`, so hosts can inspect
  composition diagnostics before creating the export provider.
- **Editor-host services**, composed automatically: classification format definitions,
  diagnostics squiggles, block structure guide lines, the suggested-actions light bulb,
  key bridging, smart indentation, snippet expansion, and image-catalog glyphs.
- **Refactoring dialogs**: Change Signature, Extract Interface, Pick Members.
- **UI helpers**: `Glyph.ToImageSource()` (Roslyn glyphs → Avalonia `DrawingImage`),
  `TaggedText.ToTextBlock()` rich-text rendering, `ImageCatalog` for known image ids.

The package does not include a Roslyn workspace implementation — hosts bring their own
`Workspace` and open documents over editor buffers (see the samples below).

## Getting started

```csharp
// On the UI thread, before the graph composes:
HostServiceExports.InitializeMainThread();

var configuration = EditorComposition.CreateConfiguration();
// Optional: inspect configuration.CompositionErrors / Catalog.DiscoveredParts.DiscoveryErrors.
// Rejected parts are expected (EditorFeatures parts with VS-only imports), so don't ThrowOnErrors().
var exportProvider = configuration.CreateExportProviderFactory().CreateExportProvider();

// Create a buffer, open a Roslyn document over it in your workspace:
var contentType = exportProvider.GetExportedValue<IContentTypeRegistryService>().GetContentType("CSharp");
var buffer = exportProvider.GetExportedValue<ITextBufferFactoryService>().CreateTextBuffer(code, contentType);
// ... add a project + document to your Workspace and open it with buffer.AsTextContainer()

// Create the view:
var editorFactory = exportProvider.GetExportedValue<ITextEditorFactoryService>();
var view = editorFactory.CreateTextView(buffer);
var viewHost = editorFactory.CreateTextViewHost(view, setFocus: true);
// viewHost.HostControl is an Avalonia control — place it anywhere.
```

For a complete, runnable example — including a minimal host workspace backed by the editor
buffer — see the
[Morgania.Demo.EditorFeatures](https://github.com/roslynpad/roslynpad/tree/main/src/Morgania.Demo.EditorFeatures)
project. For a full-featured host (multiple documents, NuGet references, execution), see RoslynPad itself.
