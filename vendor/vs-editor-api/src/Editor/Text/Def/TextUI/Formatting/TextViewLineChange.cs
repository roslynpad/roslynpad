//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    /// <summary>
    /// Defines the possible types of change in a rendered text line between one layout and another.
    /// </summary>
    public enum TextViewLineChange
    {
        /// <summary>
        /// No change type is specified.
        /// </summary>
        None,

        /// <summary>
        /// The line is new or reformatted.
        /// </summary>
        NewOrReformatted,

        /// <summary>
        /// The text has not changed, but some change has caused the y-coordinate to change. For example,
        /// a line was inserted above this line, or the user scrolled the view up or down.
        /// </summary>
        Translated
    };
}