//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.IAdornmentLayer").
//  UIElement becomes Control per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.ObjectModel;
    using Avalonia.Controls;

    using Microsoft.VisualStudio.Text;

    /// <summary>
    /// Represents an adornment layer in an <see cref="IWpfTextView"/>. Layers are declared with
    /// <see cref="AdornmentLayerDefinition"/> exports and obtained via
    /// <see cref="IWpfTextView.GetAdornmentLayer"/>.
    /// </summary>
    public interface IAdornmentLayer
    {
        /// <summary>
        /// Gets the view to which this layer belongs.
        /// </summary>
        IWpfTextView TextView { get; }

        /// <summary>
        /// Gets the elements currently in this layer.
        /// </summary>
        ReadOnlyCollection<IAdornmentLayerElement> Elements { get; }

        /// <summary>
        /// Determines whether this layer contains no adornments.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets or sets the opacity of the whole layer.
        /// </summary>
        double Opacity { get; set; }

        /// <summary>
        /// Adds a text-relative adornment with the given visual span and no removal callback.
        /// </summary>
        /// <returns>true if the adornment was added (its span intersects the rendered text), false otherwise.</returns>
        bool AddAdornment(SnapshotSpan visualSpan, object tag, Control adornment);

        /// <summary>
        /// Adds an adornment to the layer.
        /// </summary>
        /// <param name="behavior">The positioning behavior.</param>
        /// <param name="visualSpan">The span with which the adornment is associated; required for text-relative behaviors.</param>
        /// <param name="tag">A tag by which the adornment can be removed later; may be null.</param>
        /// <param name="adornment">The adornment control.</param>
        /// <param name="removedCallback">Invoked when the adornment is removed; may be null.</param>
        /// <returns>true if the adornment was added, false if its visual span does not intersect the rendered text.</returns>
        bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag, Control adornment, AdornmentRemovedCallback removedCallback);

        /// <summary>
        /// Removes the given adornment from the layer.
        /// </summary>
        void RemoveAdornment(Control adornment);

        /// <summary>
        /// Removes all adornments with the given tag.
        /// </summary>
        void RemoveAdornmentsByTag(object tag);

        /// <summary>
        /// Removes all adornments whose visual spans intersect the given span.
        /// </summary>
        void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan);

        /// <summary>
        /// Removes all adornments in the layer.
        /// </summary>
        void RemoveAllAdornments();

        /// <summary>
        /// Removes all adornments matching the given predicate.
        /// </summary>
        void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match);

        /// <summary>
        /// Removes the adornments matching the given predicate whose visual spans intersect the given span.
        /// </summary>
        void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match);
    }
}
