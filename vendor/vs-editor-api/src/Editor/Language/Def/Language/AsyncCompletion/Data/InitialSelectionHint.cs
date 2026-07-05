using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Used by <see cref="IAsyncCompletionSource"/> to recommend the selection mode.
    /// </summary>
    public enum InitialSelectionHint
    {
        /// <summary>
        /// Item is selected.
        /// It will be committed by pressing a commit character, e.g. a token delimeter,
        /// Tab, Enter and mouse click.
        /// When multiple <see cref="IAsyncCompletionSource"/> give different results, this value has the lowest priority.
        /// </summary>
        RegularSelection,

        /// <summary>
        /// Item is soft selected.
        /// It will be committed only by pressing Tab or clicking the item.
        /// Typing a commit character will dismiss the <see cref="IAsyncCompletionSession"/>.
        /// Selecting another item automatically disables soft selection and enables regular selection.
        /// When multiple <see cref="IAsyncCompletionSource"/> give different results, this value has higher priority than <see cref="RegularSelection"/>.
        /// </summary>
        SoftSelection,
    }
}
