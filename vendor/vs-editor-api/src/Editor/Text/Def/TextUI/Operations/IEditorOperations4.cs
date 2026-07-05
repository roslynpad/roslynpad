using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.Text.Operations
{
    /// <summary>
    /// Defines operations relating to the editor, in addition to operations defined by <see cref="IEditorOperations3"/>.
    /// </summary>
    internal interface IEditorOperations4 : IEditorOperations3
    {
        /// <summary>
        /// Returns a string with the original content except for newlines, which are replaced to match the document
        /// </summary>
        /// <param name="text">Text to normalize newlines</param>
        /// <returns>The normalized string, if the document has enough information to normalize with. The original string otherwise.</returns>
        /// <remarks>This method uses the newline state associated with the document buffer.</remarks>
        string NormalizeNewlinesInString(string text);

        /// <summary>
        /// Determines whether zooming operations are possible.
        /// </summary>
        bool CanZoomTo { get; }

        /// <summary>
        /// Determines whether a zoom-in operation is possible.
        /// </summary>
        bool CanZoomIn { get; }

        /// <summary>
        /// Determines whether a zoom-out operation is possible.
        /// </summary>
        bool CanZoomOut { get; }

        /// <summary>
        /// Determines whether resetting zoom to 100% operation is possible.
        /// </summary>
        bool CanZoomReset { get; }

        /// <summary>
        /// Resets the text view zoom level to 100%.
        /// </summary>
        void ZoomReset();

        /// <summary>
        /// Sorts the selected lines in alphabetical order.
        /// </summary>
        void SortSelectedLines();

        /// <summary>
        /// Joins the selected lines into a single one.
        /// </summary>
        void JoinSelectedLines();
    }
}
