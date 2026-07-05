#nullable enable

namespace Microsoft.VisualStudio.Text.Adornments.Implementation;

using System.Composition;

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The default content-model conversions the platform guarantees: classified text (colored
/// through the view's classification format map), containers (stacked or wrapped), images
/// (a fixed-size placeholder — Morgania has no proprietary image catalog), and the
/// ToString() fallback for arbitrary objects.
/// </summary>
internal static class DefaultViewElementFactoryNames
{
    internal const string ClassifiedText = "default ClassifiedTextElement to Control";
    internal const string Container = "default ContainerElement to Control";
    internal const string Image = "default ImageElement to Control";
    internal const string Fallback = "default object to Control";
}

[Export(typeof(IViewElementFactory))]
[Name(DefaultViewElementFactoryNames.ClassifiedText)]
[TypeConversion(from: typeof(ClassifiedTextElement), to: typeof(Control))]
[Order(Before = DefaultViewElementFactoryNames.Fallback)]
public sealed class ClassifiedTextElementViewElementFactory : IViewElementFactory
{
    private readonly IClassificationFormatMapService _formatMapService;
    private readonly IClassificationTypeRegistryService _typeRegistry;

    [ImportingConstructor]
    public ClassifiedTextElementViewElementFactory(
        IClassificationFormatMapService formatMapService,
        IClassificationTypeRegistryService typeRegistry)
    {
        _formatMapService = formatMapService;
        _typeRegistry = typeRegistry;
    }

    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        if (model is not ClassifiedTextElement element)
        {
            throw new ArgumentException($"Unsupported conversion from {model?.GetType().FullName}.", nameof(model));
        }

        var formatMap = _formatMapService.GetClassificationFormatMap(textView);
        var block = new TextBlock { TextWrapping = TextWrapping.Wrap };
        foreach (var run in element.Runs)
        {
            var inline = new Run(run.Text);
            var properties = _typeRegistry.GetClassificationType(run.ClassificationTypeName) is { } type
                ? formatMap.GetTextProperties(type)
                : formatMap.DefaultTextProperties;
            if (!properties.ForegroundBrushEmpty)
            {
                inline.Foreground = properties.ForegroundBrush;
            }

            if (run.Style.HasFlag(ClassifiedTextRunStyle.UseClassificationFont))
            {
                if (!properties.TypefaceEmpty)
                {
                    inline.FontFamily = properties.Typeface.FontFamily;
                    inline.FontStyle = properties.Typeface.Style;
                    inline.FontWeight = properties.Typeface.Weight;
                }

                if (!properties.FontRenderingEmSizeEmpty)
                {
                    inline.FontSize = properties.FontRenderingEmSize;
                }
            }

            if (run.Style.HasFlag(ClassifiedTextRunStyle.Bold))
            {
                inline.FontWeight = FontWeight.Bold;
            }

            if (run.Style.HasFlag(ClassifiedTextRunStyle.Italic))
            {
                inline.FontStyle = FontStyle.Italic;
            }

            // Navigation runs render in link style; invoking the action needs run-level
            // hit testing, which Avalonia inlines don't expose — a documented divergence.
            if (run.Style.HasFlag(ClassifiedTextRunStyle.Underline) || run.NavigationAction is not null)
            {
                inline.TextDecorations = TextDecorations.Underline;
            }

            block.Inlines!.Add(inline);
        }

        return block as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }
}

[Export(typeof(IViewElementFactory))]
[Name(DefaultViewElementFactoryNames.Container)]
[TypeConversion(from: typeof(ContainerElement), to: typeof(Control))]
[Order(Before = DefaultViewElementFactoryNames.Fallback)]
public sealed class ContainerElementViewElementFactory : IViewElementFactory
{
    private readonly Lazy<IViewElementFactoryService> _service;

    [ImportingConstructor]
    public ContainerElementViewElementFactory(Lazy<IViewElementFactoryService> service)
    {
        _service = service;
    }

    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        if (model is not ContainerElement container)
        {
            throw new ArgumentException($"Unsupported conversion from {model?.GetType().FullName}.", nameof(model));
        }

        Panel panel = (container.Style & ContainerElementStyle.Stacked) != 0
            ? new StackPanel { Orientation = Orientation.Vertical }
            : new StackPanel { Orientation = Orientation.Horizontal };
        double padding = (container.Style & ContainerElementStyle.VerticalPadding) != 0 ? 3.0 : 0.0;
        foreach (var element in container.Elements)
        {
            if (element is not null && _service.Value.CreateViewElement<Control>(textView, element) is { } child)
            {
                // Only the vertical margin belongs to the container; the factory that made
                // the child owns its horizontal spacing (e.g. an icon's gap from the text).
                child.Margin = new Thickness(child.Margin.Left, padding, child.Margin.Right, padding);
                panel.Children.Add(child);
            }
        }

        return panel as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }
}

[Export(typeof(IViewElementFactory))]
[Name(DefaultViewElementFactoryNames.Image)]
[TypeConversion(from: typeof(ImageElement), to: typeof(Control))]
[Order(Before = DefaultViewElementFactoryNames.Fallback)]
public sealed class ImageElementViewElementFactory : IViewElementFactory
{
    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        if (model is not ImageElement image)
        {
            throw new ArgumentException($"Unsupported conversion from {model?.GetType().FullName}.", nameof(model));
        }

        // ImageId resolution is a host concern (VS's image catalog is proprietary);
        // the default is a fixed-size placeholder that preserves layout, including the
        // gap an inline icon keeps from the text that follows it.
        var placeholder = new Border
        {
            Width = 16.0,
            Height = 16.0,
            Background = Brushes.Transparent,
            Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image.AutomationName is { } name)
        {
            AutomationProperties.SetName(placeholder, name);
        }

        return placeholder as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }
}

[Export(typeof(IViewElementFactory))]
[Name(DefaultViewElementFactoryNames.Fallback)]
[TypeConversion(from: typeof(object), to: typeof(Control))]
[Order]
public sealed class ObjectViewElementFactory : IViewElementFactory
{
    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        var block = new TextBlock { Text = model.ToString(), TextWrapping = TextWrapping.Wrap };
        return block as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }
}
