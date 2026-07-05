//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Defines a set of actions that are predefined for manipulating selections within a view. For custom manipulations see the usage
    /// of <see cref="ISelectionTransformer"/>. These transformations can be passed in to
    /// <see cref="IMultiSelectionBroker.PerformActionOnAllSelections(PredefinedSelectionTransformations)"/>,
    /// <see cref="IMultiSelectionBroker.TryPerformActionOnSelection(Selection, PredefinedSelectionTransformations, out Selection)"/>,
    /// and <see cref="ISelectionTransformer.PerformAction(PredefinedSelectionTransformations)"/>.
    /// </summary>
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
    public enum PredefinedSelectionTransformations
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
    {
        /// <summary>
        /// Resets the active and anchor points to be at the insertion point.
        /// </summary>
        ClearSelection,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead one position in the view.
        /// </summary>
        MoveToNextCaretPosition,

        /// <summary>
        /// Moves the active and insertion points ahead one position in the view, keeping the anchor point where it is.
        /// </summary>
        SelectToNextCaretPosition,

        /// <summary>
        /// Moves the active, anchor, and insertion points back one position in the view.
        /// </summary>
        MoveToPreviousCaretPosition,

        /// <summary>
        /// Moves the active and insertion points back one position in the view, keeping the anchor point where it is.
        /// </summary>
        SelectToPreviousCaretPosition,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead to the beginning of the next word.
        /// </summary>
        MoveToNextWord,

        /// <summary>
        /// Moves the active and insertion points ahead to the beginning of the next word, keeping the anchor point where it is.
        /// </summary>
        SelectToNextWord,

        /// <summary>
        /// Moves the active, anchor, and insertion points back to the end of the previous word.
        /// </summary>
        MoveToPreviousWord,

        /// <summary>
        /// Moves the active and insertion points back to the end of the previous word, keeping the anchor point where it is.
        /// </summary>
        SelectToPreviousWord,

        /// <summary>
        /// Moves the active, anchor, and insertion points back to the beginning of the current line.
        /// </summary>
        MoveToBeginningOfLine,

        /// <summary>
        /// Moves the active and insertion points back to the beginning of the current line, keeping the anchor point where it is.
        /// </summary>
        SelectToBeginningOfLine,

        /// <summary>
        /// Moves the active, anchor, and insertion points alternately between the beginning of the line, and the first non-whitespace character.
        /// </summary>
        MoveToHome,

        /// <summary>
        /// Moves the active and insertion points alternately between the beginning of the line, and the first non-whitespace character, keeping the anchor point where it is.
        /// </summary>
        SelectToHome,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead to the end of the current line.
        /// </summary>
        MoveToEndOfLine,

        /// <summary>
        /// Moves the active and insertion points ahead to the end of the current line, keeping the anchor point where it is.
        /// </summary>
        SelectToEndOfLine,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead to next line, staying as close to the user's preferred x-coordinate in the view as possible.
        /// </summary>
        MoveToNextLine,

        /// <summary>
        /// Moves the active and insertion points ahead to next line, staying as close to the user's preferred x-coordinate in the view as possible, keeping the anchor point where it is.
        /// </summary>
        SelectToNextLine,

        /// <summary>
        /// Moves the active, anchor, and insertion points back to the previous line, staying as close to the user's preferred x-coordinate in the view as possible.
        /// </summary>
        MoveToPreviousLine,

        /// <summary>
        /// Moves the active and insertion points back to the previous line, staying as close to the user's preferred x-coordinate in the view as possible, keeping the anchor point where it is.
        /// </summary>
        SelectToPreviousLine,

        /// <summary>
        /// Moves the active, anchor, and insertion points back one viewport height, staying as close to the user's preferred x and y coordinates in the view as possible.
        /// </summary>
        MovePageUp,

        /// <summary>
        /// Moves the active and insertion points back one viewport height, staying as close to the user's preferred x and y coordinates in the view as possible, keeping the anchor point where it is.
        /// </summary>
        SelectPageUp,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead one viewport height, staying as close to the user's preferred x and y coordinates in the view as possible.
        /// </summary>
        MovePageDown,

        /// <summary>
        /// Moves the active and insertion points ahead one viewport height, staying as close to the user's preferred x and y coordinates in the view as possible, keeping the anchor point where it is.
        /// </summary>
        SelectPageDown,

        /// <summary>
        /// Moves the active, anchor, and insertion points back to the beginning of the document.
        /// </summary>
        MoveToStartOfDocument,

        /// <summary>
        /// Moves the active and insertion points back to the beginning of the document, keeping the anchor point where it is.
        /// </summary>
        SelectToStartOfDocument,

        /// <summary>
        /// Moves the active, anchor, and insertion points ahead to the end of the document.
        /// </summary>
        MoveToEndOfDocument,

        /// <summary>
        /// Moves the active and insertion points ahead to the end of the document, keeping the anchor point where it is.
        /// </summary>
        SelectToEndOfDocument,

        /// <summary>
        /// Moves the anchor point to the beginning of the current word. Moves the active and insertion points to the end of the current word.
        /// </summary>
        SelectCurrentWord
    }
}
