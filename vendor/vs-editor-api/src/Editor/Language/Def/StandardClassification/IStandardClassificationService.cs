// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.StandardClassification
{
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// Provides access to standard classifications.
    /// </summary>
    /// <remarks>This is a MEF Component, and should be imported with the following attribute:
    /// [Import]
    /// </remarks>
    public interface IStandardClassificationService
    {
        /// <summary>
        /// Gets a classification type representing a natural language.
        /// </summary>
        IClassificationType NaturalLanguage { get; }

        /// <summary>
        /// Gets a classification type representing a formal language.
        /// </summary>
        IClassificationType FormalLanguage { get; }

        /// <summary>
        /// Gets a classification type representing comments in a formal language.
        /// </summary>
        IClassificationType Comment { get; }

        /// <summary>
        /// Gets a classification type representing identifiers in a formal language.
        /// </summary>
        IClassificationType Identifier { get; }

        /// <summary>
        /// Gets a classification type representing keywords in a formal language.
        /// </summary>
        IClassificationType Keyword { get; }

        /// <summary>
        /// Gets a classification type representing whitespace in a formal language.
        /// </summary>
        IClassificationType WhiteSpace { get; }

        /// <summary>
        /// Gets a classification type representing whitespace in a formal language.
        /// </summary>
        IClassificationType Operator { get; }

        /// <summary>
        /// Gets a classification type representing literals in a formal language.
        /// </summary>
        IClassificationType Literal { get; }

        /// <summary>
        /// Gets a classification type representing numerical literals which derives from the literal classification type in a formal language.
        /// </summary>
        IClassificationType NumberLiteral { get; }

        /// <summary>
        /// Gets a classification type representing string literals which derives from the literal classification type in a formal language.
        /// </summary>
        IClassificationType StringLiteral { get; }

        /// <summary>
        /// Gets a classification type representing character literals which derives from the literal classification type in a formal language.
        /// </summary>
        IClassificationType CharacterLiteral { get; }

        /// <summary>
        /// Gets a classification type representing anything in a formal language.
        /// </summary>
        IClassificationType Other { get; }

        /// <summary>
        /// Gets a classification type representing blocks excluded by the preprocessor or other means in a formal language.
        /// </summary>
        IClassificationType ExcludedCode { get; }

        /// <summary>
        /// Gets a classification type representing preprocessor keywords in a formal language.
        /// </summary>
        IClassificationType PreprocessorKeyword { get; }

        /// <summary>
        /// Gets a classification type representing the definition of a symbol in a formal language.
        /// </summary>
        IClassificationType SymbolDefinition { get; }

        /// <summary>
        /// Gets a classification type representing the reference to a symbol in a formal language.
        /// </summary>
        IClassificationType SymbolReference { get; }
    }
}
