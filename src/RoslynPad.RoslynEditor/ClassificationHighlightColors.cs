using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Classification;

namespace RoslynPad.RoslynEditor
{
    internal static class ClassificationHighlightColors
    {
        public static HighlightingColor DefaultColor { get; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black) }.AsFrozen();

        private static readonly HighlightingColor TypeColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Teal) }.AsFrozen();
        private static readonly HighlightingColor CommentColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Green) }.AsFrozen();
        private static readonly HighlightingColor XmlCommentColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) }.AsFrozen();
        private static readonly HighlightingColor KeywordColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) }.AsFrozen();
        private static readonly HighlightingColor PreprocessorKeywordColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) }.AsFrozen();
        private static readonly HighlightingColor StringColor = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Maroon) }.AsFrozen();

        private static readonly ImmutableDictionary<string, HighlightingColor> _map = new Dictionary<string, HighlightingColor>
        {
            [ClassificationTypeNames.ClassName] = TypeColor,
            [ClassificationTypeNames.StructName] = TypeColor,
            [ClassificationTypeNames.InterfaceName] = TypeColor,
            [ClassificationTypeNames.DelegateName] = TypeColor,
            [ClassificationTypeNames.EnumName] = TypeColor,
            [ClassificationTypeNames.ModuleName] = TypeColor,
            [ClassificationTypeNames.TypeParameterName] = TypeColor,
            [ClassificationTypeNames.Comment] = CommentColor,
            [ClassificationTypeNames.XmlDocCommentAttributeName] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentAttributeValue] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentCDataSection] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentComment] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentDelimiter] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentEntityReference] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentName] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = XmlCommentColor,
            [ClassificationTypeNames.XmlDocCommentText] = CommentColor,
            [ClassificationTypeNames.Keyword] = KeywordColor,
            [ClassificationTypeNames.PreprocessorKeyword] = PreprocessorKeywordColor,
            [ClassificationTypeNames.StringLiteral] = StringColor,
            [ClassificationTypeNames.VerbatimStringLiteral] = StringColor
        }.ToImmutableDictionary();

        public static HighlightingColor GetColor(string classificationTypeName)
        {
            HighlightingColor color;
            _map.TryGetValue(classificationTypeName, out color);
            return color ?? DefaultColor;
        }

        private static HighlightingColor AsFrozen(this HighlightingColor color)
        {
            if (!color.IsFrozen)
            {
                color.Freeze();
            }
            return color;
        }
    }
}