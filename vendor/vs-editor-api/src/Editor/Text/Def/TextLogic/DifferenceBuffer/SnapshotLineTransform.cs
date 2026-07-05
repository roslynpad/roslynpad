//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A custom transform for text snapshot lines, to allow clients of <see cref="IDifferenceBuffer"/> to modify lines before
    /// performing any comparison.
    /// </summary>
    /// <param name="line">The original snapshot line that this transform is being asked to operate on.</param>
    /// <param name="currentText">The current text of the line, which may differ from <see cref="ITextSnapshotLine.GetText"/> if an
    /// earlier transform has already processed the line.</param>
    /// <returns>The new line text to use for the line.</returns>
    public delegate string SnapshotLineTransform(ITextSnapshotLine line, string currentText);
}
