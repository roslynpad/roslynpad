using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using StructureTag = Microsoft.CodeAnalysis.Editor.Implementation.Structure.StructureTag;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Exports Roslyn's structure tagger (<see cref="AbstractStructureTaggerProvider"/>), which
/// produces <see cref="IStructureTag"/>s for code blocks — the data behind block structure
/// guide lines and outlining. Upstream's concrete provider is excluded from the recompile
/// because its collapsed-region hover preview hosts a zoomed-out projection-buffer view;
/// this stand-in returns the hint text directly instead.
/// </summary>
[Export(typeof(ITaggerProvider))]
[Shared]
[ContentType("Roslyn Languages")]
[TagType(typeof(IStructureTag))]
internal sealed class StructureTaggerProvider : AbstractStructureTaggerProvider
{
    [ImportingConstructor]
    public StructureTaggerProvider(
        TaggerHost taggerHost,
        EditorOptionsService editorOptionsService,
        IProjectionBufferFactoryService projectionBufferFactoryService)
        : base(taggerHost, editorOptionsService, projectionBufferFactoryService)
    {
    }

    internal override object? GetCollapsedHintForm(StructureTag structureTag)
        => structureTag.Snapshot.GetText(structureTag.CollapsedHintFormSpan);
}
