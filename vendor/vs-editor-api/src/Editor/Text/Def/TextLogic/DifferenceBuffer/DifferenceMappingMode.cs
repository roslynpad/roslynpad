//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The mapping modes that can be used when mapping points inside a difference between the left and right snapshots.
    /// </summary>
    public enum DifferenceMappingMode
    {
        /// <summary>
        /// Map any point in a difference to the start of the corresponding difference in the other snapshot.
        /// </summary>
        Start,

        /// <summary>
        /// Map any point in a difference to the corresponding line/column of the corresponding difference in the other snapshot.
        /// </summary>
        /// <remarks>
        /// If the difference is in the other snapshot doesn't have a corresponding line the point will be mapped to the end of the difference.
        /// If the column if greater than the length of the corresponding line in the other snapshpt, then the point will be mapped to the end of the corresponding line.
        /// </remarks>
        LineColumn,

        /// <summary>
        /// Map any point in a difference to the end of the corresponding difference in the other snapshot.
        /// </summary>
        End
    }
}
