namespace Morgania.CodeAnalysis.Editor.Classification;

/// <summary>
/// Maps classification type names to LSP token names.
/// </summary>
public static class SemanticTokensSchema
{
    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToTokenName =>
        EditorFeatures.SemanticTokensSchemaAccessor.ClassificationTypeNameToTokenName;

    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToCustomTokenName =>
        EditorFeatures.SemanticTokensSchemaAccessor.ClassificationTypeNameToCustomTokenName;
}
