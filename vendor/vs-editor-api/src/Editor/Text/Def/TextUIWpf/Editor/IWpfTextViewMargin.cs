//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.IWpfTextViewMargin").
//  FrameworkElement becomes Control per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using Avalonia.Controls;

    /// <summary>
    /// Represents a margin attached to an edge of an <see cref="IWpfTextView"/>.
    /// </summary>
    public interface IWpfTextViewMargin : ITextViewMargin
    {
        /// <summary>
        /// Gets the control that renders the margin.
        /// </summary>
        Control VisualElement { get; }
    }
}
