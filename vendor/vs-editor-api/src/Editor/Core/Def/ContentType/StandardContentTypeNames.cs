//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    public static class StandardContentTypeNames
    {
        /// <summary>
        /// Base content type of all contents types except for <see cref="Inert"/>.
        /// </summary>
        public const string Any = "any";

        /// <summary>
        /// Base content type of any content type use for a document. Note that <see cref="Projection"/> does not derive from <see cref="Text"/>.
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// Base content type of any document containing code. Derives from <see cref="Text"/>.
        /// </summary>
        public const string Code = "code";

        /// <summary>
        /// Base content type for a projection of a document that contains a mix of distinct content types (e.g. a .aspx file containing
        /// html and embedded c#).
        /// </summary>
        public const string Projection = "projection";

        /// <summary>
        /// A content type for which no associated artifacts are automatically created.
        /// </summary>
        public const string Inert = "inert";
    }
}

