#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Composition;

using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The standard adornment layer definitions (in VS these are exported by the WPF view
/// implementation). Layers ordered before "Text" render under the text; the built-in
/// selection and caret layers anchor the two ends of the stack.
/// </summary>
public sealed class StandardAdornmentLayers
{
    [Export]
    [Name(PredefinedAdornmentLayers.Outlining)]
    [Order(Before = PredefinedAdornmentLayers.CurrentLineHighlighter)]
    public AdornmentLayerDefinition? OutliningLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.CurrentLineHighlighter)]
    [Order(Before = PredefinedAdornmentLayers.Selection)]
    public AdornmentLayerDefinition? CurrentLineHighlighterLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.Selection)]
    [Order(Before = PredefinedAdornmentLayers.TextMarker)]
    public AdornmentLayerDefinition? SelectionLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.TextMarker)]
    [Order(Before = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? TextMarkerLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.Text)]
    [Order]
    public AdornmentLayerDefinition? TextLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.Squiggle)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? SquiggleLayer { get; }

    [Export]
    [Name(IntraTextAdornmentSupportProvider.IntraTextAdornmentSupport.LayerName)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? IntraTextAdornmentLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.BraceCompletion)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? BraceCompletionLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.BlockStructure)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? BlockStructureLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.InterLine)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    public AdornmentLayerDefinition? InterLineLayer { get; }

    [Export]
    [Name(PredefinedAdornmentLayers.Caret)]
    [Order(After = IntraTextAdornmentSupportProvider.IntraTextAdornmentSupport.LayerName)]
    public AdornmentLayerDefinition? CaretLayer { get; }
}

/// <summary>
/// Concrete metadata view for content-type + view-role scoped exports
/// (System.Composition needs concrete dictionary-constructor views; ADR-003 rule 5).
/// </summary>
public sealed class ContentTypeAndTextViewRoleMetadata : IContentTypeAndTextViewRoleMetadata
{
    public ContentTypeAndTextViewRoleMetadata(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ContentTypes = MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        TextViewRoles = MetadataValue.GetMany<string>(data, nameof(TextViewRoles));
    }

    public IEnumerable<string> ContentTypes { get; }

    public IEnumerable<string> TextViewRoles { get; }
}
