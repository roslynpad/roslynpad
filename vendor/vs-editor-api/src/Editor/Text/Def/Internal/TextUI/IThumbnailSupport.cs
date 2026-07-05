//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    public interface IThumbnailSupport
    {
        /// <summary>
        /// Controls whether or note the view's visuals are removed when the view is hidden.
        /// </summary>
        /// <remarks>
        /// Defaults to true and is should to false when generating thumbnails of a hidden view (then restored afterwards).
        /// </remarks>
        bool RemoveVisualsWhenHidden { get; set; }
    }
}
