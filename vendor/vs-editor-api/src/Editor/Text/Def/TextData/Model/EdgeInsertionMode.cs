//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Specifies the edge insertion modes for read-only regions.
    /// </summary>
    public enum EdgeInsertionMode
    {
        /// <summary>
        /// Allows insertions at the edge of read-only regions. If
        /// there is a read-only region [3, 6) that allows edge insertions, an insertion at
        /// position 3 or position 6 will succeed.
        /// </summary>
        Allow,

        /// <summary>
        /// Prevents insertions at the edge of read-only regions. If
        /// there is a read-only region [3, 6) that allows edge insertions, an insertion at
        /// position 3 or position 6 will fail.
        /// </summary>
        Deny
    }
}
