//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Defines a header object for <see cref="CodeLensDetailsDescriptor"/>.
    /// </summary>
    public sealed class CodeLensDetailHeaderDescriptor
    {
        /// <summary>
        /// The header's unique name.
        /// </summary>
        public string UniqueName { get; set; }

        /// <summary>
        /// The localized name of the header when displayed.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The desired width of this header when displayed in the detail popup.
        /// </summary>
        /// <remarks>
        /// If <see cref="Width"/> &lt;= 1.0, this value is used as a multifier
        /// for the percentage of remaining width in the grid view, excluding all fixed width columns,
        /// should be allocated to this column. A value of 1.0 means 100% of remaining width is allocated to this column.
        /// </remarks>
        public double Width { get; set; }

        /// <summary>
        /// Indicates whether this column should display in the grid view.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }
}
