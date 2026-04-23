# RoslynPad.Roslyn

Exposes many Roslyn editor services that are currently internal. Package versions correspond to Roslyn package versions.

## Key Types

### `RoslynHost`

Central service host that manages Roslyn workspaces and `System.Composition` (MEF) for dependency injection.

```csharp
var host = new RoslynHost(
    additionalAssemblies: [Assembly.Load("RoslynPad.Roslyn.Avalonia")],
    references: RoslynHostReferences.NamespaceDefault.With(
        assemblyReferences: [typeof(object).Assembly]),
    disabledDiagnostics: null);

var documentId = host.AddDocument(new DocumentCreationArgs(
    textContainer, workingDirectory, SourceCodeKind.Script));

// Later
host.CloseDocument(documentId);
```

### `RoslynHostReferences`

Configures default assembly references and namespace imports for scripts.

```csharp
var references = RoslynHostReferences.NamespaceDefault.With(
    assemblyReferences: [typeof(object).Assembly, typeof(Enumerable).Assembly],
    typeNamespaceImports: [typeof(Console)]);
```

### `RoslynWorkspace`

Per-document Roslyn workspace. Accessed via `host.GetDocument(documentId)` or `host.CreateWorkspace()`.

### Services

| Namespace | Interface | Purpose |
|-----------|-----------|---------|
| `Diagnostics` | `IDiagnosticsUpdater` | Real-time diagnostic events |
| `Completion` | `CompletionItemExtensions` | Glyph and description for completion items |
| `SignatureHelp` | `ISignatureHelpProvider` | Method signature overload display |
| `QuickInfo` | `IQuickInfoProvider` | Hover tooltip information |
| `BraceMatching` | `IBraceMatchingService` | Matching brace highlighting |
| `CodeActions` | `CodeActionExtensions` | Quick fix and refactoring actions |
| `Rename` | `RenameHelper` | Symbol rename support |
| `Structure` | `IBlockStructureService` | Code folding regions |

For a full initialization and editor integration sample, see the [samples](https://github.com/aelij/RoslynPad/tree/main/samples) directory.
