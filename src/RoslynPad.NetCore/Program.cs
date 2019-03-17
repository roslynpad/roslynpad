using Avalonia;
using System;

namespace RoslynPad
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .Start<MainWindow>();
        }
    }
}