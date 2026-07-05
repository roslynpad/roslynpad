using System.Composition;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>
/// Resolves <see cref="ImageElement"/>s (quick info symbol glyphs, completion icons) against
/// the VS image-catalog glyphs in <see cref="ImageCatalog"/>, superseding Morgania's
/// fixed-size placeholder factory. Unknown ids keep the placeholder so layout is preserved.
/// </summary>
[Export(typeof(IViewElementFactory))]
[Name("Morgania ImageElement to Control")]
[TypeConversion(from: typeof(ImageElement), to: typeof(Control))]
[Order(Before = "default ImageElement to Control")]
internal sealed class ImageElementViewElementFactory : IViewElementFactory
{
    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        if (model is not ImageElement image)
        {
            throw new ArgumentException($"Unsupported conversion from {model?.GetType().FullName}.", nameof(model));
        }

        // The right margin keeps inline icons (quick info's symbol glyph) clear of the text
        // that follows; composers that manage their own spacing (the completion list)
        // overwrite Margin on the returned control.
        Control control = ImageCatalog.GetImage(image.ImageId.Guid, image.ImageId.Id) is { } source
            ? new Image
            {
                Width = 16.0,
                Height = 16.0,
                Source = source,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
            }
            : new Border
            {
                Width = 16.0,
                Height = 16.0,
                Background = Brushes.Transparent,
                Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
                VerticalAlignment = VerticalAlignment.Center,
            };
        if (image.AutomationName is { } name)
        {
            AutomationProperties.SetName(control, name);
        }

        return control as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }
}
