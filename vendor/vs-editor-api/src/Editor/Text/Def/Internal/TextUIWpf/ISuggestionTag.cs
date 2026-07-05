//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Represents a suggestion tag, which is consumed by the suggestion margin
    /// to place suggestion visual element such as a Light Bulb.
    /// </summary>
    public interface ISuggestionTag : ITag
    {
    }
}
