//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.IWpfTextViewHost").
//  Control replaces the WPF host element per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Avalonia.Controls;

    /// <summary>
    /// Hosts an <see cref="IWpfTextView"/> together with its margins.
    /// </summary>
    public interface IWpfTextViewHost
    {
        /// <summary>
        /// Closes the host and its view.
        /// </summary>
        /// <exception cref="InvalidOperationException">The host is already closed.</exception>
        void Close();

        /// <summary>
        /// Determines whether this host has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets the named <see cref="IWpfTextViewMargin"/>, or null if it is not present.
        /// </summary>
        IWpfTextViewMargin GetTextViewMargin(string marginName);

        /// <summary>
        /// Gets the control containing the view and its margins.
        /// </summary>
        Control HostControl { get; }

        /// <summary>
        /// Gets the hosted text view.
        /// </summary>
        IWpfTextView TextView { get; }

        /// <summary>
        /// Occurs immediately after the host is closed.
        /// </summary>
        event EventHandler Closed;
    }
}
