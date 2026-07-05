//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents the built-in priorities for a classification format.
    /// </summary>
    /// <remarks>
    /// These fields are equivalent to the corresponding fields in <see cref="DefaultOrderings"/>, which should be used instead of these fields.
    /// </remarks>
    public static class Priority
    {
        /// <summary>
        /// The default priority.
        /// </summary>
        public const string Default = DefaultOrderings.Default;

        /// <summary>
        /// Low priority.
        /// </summary>
        public const string Low = DefaultOrderings.Low;

        /// <summary>
        /// High priority.
        /// </summary>
        public const string High = DefaultOrderings.High;
    }
}
