//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Represents the text marker tag, which is used to place text marker adornments on a view.
    /// </summary>
    public interface ITextMarkerTag : ITag
    {
        /// <summary>
        /// Gets the type of adornment to use.
        /// </summary>
        string Type { get; }
    }
}
