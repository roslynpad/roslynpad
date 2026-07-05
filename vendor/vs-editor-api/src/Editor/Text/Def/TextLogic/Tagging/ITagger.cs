//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A provider of tags over a buffer.
    /// </summary>
    /// <typeparam name="T">The type of tags to generate.</typeparam>
    public interface ITagger<out T> where T : ITag
    {
        /// <summary>
        /// Gets all the tags that intersect the <paramref name="spans"/>.
        /// </summary>
        /// <param name="spans">The spans to visit.</param>
        /// <returns>A <see cref="ITagSpan&lt;T&gt;"/> for each tag.</returns>
        /// <remarks>
        /// <para>Taggers are not required to return their tags in any specific order.</para>
        /// <para>The recommended way to implement this method is by using generators ("yield return"),
        /// which allows lazy evaluation of the entire tagging stack.</para>
        /// </remarks>
        IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans);

        /// <summary>
        /// Occurs when tags are added to or removed from the provider.
        /// </summary>
        event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
