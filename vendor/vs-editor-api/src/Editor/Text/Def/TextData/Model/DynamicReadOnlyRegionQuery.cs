//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// The callback delegate for notifying read only regions of edits.
    /// </summary>
    /// <param name="isEdit">True if an edit is being attempted. False if the read-only check should be side-effect free.</param>
    /// <returns>Whether the read-only region is in effect.</returns>
    public delegate bool DynamicReadOnlyRegionQuery(bool isEdit);
}
