//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.
//  IAdornmentLayerElement"). UIElement becomes Control per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using Avalonia.Controls;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// An adornment and its placement metadata in an <see cref="IAdornmentLayer"/>.
    /// </summary>
    public interface IAdornmentLayerElement
    {
        /// <summary>
        /// Gets the adornment control.
        /// </summary>
        Control Adornment { get; }

        /// <summary>
        /// Gets the positioning behavior of the adornment.
        /// </summary>
        AdornmentPositioningBehavior Behavior { get; }

        /// <summary>
        /// Gets the callback invoked when the adornment is removed, or null.
        /// </summary>
        AdornmentRemovedCallback RemovedCallback { get; }

        /// <summary>
        /// Gets the tag associated with the adornment, or null.
        /// </summary>
        object Tag { get; }

        /// <summary>
        /// Gets the span with which the adornment is associated, or null for adornments
        /// that are not associated with text.
        /// </summary>
        SnapshotSpan? VisualSpan { get; }
    }
}
