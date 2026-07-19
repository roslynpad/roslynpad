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
using Microsoft.VisualStudio.Text.Formatting;
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
            var properties = _typeRegistry.GetClassificationType(run.ClassificationTypeName) is { } type
                ? formatMap.GetTextProperties(type)
                : formatMap.DefaultTextProperties;

            // Avalonia inlines are not input elements, so navigation runs are embedded as
            // clickable controls; plain runs stay real inlines so text wraps inside them.
            if (run.NavigationAction is { } action)
            {
                var link = new NavigationTextBlock
                {
                    Text = run.Text,
                    NavigationAction = action,
                    TextDecorations = TextDecorations.Underline,
                };
                ApplyRunFormat(link, run, properties);
                if (run.Tooltip is { } tooltip)
                {
                    ToolTip.SetTip(link, tooltip);
                }

                block.Inlines!.Add(new InlineUIContainer(link));
                continue;
            }

            var inline = new Run(run.Text);
            ApplyRunFormat(inline, run, properties);
            if (run.Style.HasFlag(ClassifiedTextRunStyle.Underline))
            {
                inline.TextDecorations = TextDecorations.Underline;
            }

            block.Inlines!.Add(inline);
        }

        return block as TView
            ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
    }

    // TextBlock's font/foreground properties are AddOwner'd from TextElement, so one
    // helper formats both Run inlines and embedded link controls.
    private static void ApplyRunFormat(AvaloniaObject target, ClassifiedTextRun run, TextFormattingRunProperties properties)
    {
        if (!properties.ForegroundBrushEmpty)
        {
            target.SetValue(TextElement.ForegroundProperty, properties.ForegroundBrush);
        }

        if (run.Style.HasFlag(ClassifiedTextRunStyle.UseClassificationFont))
        {
            if (!properties.TypefaceEmpty)
            {
                target.SetValue(TextElement.FontFamilyProperty, properties.Typeface.FontFamily);
                target.SetValue(TextElement.FontStyleProperty, properties.Typeface.Style);
                target.SetValue(TextElement.FontWeightProperty, properties.Typeface.Weight);
            }

            if (!properties.FontRenderingEmSizeEmpty)
            {
                target.SetValue(TextElement.FontSizeProperty, properties.FontRenderingEmSize);
            }
        }

        if (run.Style.HasFlag(ClassifiedTextRunStyle.Bold))
        {
            target.SetValue(TextElement.FontWeightProperty, FontWeight.Bold);
        }

        if (run.Style.HasFlag(ClassifiedTextRunStyle.Italic))
        {
            target.SetValue(TextElement.FontStyleProperty, FontStyle.Italic);
        }
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

        double padding = (container.Style & ContainerElementStyle.VerticalPadding) != 0 ? 3.0 : 0.0;
        if ((container.Style & ContainerElementStyle.Stacked) == 0)
        {
            // Wrapped is inline flow, not a horizontal panel: a panel can never break
            // inside a child, so long classified text would run past the popup edge
            // instead of wrapping mid-text beside the icon.
            var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0.0, padding) };
            foreach (var element in container.Elements)
            {
                if (element is null || _service.Value.CreateViewElement<Control>(textView, element) is not { } child)
                {
                    continue;
                }

                if (child is TextBlock { Inlines.Count: > 0 } block)
                {
                    var runs = block.Inlines!.ToArray();
                    block.Inlines!.Clear();
                    text.Inlines!.AddRange(runs);
                }
                else if (child is TextBlock { Text.Length: > 0 } plain)
                {
                    text.Inlines!.Add(new Run(plain.Text));
                }
                else
                {
                    text.Inlines!.Add(new InlineUIContainer(child) { BaselineAlignment = BaselineAlignment.Center });
                }
            }

            return text as TView
                ?? throw new ArgumentException($"Unsupported conversion to {typeof(TView).FullName}.", nameof(model));
        }

        var panel = new StackPanel { Orientation = Orientation.Vertical };
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
