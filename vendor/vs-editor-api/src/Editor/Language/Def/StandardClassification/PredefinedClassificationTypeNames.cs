// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.StandardClassification
{
    /// <summary>
    /// Defines a list of predefined classification type names.
    /// </summary>
    /// <remarks>
    /// The classification type names defined herein are used by the <see cref="IStandardClassificationService"/> to create the set of pre-existing classification types.
    /// </remarks>
    public static class PredefinedClassificationTypeNames
    {
        /// <summary>
        /// Name of the classification type representing the background color of Peek adornements.
        /// </summary>
        public const string PeekBackground = "peek background";

        /// <summary>
        /// Name of the classification type representing the background color of Peek adornments when they do not have keyboard focus.
        /// </summary>
        public const string PeekBackgroundUnfocused = "peek background unfocused";

        /// <summary>
        /// Name of the classification type representing the border color when Peek is focused.
        /// </summary>
        public const string PeekFocusedBorder = "peek focused border";

        /// <summary>
        /// Name of the classification type representing the color of a history dot in Peek when it is currently selected.
        /// </summary>
        public const string PeekHistorySelected = "peek history selected";

        /// <summary>
        /// Name of the classification type representing the color of a history dot in Peek when the mouse is currently over it.
        /// </summary>
        public const string PeekHistoryHovered = "peek history hovered";

        /// <summary>
        /// Name of the classification type representing the foreground color of peek error pages. This should match ToolWindowText color by default.
        /// </summary>
        public const string PeekLabelText = "peek label text";

        /// <summary>
        /// Name of the classification type representing the background color of peek highlights.
        /// </summary>
        public const string PeekHighlightedText = "peek highlighted text";

        /// <summary>
        /// Name of the classification type representing the background color of peek highlights when the peek window is not currently focused.
        /// </summary>
        public const string PeekHighlightedTextUnfocused = "peek highlighted text unfocused";

        /// <summary>
        /// Name of the classification type representing comments.
        /// </summary>
        public const string Comment = "comment";

        /// <summary>
        /// Name of the classification type representing identifiers. In C# for instance, these would be variable names.
        /// </summary>
        public const string Identifier = "identifier";

        /// <summary>
        /// Name of the classification type representing keywords. In C# for instance, foreach would be a keyword.
        /// </summary>
        public const string Keyword = "keyword";

        /// <summary>
        /// Name of the classification type representing white space.
        /// </summary>
        public const string WhiteSpace = "whitespace";

        /// <summary>
        /// Name of the classification type representing operators. In C# for instance, + would be an operator.
        /// </summary>
        public const string Operator = "operator";

        /// <summary>
        /// Name of the classification type representing literals.
        /// </summary>
        public const string Literal = "literal";

        /// <summary>
        /// Name of the classification type representing markup attributes. Markup attributes are attributes in markup languages,
        /// such as HTML, XML, and YAML.
        /// </summary>
        public const string MarkupAttribute = "markup attribute";

        /// <summary>
        /// Name of the classification type representing markup attribute values. Markup attribute values are values
        /// of attributes in markup languages, such as HTML, XML, and YAML.
        /// </summary>
        public const string MarkupAttributeValue = "markup attribute value";

        /// <summary>
        /// Name of the classification type representing markup nodes. Markup nodes are nodes in markup languages,
        /// such as HTML, XML, and YAML.
        /// </summary>
        public const string MarkupNode = "markup node";

#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// Name of the classification type representing strings.
        /// </summary>
        public const string String = "string";
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Name of the classification type representing types.
        /// </summary>
        public const string Type = "type";

        /// <summary>
        /// Name of the classification type representing characters.
        /// </summary>
        public const string Character = "character";

        /// <summary>
        /// Name of the classification type representing numbers.
        /// </summary>
        public const string Number = "number";

        /// <summary>
        /// Name of the classification type representing all other types of classifications.
        /// </summary>
        public const string Other = "other";

        /// <summary>
        /// Name of the classification type representing items that are excluded via a preprocessor macro or other means.
        /// </summary>
        public const string ExcludedCode = "excluded code";

        /// <summary>
        /// Name of the classification type representing preprocessor keywords.
        /// </summary>
        public const string PreprocessorKeyword = "preprocessor keyword";

        /// <summary>
        /// Name of the classification type representing definition of symbols.
        /// </summary>
        public const string SymbolDefinition = "symbol definition";

        /// <summary>
        /// Name of the classification type representing symbol references.
        /// </summary>
        public const string SymbolReference = "symbol reference";

        /// <summary>
        /// Name of the classification type representing a natural language classification. This classification type is intended to be used
        /// as a base classification type for other classification types belonging to a set of natural language classifications.
        /// </summary>
        public const string NaturalLanguage = "natural language";

        /// <summary>
        /// Name of the classification type representing a formal language. This classification type is intended to be used as a base
        /// classification type for all classification types belonging to a set of formal langauge classifications. For example literals
        /// and keywords.
        /// </summary>
        public const string FormalLanguage = "formal language";

        /// <summary>
        /// Name of the classification type representing plain text.
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// Name of the classification type representing punctuation.
        /// </summary>
        public const string Punctuation = "punctuation";
    }
}
