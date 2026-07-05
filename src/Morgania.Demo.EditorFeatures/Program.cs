using Avalonia;
using Avalonia.Headless;

namespace Morgania.Demo.EditorFeatures;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--smoke"))
        {
            // Headless smoke run: boots the full pipeline (MEF graph, Roslyn workspace, editor
            // view) without a display and exercises classification and completion.
            App.SmokeMode = true;
            AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
                .StartWithClassicDesktopLifetime(args);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect();
}
