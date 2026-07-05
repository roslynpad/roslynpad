//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// A mutable sequence of Unicode characters encoded using UTF-16.
    /// Positions within the buffer are treated as a sequence of characters (starting at character zero) or
    /// as a sequence of lines (starting at line zero). An empty buffer has a single line containing no characters.
    /// </summary>
    /// <remarks>Any <see cref="ITextBuffer"/> will be upcastable to an <see cref="ITextBuffer2"/>.</remarks>
    public interface ITextBuffer2 : ITextBuffer
    {
        /// <summary>
        /// Occurs when a non-empty <see cref="ITextEdit"/> is successfully applied.
        /// This is raised on a background thread. Listeners are expected to schedule any expensive
        /// work to be done asynchronously outside of this thread.
        /// </summary>
        /// <remarks>
        /// Listeners of this event are not expected to modify the buffer. For performance reasons
        /// handlers that modify the buffer should listen to <see cref="ITextBuffer.ChangedHighPriority"/> event
        /// instead.
        /// <para>
        /// This event is raised after <see cref="ITextBuffer.ChangedHighPriority"/> event.
        /// It's guaranteed that individual listeners receive the <see cref="ChangedOnBackground"/> events 
        /// in a synchronized way (never on more than one thread at a time) and in the order
        /// the edits were applied.
        /// </para>
        /// </remarks>
        event EventHandler<TextContentChangedEventArgs> ChangedOnBackground;
    }
}
