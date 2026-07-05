//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System.Collections.Generic;

    /// <summary>
    /// A normalized list of <see cref="ITextChange"/> objects. Changes are sorted in ascending order of position,
    /// and abutting and overlapping changes are combined into a single change.
    /// </summary>
    /// <remarks>
    /// <see cref="INormalizedTextChangeCollection"/> objects are immutable.
    /// </remarks>
    public interface INormalizedTextChangeCollection : IList<ITextChange>
    {
        /// <summary>
        /// Determines whether any of the <see cref="ITextChange"/> objects in this list have a nonzero <see cref="ITextChange.LineCountDelta"/>.
        /// </summary>
        bool IncludesLineChanges { get; }
    }
}
