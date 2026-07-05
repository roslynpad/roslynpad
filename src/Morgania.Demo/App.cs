namespace Microsoft.VisualStudio.Demo;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Themes.Fluent;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateMainWindow()
    {
        var container = EditorHost.Create();
        var bufferFactory = container.GetExport<ITextBufferFactoryService>();
        var contentTypes = container.GetExport<IContentTypeRegistryService>();
        var editorFactory = container.GetExport<ITextEditorFactoryService>();
        var formatMaps = container.GetExport<IClassificationFormatMapService>();

        var buffer = bufferFactory.CreateTextBuffer(SampleDocument.Text, contentTypes.GetContentType("code"));
        var view = editorFactory.CreateTextView(buffer);
        view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);

        var formatMap = formatMaps.GetClassificationFormatMap(view);
        formatMap.DefaultTextProperties = formatMap.DefaultTextProperties
            .SetForeground(Color.FromRgb(0xD4, 0xD4, 0xD4))
            .SetFontRenderingEmSize(14.0);
        view.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

        var host = editorFactory.CreateTextViewHost(view, setFocus: true);
        var window = new Window
        {
            Width = 1100,
            Height = 750,
            Content = host.HostControl,
        };

        // Live zoom readout; the full gesture cheat sheet lives in the status margin.
        void UpdateTitle() => window.Title =
            $"Morgania — VS editor core on Avalonia — zoom {view.ZoomLevel:0}%";
        UpdateTitle();
        view.ZoomLevelChanged += (_, _) => UpdateTitle();

        // Word wrap toggle (Alt+Z): wrapped lines re-anchor on relayout.
        view.VisualElement.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Alt) && !view.IsClosed)
            {
                var current = view.Options.GetOptionValue(DefaultTextViewOptions.WordWrapStyleId);
                bool wrapped = (current & WordWrapStyles.WordWrap) != 0;
                view.Options.SetOptionValue(
                    DefaultTextViewOptions.WordWrapStyleId,
                    wrapped ? WordWrapStyles.None : WordWrapStyles.WordWrap | WordWrapStyles.VisibleGlyphs);
                e.Handled = true;
            }
        };

        // Find/replace (Ctrl+F, Ctrl+H, F3/Shift+F3): the panel ships with the editor.
        var findReplace = FindReplacePanel.Get(view);
        window.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (findReplace is null || view.IsClosed)
            {
                return;
            }

            switch (e.Key, e.KeyModifiers)
            {
                case (Key.F, KeyModifiers.Control):
                    findReplace.Show(showReplace: false);
                    e.Handled = true;
                    break;
                case (Key.H, KeyModifiers.Control):
                    findReplace.Show(showReplace: true);
                    e.Handled = true;
                    break;
                case (Key.F3, KeyModifiers.None):
                    findReplace.FindNext();
                    e.Handled = true;
                    break;
                case (Key.F3, KeyModifiers.Shift):
                    findReplace.FindPrevious();
                    e.Handled = true;
                    break;
            }
        });

        return window;
    }
}
