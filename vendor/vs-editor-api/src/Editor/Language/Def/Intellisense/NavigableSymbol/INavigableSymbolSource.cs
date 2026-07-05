// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a source that provides <see cref="INavigableSymbol"/> over a text buffer of a given content type.
    /// </summary>
    /// <remarks>
    /// Providers implement <see cref="INavigableSymbolSource"/> and expose instances of this type via MEF export <see cref="INavigableSymbolSourceProvider"/>.  
    /// </remarks>
    public interface INavigableSymbolSource : IDisposable
    {
        /// <summary>
        /// Asynchronously gets an <see cref="INavigableSymbol"/> at the trigger span position.
        /// </summary>
        /// <param name="triggerSpan">A 1-character length span over which navigable symbol is queried.</param>
        /// <param name="token">A <see cref="CancellationToken"/> used to cancel the task as needed.</param>
        /// <returns>
        /// A task that returns <see cref="INavigableSymbol"/> upon completion.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This async method is called on background thread.
        /// </para>
        /// <para>
        /// The <paramref name="triggerSpan"/> is a 1-character span containing the character over which a symbol is queried.
        /// This is to disambiguate the case in a projection buffer where the trigger point is between two buffer boundaries
        /// and thus multiple symbol sources may be queried. A span eliminates this ambiguity because it can only fall in one buffer.
        /// </para>
        /// <para>
        /// Providers may return a <see cref="Task"/> with null results if no navigable symbol is available over the queried <paramref name="triggerSpan"/>.
        /// </para>
        /// </remarks>
        Task<INavigableSymbol> GetNavigableSymbolAsync(SnapshotSpan triggerSpan, CancellationToken token);
    }
}
