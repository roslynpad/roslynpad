using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data
{
    /// <summary>
    /// Used by <see cref="IAsyncCompletionItemManager" /> to recommend the selection mode.
    /// </summary>
    public enum UpdateSelectionHint
    {
        /// <summary>
        /// Don't change the current selection mode. This is the recommended value.
        /// </summary>
        NoChange,

        /// <summary>
        /// Set selection mode to soft selection: item is committed only using Tab or mouse.
        /// </summary>
        SoftSelected,

        /// <summary>
        /// Set selection mode to regular selection: item is committed using Tab, mouse, enter and commit characters.
        /// </summary>
        Selected
    }
}
