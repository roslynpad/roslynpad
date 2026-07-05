//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Adornments
{
    /// <summary>
    /// Enumerates the predefined structural block types.
    /// </summary>
    public static class PredefinedStructureTagTypes
    {
        /// <summary>
        /// Represents structural blocks, with vertical line adornments displayed.
        /// </summary>
        public const string Structural = nameof(Structural);

        /// <summary>
        /// Represents non-structural blocks, with no vertical line adornments
        /// displayed, only expand and collapse.
        /// </summary>
        public const string Nonstructural = nameof(Nonstructural);

        /// <summary>
        /// Represents a code comment, with vertical line adornments.
        /// </summary>
        public const string Comment = nameof(Comment);

        /// <summary>
        /// Represents a PreprocessorRegion, with vertical line adornments.
        /// </summary>
        public const string PreprocessorRegion = nameof(PreprocessorRegion);

        /// <summary>
        /// Represents an Import or Imports Block, with vertical line adornments.
        /// </summary>
        public const string Imports = nameof(Imports);

        /// <summary>
        /// Represents a Namespace, with vertical line adornments.
        /// </summary>
        public const string Namespace = nameof(Namespace);

        /// <summary>
        /// Represents a Type, with vertical line adornments.
        /// </summary>
        public const string Type = nameof(Type);

        /// <summary>
        /// Represents a class Member, such as a method or property, with vertical line adornments.
        /// </summary>
        public const string Member = nameof(Member);

        /// <summary>
        /// Represents a Statement, with vertical line adornments.
        /// </summary>
        public const string Statement = nameof(Statement);

        /// <summary>
        /// Represents a Conditional, with vertical line adornments.
        /// </summary>
        public const string Conditional = nameof(Conditional);

        /// <summary>
        /// Represents a Loop, with vertical line adornments.
        /// </summary>
        public const string Loop = nameof(Loop);

        /// <summary>
        /// Represents an Expression, with vertical line adornments.
        /// </summary>
        public const string Expression = nameof(Expression);
    }
}
