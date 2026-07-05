//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Formatting.IFormattedLine").
//  Signature-adapted per PLAN §4.2: the WPF Visual becomes Avalonia.Visual and
//  Rect becomes Avalonia.Rect.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using Avalonia;

    /// <summary>
    /// A formatted line produced by an <see cref="IFormattedLineSource"/>, owning the visual
    /// used to render it. The mutating members (SetTop, SetSnapshot, SetVisibleArea, ...) are
    /// for use by the view's layout engine only.
    /// </summary>
    public interface IFormattedLine : IWpfTextViewLine
    {
        /// <summary>
        /// Gets the visual that renders this line, creating it if it does not exist.
        /// </summary>
        Visual GetOrCreateVisual();

        /// <summary>
        /// Releases the visual used to render this line.
        /// </summary>
        void RemoveVisual();

        /// <summary>
        /// Sets the area of the view in which this line could be displayed (<see cref="IWpfTextViewLine.VisibleArea"/>).
        /// </summary>
        void SetVisibleArea(Rect visibleArea);
    }
}
