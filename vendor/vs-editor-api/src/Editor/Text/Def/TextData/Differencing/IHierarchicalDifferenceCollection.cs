//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// Represents a difference collection in which the left and right
    /// sequences are <see cref="ITokenizedStringList"/> objects, and each difference may itself contain
    /// an <see cref="IHierarchicalDifferenceCollection"/>.
    /// </summary>
    /// <remarks>You can get this collection by using the <see cref="IHierarchicalStringDifferenceService" />. 
    /// When you request multiple types of string differencing
    /// (e.g. line and word), the first level of differences will be the lines,
    /// and each line difference may contain an <see cref="IHierarchicalDifferenceCollection" />
    /// of word differences. See <see cref="IHierarchicalStringDifferenceService" />
    /// for more information and examples.</remarks>
    public interface IHierarchicalDifferenceCollection : IDifferenceCollection<string>
    {
        /// <summary>
        /// Gets the original left tokenized list.
        /// </summary>
        /// <remarks>
        /// This is the same as IDifferenceCollection.LeftSequence, except that
        /// it is typed as an <see cref="ITokenizedStringList"/>.
        /// </remarks>
        ITokenizedStringList LeftDecomposition
        {
            get;
        }

        /// <summary>
        /// Get the original right tokenized list.
        /// </summary>
        /// <remarks>
        /// This is the same as IDifferenceCollection.RightSequence, except that
        /// it is typed as an <see cref="ITokenizedStringList"/>.
        /// </remarks>
        ITokenizedStringList RightDecomposition
        {
            get;
        }

        /// <summary>
        /// Gets the contained difference collection for the given element, if
        /// it has any.  This forces an evaluation of the contained differences.
        /// </summary> 
        /// <param name="index">The index at which to compute the contained differences.</param>
        /// <returns>The contained differences at this level, or <c>null</c> if there are none.</returns>
        IHierarchicalDifferenceCollection GetContainedDifferences(int index);

        /// <summary>
        /// Determines whether or not the <see cref="Difference"/> at the given index itself contains differences.  This forces an evaluation of the contained differences for the given element.
        /// </summary>
        /// <param name="index">The index at which to check for contained differences.</param>
        /// <returns><c>true</c> if the <see cref="Difference"/> in question has contained differences, otherwise <c>false</c>.</returns>
        bool HasContainedDifferences(int index);
    }
}
