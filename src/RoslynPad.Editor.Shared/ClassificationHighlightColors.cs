using System.Collections.Generic;
using System.Collections.Immutable;
#if AVALONIA
using Avalonia.Media;
using AvaloniaEdit.Highlighting;
#else
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
        public HighlightingColor CommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Green) };
        public HighlightingColor XmlCommentBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
        public HighlightingColor KeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Blue) };
        public HighlightingColor PreprocessorKeywordBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Gray) };
        public HighlightingColor StringBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Maroon) };
        public HighlightingColor BraceMatchingBrush { get; protected set; } = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Colors.Black), Background = new SimpleHighlightingBrush(Color.FromRgb(219, 224, 204))};

        public const string BraceMatchingClassificationTypeName = "brace matching";

        private ImmutableDictionary<string, HighlightingColor> _map;
        protected virtual ImmutableDictionary<string, HighlightingColor> GetOrCreateMap()
        {
            return _map ?? (_map = new Dictionary<string, HighlightingColor>
            {
                [ClassificationTypeNames.ClassName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.StructName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.InterfaceName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.DelegateName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.EnumName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.ModuleName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.TypeParameterName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.Comment] = AsFrozen(CommentBrush),
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
                [ClassificationTypeNames.PreprocessorKeyword] = AsFrozen(PreprocessorKeywordBrush),
                [ClassificationTypeNames.StringLiteral] = AsFrozen(StringBrush),
                [ClassificationTypeNames.VerbatimStringLiteral] = AsFrozen(StringBrush),
                [BraceMatchingClassificationTypeName] = AsFrozen(BraceMatchingBrush)
            }.ToImmutableDictionary());
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