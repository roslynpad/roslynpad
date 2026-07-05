//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.
//  BackgroundBrushChangedEventArgs"). Brush becomes IBrush per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Avalonia.Media;

    /// <summary>
    /// Provides information for the <see cref="IWpfTextView.BackgroundBrushChanged"/> event.
    /// </summary>
    public class BackgroundBrushChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance with the new background brush.
        /// </summary>
        public BackgroundBrushChangedEventArgs(IBrush newBackgroundBrush)
        {
            if (newBackgroundBrush == null)
                throw new ArgumentNullException(nameof(newBackgroundBrush));
            NewBackgroundBrush = newBackgroundBrush;
        }

        /// <summary>
        /// Gets the new background brush of the view's text area.
        /// </summary>
        public IBrush NewBackgroundBrush { get; }
    }
}
