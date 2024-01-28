using System.Collections.Generic;
namespace RoslynPad.Roslyn.Classification;

/// <summary>
/// Maps classification type names to LSP token names.
/// </summary>
public static class SemanticTokensSchema
{
    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToTokenName =>
        Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens.SemanticTokensSchema.GetSchema(clientSupportsVisualStudioExtensions: false).TokenTypeMap;

    public static IReadOnlyDictionary<string, string> ClassificationTypeNameToCustomTokenName =>
        Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens.CustomLspSemanticTokenNames.ClassificationTypeNameToCustomTokenName;
}
