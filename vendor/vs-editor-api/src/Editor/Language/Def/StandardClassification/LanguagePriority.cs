// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.StandardClassification
{
    /// <summary>
    /// Represents the built-in priorities for language classifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The set of default classification types and format definitions provided by the <see cref="IStandardClassificationService"/> define
    /// natural language and formal language classification types. These classification types act as classification types upon which other
    /// classification types are based.
    /// </para>
    /// <para>
    /// Formal language classifications have higher priority than natural language classification types (and by definition all derivatives of
    /// formal language classifications have higher priority than derivatives of the natural language classification types).
    /// </para>
    /// <para>
    /// Both the formal and natural language classification types fall between the Priority.Default and Priority.High
    /// generic priorities.
    /// </para>
    /// </remarks>
    public static class LanguagePriority
    {
        /// <summary>
        /// Priority for the natural language classification definitions.
        /// </summary>
        public const string NaturalLanguage = "Natural Language Priority";

        /// <summary>
        /// Priority for the formal language classificaiton definitions.
        /// </summary>
        public const string FormalLanguage = "Formal Language Priority";
    }
}
