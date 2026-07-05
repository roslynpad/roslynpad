//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation of the WPF text-view contract (PLAN §3.3/§5.4:
//  recreated from public documentation, learn.microsoft.com
//  "Microsoft.VisualStudio.Text.Editor.IWpfTextView"). Signature-adapted per
//  PLAN §4.2: FrameworkElement becomes Control, Brush becomes IBrush. The name
//  "Wpf" is retained as a historical label; it is part of the contract
//  identity that Roslyn's editor layer compiles against (PLAN §4, ADR-002).
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using Avalonia.Controls;
    using Avalonia.Media;

    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Formatting;

    /// <summary>
    /// Represents the Avalonia-rendered text view.
    /// </summary>
    public interface IWpfTextView : ITextView
    {
        /// <summary>
        /// Gets or sets the background of the view's text area.
        /// </summary>
        IBrush Background { get; set; }

        /// <summary>
        /// Occurs when the <see cref="Background"/> brush changes.
        /// </summary>
        event EventHandler<BackgroundBrushChangedEventArgs> BackgroundBrushChanged;

        /// <summary>
        /// Gets the <see cref="IFormattedLineSource"/> used to format text in this view.
        /// May change on layout; do not cache across layouts.
        /// </summary>
        IFormattedLineSource FormattedLineSource { get; }

        /// <summary>
        /// Gets the <see cref="ILineTransformSource"/> used in this view's layouts.
        /// </summary>
        ILineTransformSource LineTransformSource { get; }

        /// <summary>
        /// Gets the control that renders the view.
        /// </summary>
        Control VisualElement { get; }

        /// <summary>
        /// Gets or sets the zoom level applied to the view, as a percentage (100.0 is unzoomed).
        /// </summary>
        double ZoomLevel { get; set; }

        /// <summary>
        /// Occurs when the <see cref="ZoomLevel"/> changes.
        /// </summary>
        event EventHandler<ZoomLevelChangedEventArgs> ZoomLevelChanged;

        /// <summary>
        /// Gets the named adornment layer. The layer must have been declared with an
        /// <see cref="AdornmentLayerDefinition"/> export.
        /// </summary>
        IAdornmentLayer GetAdornmentLayer(string name);

        /// <summary>
        /// Gets the text view lines rendered in this view as <see cref="IWpfTextViewLine"/> objects.
        /// </summary>
        new IWpfTextViewLineCollection TextViewLines { get; }

        /// <summary>
        /// Gets the <see cref="IWpfTextViewLine"/> containing the given buffer position, formatting
        /// it if it is not in the rendered collection.
        /// </summary>
        new IWpfTextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);
    }
}
