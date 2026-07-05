//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.
//  AdornmentRemovedCallback"). UIElement becomes Control per PLAN §4.2.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using Avalonia.Controls;

    /// <summary>
    /// Called when an adornment has been removed from an <see cref="IAdornmentLayer"/>.
    /// </summary>
    /// <param name="tag">The tag associated with the adornment.</param>
    /// <param name="element">The adornment removed from the view.</param>
    public delegate void AdornmentRemovedCallback(object tag, Control element);
}
