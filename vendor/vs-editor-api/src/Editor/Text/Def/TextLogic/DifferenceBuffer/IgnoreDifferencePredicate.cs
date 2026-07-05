//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A custom predicate that the <see cref="IDifferenceBuffer"/> uses to selectively ignore differences.
    /// </summary>
    /// <param name="lineDifference">The lines that have changed.  The <see cref="Difference.Left"/> and <see cref="Difference.Right"/> spans
    /// are line numbers in the <paramref name="leftSnapshot"/> and <paramref name="rightSnapshot"/>, respectively.</param>
    /// <param name="leftSnapshot">The snapshot of the <see cref="IDifferenceBuffer.LeftBuffer"/> being compared.</param>
    /// <param name="rightSnapshot">The snapshot of the <see cref="IDifferenceBuffer.RightBuffer"/> being compared.</param>
    /// <returns><c>true</c> to ignore the given difference, <c>false</c> to include it in the list of differences.</returns>
    public delegate bool IgnoreDifferencePredicate(Difference lineDifference, ITextSnapshot leftSnapshot, ITextSnapshot rightSnapshot);
}
