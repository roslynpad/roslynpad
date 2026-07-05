//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;

    /// <summary>
    /// Represents margins that are attached to an edge of an <see cref="ITextView"/>.
    /// </summary>
    public interface ITextViewMargin : IDisposable
    {
        /// <summary>
        /// Gets the size of the margin. 
        /// </summary>
        /// <remarks>For a horizontal margin this is the height of the margin, 
        /// since the width will be determined by the <see cref="ITextView"/>. 
        /// For a vertical margin this is the width of the margin, since the height will be determined by the <see cref="ITextView"/>.</remarks>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        double MarginSize { get; }

        /// <summary>
        /// Determines whether the margin is enabled.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        bool Enabled { get; }

        /// <summary>
        /// Gets the <see cref="ITextViewMargin"/> with the given <paramref name="marginName"/>.
        /// </summary>
        /// <param name="marginName">The name of the <see cref="ITextViewMargin"/>.</param>
        /// <returns>The <see cref="ITextViewMargin"/> named <paramref name="marginName"/>, or null if no match is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="marginName"/> is null.</exception>
        /// <remarks>A margin returns itself if it is passed its own name. If the name does not match and it is a container margin, it
        /// forwards the call to its children. Margin name comparisons are case-insensitive.</remarks>
        ITextViewMargin GetTextViewMargin(string marginName);
    }
}
