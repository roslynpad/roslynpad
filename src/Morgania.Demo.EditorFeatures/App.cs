using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Morgania.CodeAnalysis.Editor;

namespace Morgania.Demo.EditorFeatures;

public sealed class App : Application
{
    public static bool SmokeMode { get; set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Bind the JoinableTaskContext to the UI thread before the graph composes.
        HostServiceExports.InitializeMainThread();

        // The package's EditorComposition builds the complete editor graph: the Morgania
        // editor, the recompiled Roslyn EditorFeatures, Roslyn Workspaces/Features, and the
        // editor-host services (classification formats, squiggles, light bulb, key bridge, …).
        var exportProvider = EditorComposition.CreateExportProvider();

        var contentTypes = exportProvider.GetExportedValue<IContentTypeRegistryService>();
        var bufferFactory = exportProvider.GetExportedValue<ITextBufferFactoryService>();
        var editorFactory = exportProvider.GetExportedValue<ITextEditorFactoryService>();
        var formatMaps = exportProvider.GetExportedValue<IClassificationFormatMapService>();

        // "CSharp" is registered by the recompiled Roslyn EditorFeatures assemblies.
        var contentType = contentTypes.GetContentType("CSharp");
        if (contentType is null)
        {
            Console.Error.WriteLine("known content types: " +
                string.Join(", ", contentTypes.ContentTypes.Select(c => c.TypeName).Order()));
            var definitions = exportProvider.GetExports<Microsoft.VisualStudio.Utilities.ContentTypeDefinition, IDictionary<string, object>>();
            Console.Error.WriteLine("content type definition exports: " +
                string.Join(", ", definitions.Select(d => d.Metadata.TryGetValue("Name", out var n) ? n?.ToString() : "?").Order()));
            throw new InvalidOperationException("The CSharp content type is not registered");
        }
        var buffer = bufferFactory.CreateTextBuffer(SampleCode.Text, contentType);

        if (SmokeMode)
        {
            // Roslyn resolves every ClassificationTypeNames constant through the semantic layer
            // at startup; a miss crashes the taggers, so fail fast with the actual names.
            var registry = exportProvider.GetExportedValue<IClassificationTypeRegistryService>();
            var missing = typeof(Microsoft.CodeAnalysis.Classification.ClassificationTypeNames).GetFields()
                .Select(f => (string)f.GetValue(null)!)
                .Where(name => registry.GetClassificationType(ClassificationLayer.Semantic, name) is null)
                .Order()
                .ToList();
            if (missing.Count > 0)
            {
                throw new InvalidOperationException($"unresolved classification types: {string.Join("; ", missing)}");
            }
        }

        var workspace = RoslynDemoHost.OpenDocument(exportProvider, buffer, "DemoDocument.cs");

        var view = editorFactory.CreateTextView(buffer);
        view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
        view.Options.SetOptionValue(DefaultTextViewOptions.BraceCompletionEnabledOptionId, true);

        var formatMap = formatMaps.GetClassificationFormatMap(view);
        formatMap.DefaultTextProperties = formatMap.DefaultTextProperties
            .SetForeground(Color.FromRgb(0xD4, 0xD4, 0xD4))
            .SetFontRenderingEmSize(14.0);
        view.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

        var viewHost = editorFactory.CreateTextViewHost(view, setFocus: true);
        var window = new Window
        {
            Width = 1100,
            Height = 750,
            Title = "RoslynPad — Roslyn EditorFeatures on Morgania",
            Content = viewHost.HostControl,
        };

        window.Closed += (_, _) => workspace.Dispose();

        if (SmokeMode)
        {
            _ = SmokeTest.RunAsync(desktop, exportProvider, view, buffer);
        }

        return window;
    }
}
