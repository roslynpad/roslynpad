//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;

    using Microsoft.VisualStudio.Text.Adornments;

    /// <summary>
    /// An implementation of <see cref="IErrorTag" />.
    /// </summary>
    public class ErrorTag : IErrorTag
    {
        /// <summary>
        /// Initializes a new instance of a <see cref="ErrorTag"/> of the specified type.
        /// </summary>
        /// <param name="errorType">The type of error to use.</param>
        /// <param name="toolTipContent">The tooltip content to display. May be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="errorType"/> is null.</exception>
        public ErrorTag(string errorType, object toolTipContent)
        {
            if (errorType == null)
                throw new ArgumentNullException(nameof(errorType));
            
            ErrorType = errorType;
            ToolTipContent = toolTipContent;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="ErrorTag"/> of the specified type with no tooltip content.
        /// </summary>
        /// <param name="errorType">The type of error to use,</param>
        public ErrorTag(string errorType) : this(errorType, null) { }

        /// <summary>
        /// Initializes a new instance of a <see cref="ErrorTag"/> of type SyntaxError with no tooltip content.
        /// </summary>
        public ErrorTag() : this(PredefinedErrorTypeNames.SyntaxError, null) { }

        /// <summary>
        /// Gets the type of error to use.
        /// </summary>
        public string ErrorType { get; private set; }

        /// <summary>
        /// Gets the content to use when displaying a tooltip for this error.
        /// This property may be null.
        /// </summary>
        public object ToolTipContent { get; private set; }
    }
}
