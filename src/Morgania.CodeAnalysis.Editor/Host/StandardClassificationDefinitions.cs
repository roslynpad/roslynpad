using System.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// The standard classification types Roslyn's classification types derive from
/// ("formal language", "keyword", "identifier", …). In Visual Studio these come from the
/// closed-source StandardClassification implementation; the vendored editor only defines
/// "text", so the host registers the rest, the way a language package would.
/// </summary>
public sealed class StandardClassificationDefinitions
{
    [Export]
    [Name(PredefinedClassificationTypeNames.NaturalLanguage)]
    [BaseDefinition(PredefinedClassificationTypeNames.Text)]
    public ClassificationTypeDefinition? NaturalLanguage { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.FormalLanguage)]
    [BaseDefinition(PredefinedClassificationTypeNames.Text)]
    public ClassificationTypeDefinition? FormalLanguage { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Comment)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Comment { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Identifier)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Identifier { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Keyword)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Keyword { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.WhiteSpace)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? WhiteSpace { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Operator)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Operator { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Literal)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Literal { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.String)]
    [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
    public ClassificationTypeDefinition? StringLiteral { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Character)]
    [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
    public ClassificationTypeDefinition? Character { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Number)]
    [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
    public ClassificationTypeDefinition? Number { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Other)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? Other { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.ExcludedCode)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? ExcludedCode { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.PreprocessorKeyword)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? PreprocessorKeyword { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.SymbolDefinition)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? SymbolDefinition { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.SymbolReference)]
    [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
    public ClassificationTypeDefinition? SymbolReference { get; }
}
