//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

using Microsoft.VisualStudio.Text.Formatting;

namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Provides UI specific properties about an <see cref="Selection"/>.
    /// </summary>
    public abstract class AbstractSelectionPresentationProperties
    {
        /// <summary>
        /// Gets the position that the caret prefers to occupy on a given line. This position may not be honored
        /// if virtual space is off and the line is insufficiently long. See <see cref="CaretBounds"/> for the
        /// actual location.
        /// </summary>
        public virtual double PreferredXCoordinate { get; protected set; }

        /// <summary>
        /// Gets the position that the caret prefers to occupy vertically in the view. This position is used for operations
        /// such as page up/down, but may not be honored if there is an adornment at the desired location. See
        /// <see cref="CaretBounds"/> for the actual location.
        /// </summary>
        public virtual double PreferredYCoordinate { get; protected set; }

        /// <summary>
        /// Gets the caret location and size.
        /// </summary>
        public virtual TextBounds CaretBounds { get; }

        /// <summary>
        /// Gets whether the caret is shown in its entirety on the screen.
        /// </summary>
        public virtual bool IsWithinViewport { get; }

        /// <summary>
        /// Gets whether the caret should be rendered as overwrite.
        /// </summary>
        public virtual bool IsOverwriteMode { get; }

        /// <summary>
        /// Gets the <see cref="ITextViewLine"/> that contains the <see cref="Selection.InsertionPoint"/>.
        /// </summary>
        public virtual ITextViewLine ContainingTextViewLine { get; }

        /// <summary>
        /// Tries to get the <see cref="ITextViewLine"/> that contains the <see cref="Selection.InsertionPoint"/>.
        /// This can fail if the call happens during a view layout or after the view is closed.
        /// </summary>
        /// <param name="line">Returns out the requested line if available, or null otherwise.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public abstract bool TryGetContainingTextViewLine(out ITextViewLine line);
    }
}
