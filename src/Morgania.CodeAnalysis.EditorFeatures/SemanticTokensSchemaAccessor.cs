using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

namespace Morgania.CodeAnalysis.EditorFeatures;

internal static class SemanticTokensSchemaAccessor
{
    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToTokenName =>
        SemanticTokensSchema.GetSchema(clientSupportsVisualStudioExtensions: false).TokenTypeMap;

    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToCustomTokenName =>
        CustomLspSemanticTokenNames.ClassificationTypeNameToCustomTokenName;
}
