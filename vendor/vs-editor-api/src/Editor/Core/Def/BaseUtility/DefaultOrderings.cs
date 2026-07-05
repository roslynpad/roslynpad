//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Static class defining some default placeholders for the ordering attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Orderable items that do not explicitly indicate they are before <see cref="DefaultOrderings.Lowest"/> have an implicit constraint
    /// that they are after <see cref="DefaultOrderings.Lowest"/>.
    /// </para>
    /// <para>
    /// Orderable items that do not explicitly indicate they are after <see cref="DefaultOrderings.Highest"/> have an implicit constraint
    /// that they are before <see cref="DefaultOrderings.Highest"/>.
    /// </para>
    /// </remarks>
    public static class DefaultOrderings
    {
        public const string Lowest = "Lowest Priority";
        public const string Low = "Low Priority";
        public const string Default = "Default Priority";
        public const string High = "High Priority";
        public const string Highest = "Highest Priority";
    }
}
