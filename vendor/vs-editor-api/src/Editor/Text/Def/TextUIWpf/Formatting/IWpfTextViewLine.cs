//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation of the WPF view-line contract (PLAN §3.3/§5.4:
//  recreated from public documentation, learn.microsoft.com
//  "Microsoft.VisualStudio.Text.Formatting.IWpfTextViewLine"). Signature-adapted
//  per PLAN §4.2: WPF Rect/TextRunProperties become Avalonia Rect/TextRunProperties.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using Avalonia;
    using Avalonia.Media.TextFormatting;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents text that has been formatted for display in the Avalonia-rendered text view.
    /// The name preserves the original WPF contract identity (PLAN §4, ADR-002).
    /// </summary>
    public interface IWpfTextViewLine : ITextViewLine
    {
        /// <summary>
        /// Gets the area of the view in which this line could be displayed, in the text rendering
        /// coordinate system. Used to decide how much of the line needs to be rendered.
        /// </summary>
        Rect VisibleArea { get; }

        /// <summary>
        /// Gets the formatting properties in effect for the character at the given buffer position.
        /// </summary>
        TextRunProperties GetCharacterFormatting(SnapshotPoint bufferPosition);
    }
}
