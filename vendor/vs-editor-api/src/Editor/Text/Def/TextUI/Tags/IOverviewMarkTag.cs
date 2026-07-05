//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Provides the information needed to render a mark in the overview margin.
    /// </summary>
    public interface IOverviewMarkTag : ITag
    {
        /// <summary>
        /// Gets the name of the EditorFormatDefinition whose background color is used to draw the mark.
        /// </summary>
        string MarkKindName { get; }
    }
}
