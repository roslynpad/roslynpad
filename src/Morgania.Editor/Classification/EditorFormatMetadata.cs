#nullable enable

namespace Microsoft.VisualStudio.Text.Classification.Implementation;

using Microsoft.VisualStudio.Utilities;

/// <summary>
/// Concrete metadata view for <c>[Export(typeof(EditorFormatDefinition))]</c> parts
/// (System.Composition requires concrete dictionary-constructor views; ADR-003 rule 5).
/// </summary>
public class EditorFormatMetadata
{
    public EditorFormatMetadata(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Name = MetadataValue.Get<string>(data, nameof(Name));
        UserVisible = MetadataValue.Get<bool>(data, nameof(UserVisible));
        Priority = MetadataValue.Get<int>(data, nameof(Priority));
    }

    public string? Name { get; }

    public bool UserVisible { get; }

    public int Priority { get; }
}

/// <summary>
/// Concrete metadata view for classification format definitions: editor format exports
/// that carry <c>[ClassificationType]</c> metadata and orderability.
/// </summary>
public sealed class ClassificationFormatMetadata : EditorFormatMetadata, IOrderable
{
    public ClassificationFormatMetadata(IDictionary<string, object> data)
        : base(data)
    {
        ClassificationTypeNames = MetadataValue.GetMany<string>(data, nameof(ClassificationTypeNames));
        Before = MetadataValue.GetMany<string>(data, nameof(Before));
        After = MetadataValue.GetMany<string>(data, nameof(After));
    }

    public IEnumerable<string> ClassificationTypeNames { get; }

    public IEnumerable<string> Before { get; }

    public IEnumerable<string> After { get; }

    string IOrderable.Name => Name ?? string.Empty;
}
