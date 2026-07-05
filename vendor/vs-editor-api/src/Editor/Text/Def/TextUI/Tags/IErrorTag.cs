//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Represents an error, which is used to place squiggle adornments on the view.
    /// </summary>
    public interface IErrorTag : ITag
    {
        /// <summary>
        /// Gets the type of error to use.
        /// </summary>
        string ErrorType { get; }

        /// <summary>
        /// Gets the content to use when displaying a tooltip for this error.
        /// This property may be null.
        /// </summary>
        object ToolTipContent { get; }
    }
}
