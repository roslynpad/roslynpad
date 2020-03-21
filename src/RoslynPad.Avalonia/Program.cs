using Avalonia;
using System;

namespace RoslynPad
{
    class Program
    {
        public static AppBuilder BuildAvaloniaApp()
          => AppBuilder.Configure<App>().UsePlatformDetect();

        [STAThread]
        public static int Main(string[] args)
          => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
}