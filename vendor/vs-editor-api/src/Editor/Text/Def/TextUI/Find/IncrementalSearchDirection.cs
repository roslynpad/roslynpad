//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.IncrementalSearch
{
    /// <summary>
    /// Determines the direction of the incremental search.
    /// See <see cref="IIncrementalSearch"/> for more information.
    /// </summary>
    public enum IncrementalSearchDirection
    {
        /// <summary>
        /// Forward search.
        /// </summary>
        Forward,

        /// <summary>
        ///Backward search.
        /// </summary>
        Backward
    }
}