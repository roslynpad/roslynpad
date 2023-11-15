using Avalonia;

namespace RoslynPadReplSample;

internal static class Program
{
    // This method is needed for IDE previewer infrastructure
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect();

    // The entry point. Things aren't ready yet
    public static int Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}
