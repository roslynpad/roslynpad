//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
#nullable enable
namespace Microsoft.VisualStudio.Text.Classification
{
    using Microsoft.VisualStudio.Text.Tagging;

    /// <summary>
    /// Defines a set of well known classification layers to which an
    /// <see cref="ILayeredClassificationType"/> can belong.
    /// </summary>
    /// <remarks>
    /// <see cref="IClassificationTag"/> layers with greater numeric values take
    /// precedence over layers with smaller values. e.g.: <see cref="Semantic"/>
    /// classifications override <see cref="Lexical"/> classifications in cases
    /// of overlap.
    /// </remarks>
    public enum ClassificationLayer
    {
        /// <summary>
        /// The default behavior, assigned to any classifications that
        /// implement <see cref="IClassificationType"/> but do not
        /// implement <see cref="ILayeredClassificationType"/>.
        /// </summary>
        /// <remarks>
        /// The default behavior is 'non-layered', meaning that these classifications
        /// can be merged into any layer. This behavior exists primarily for backwards
        /// compatibility.
        /// </remarks>
        Default = 0,

        /// <summary>
        /// The lexical layer, including classifications generated through pure
        /// lexical analysis of a file.
        /// </summary>
        Lexical = 1_000,

        /// <summary>
        /// The syntatic layer, including classifications generated through parsing
        /// the lexed syntactic tokens into a parse tree.
        /// </summary>
        Syntactic = 2_000,

        /// <summary>
        /// The semantic layer, including classifications generated through type checking
        /// of a file, resolving of references, and other complex analyses.
        /// </summary>
        Semantic = 3_000
    }
}
