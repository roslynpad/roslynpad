using System;
using System.Collections.Generic;
using System.Collections.Immutable;
#if AVALONIA
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
using FontWeights = Avalonia.Media.FontWeight;
#else
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
#endif
using Microsoft.CodeAnalysis.Classification;

namespace RoslynPad.Editor
{
    public class ClassificationHighlightColors : IClassificationHighlightColors
    {
        public HighlightingColor DefaultBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black) };

        public HighlightingColor TypeBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Teal) };
        public HighlightingColor MethodBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Olive) };
        public HighlightingColor CommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Green) };
        public HighlightingColor XmlCommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
        public HighlightingColor KeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };
        public HighlightingColor PreprocessorKeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
        public HighlightingColor StringBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Maroon) };
        public HighlightingColor BraceMatchingBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black), Background = new SimpleHighlightingBrush(Color.FromArgb(150, 219, 224, 204)) };
        public HighlightingColor StaticSymbolBrush { get; protected set; } = new HighlightingColor { FontWeight = FontWeights.Bold };

        public const string BraceMatchingClassificationTypeName = "brace matching";

        private readonly Lazy<ImmutableDictionary<string, HighlightingColor>> _map;

        public ClassificationHighlightColors()
        {
            _map = new Lazy<ImmutableDictionary<string, HighlightingColor>>(() => new Dictionary<string, HighlightingColor>
            {
                [ClassificationTypeNames.ClassName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.StructName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.InterfaceName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.DelegateName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.EnumName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.ModuleName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.TypeParameterName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.MethodName] = AsFrozen(MethodBrush),
                [ClassificationTypeNames.Comment] = AsFrozen(CommentBrush),
                [ClassificationTypeNames.StaticSymbol] = AsFrozen(StaticSymbolBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeName] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeValue] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentCDataSection] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentComment] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentDelimiter] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentEntityReference] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentName] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentText] = AsFrozen(CommentBrush),
                [ClassificationTypeNames.Keyword] = AsFrozen(KeywordBrush),
                [ClassificationTypeNames.ControlKeyword] = AsFrozen(KeywordBrush),
                [ClassificationTypeNames.PreprocessorKeyword] = AsFrozen(PreprocessorKeywordBrush),
                [ClassificationTypeNames.StringLiteral] = AsFrozen(StringBrush),
                [ClassificationTypeNames.VerbatimStringLiteral] = AsFrozen(StringBrush),
                [BraceMatchingClassificationTypeName] = AsFrozen(BraceMatchingBrush)
            }.ToImmutableDictionary());
        }

        protected virtual ImmutableDictionary<string, HighlightingColor> GetOrCreateMap()
        {
            return _map.Value;
        }

        public HighlightingColor GetBrush(string classificationTypeName)
        {
            GetOrCreateMap().TryGetValue(classificationTypeName, out var brush);
            return brush ?? AsFrozen(DefaultBrush);
        }

        private static HighlightingColor AsFrozen(HighlightingColor color)
        {
            if (!color.IsFrozen)
            {
                color.Freeze();
            }
            return color;
        }
    }
}