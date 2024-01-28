using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using RoslynPad.Roslyn.Classification;

namespace RoslynPad.Editor;

public class ClassificationHighlightColors : IClassificationHighlightColors
{
    public HighlightingColor DefaultBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black) };

    public HighlightingColor TypeBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Teal) };
    public HighlightingColor MethodBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Olive) };
    public HighlightingColor ParameterBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.DarkBlue) };
    public HighlightingColor CommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Green) };
    public HighlightingColor XmlCommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
    public HighlightingColor KeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };
    public HighlightingColor PreprocessorKeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
    public HighlightingColor StringBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Maroon) };
    public HighlightingColor BraceMatchingBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black), Background = new SimpleHighlightingBrush(Color.FromArgb(150, 219, 224, 204)) };
    public HighlightingColor StaticSymbolBrush { get; protected set; } = new HighlightingColor { FontWeight = FontWeights.Bold };


    private readonly Lazy<ImmutableDictionary<string, HighlightingColor>> _map;

    public ClassificationHighlightColors()
    {
        _map = new Lazy<ImmutableDictionary<string, HighlightingColor>>(() => new Dictionary<string, HighlightingColor>
        {
            [ClassificationTypeNames.ClassName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.RecordClassName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.RecordStructName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.StructName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.InterfaceName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.DelegateName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.EnumName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.ModuleName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.TypeParameterName] = TypeBrush.AsFrozen(),
            [ClassificationTypeNames.MethodName] = MethodBrush.AsFrozen(),
            [ClassificationTypeNames.ExtensionMethodName] = MethodBrush.AsFrozen(),
            [ClassificationTypeNames.ParameterName] = ParameterBrush.AsFrozen(),
            [ClassificationTypeNames.Comment] = CommentBrush.AsFrozen(),
            [ClassificationTypeNames.StaticSymbol] = StaticSymbolBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentAttributeName] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentAttributeValue] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentCDataSection] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentComment] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentDelimiter] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentEntityReference] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentName] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = XmlCommentBrush.AsFrozen(),
            [ClassificationTypeNames.XmlDocCommentText] = CommentBrush.AsFrozen(),
            [ClassificationTypeNames.Keyword] = KeywordBrush.AsFrozen(),
            [ClassificationTypeNames.ControlKeyword] = KeywordBrush.AsFrozen(),
            [ClassificationTypeNames.PreprocessorKeyword] = PreprocessorKeywordBrush.AsFrozen(),
            [ClassificationTypeNames.StringLiteral] = StringBrush.AsFrozen(),
            [ClassificationTypeNames.VerbatimStringLiteral] = StringBrush.AsFrozen(),
            [AdditionalClassificationTypeNames.BraceMatching] = BraceMatchingBrush.AsFrozen()
        }.ToImmutableDictionary());
    }

    protected virtual ImmutableDictionary<string, HighlightingColor> GetOrCreateMap()
    {
        return _map.Value;
    }

    public HighlightingColor GetBrush(string classificationTypeName)
    {
        GetOrCreateMap().TryGetValue(classificationTypeName, out var brush);
        return brush ?? DefaultBrush.AsFrozen();
    }
}
