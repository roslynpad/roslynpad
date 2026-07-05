//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// The types of differences.
    /// 
    /// </summary>
    /// <remarks>Differences are read from left to right, so that Add means that only
    /// the right span has text, Remove means that only the left span has text, and
    /// Change means that both the left and right spans have text.</remarks>
    public enum DifferenceType
    {
        /// <summary>
        /// Lines were added, so the text is on the right-hand side
        /// </summary>
        Add,
        /// <summary>
        /// Lines were removed, so the text is on the left-hand side
        /// </summary>
        Remove,
        /// <summary>
        /// Lines were changed, so the text is on both sides
        /// </summary>
        Change
    }
}
        
