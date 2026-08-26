using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia;
using Avalonia.Platform;

namespace RoslynPad;

internal class Program
{
    public static AppBuilder BuildAvaloniaApp()
      => AppBuilder.Configure<App>().UsePlatformDetect();

    [STAThread]
    public static int Main(string[] args)
    {
        var exitCode = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        if (OperatingSystem.IsMacOS() && Environment.Version.Major >= 11)
        {
            ShutdownAvaloniaNative();
        }

        return exitCode;
    }

    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods,
        typeof(AvaloniaLocator))]
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.NonPublicMethods,
        "Avalonia.Native.AvaloniaNativePlatform",
        "Avalonia.Native")]
    private static void ShutdownAvaloniaNative()
    {
        // Avalonia normally invokes this from ProcessExit, after .NET 11 has already torn down the
        // virtual-dispatch state needed to release the native library's managed callbacks.
        var locatorType = typeof(AvaloniaLocator);
        var locator = locatorType.GetProperty("Current", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
            ?? throw new MissingMemberException(locatorType.FullName, "Current");
        var getServiceMethod = locatorType.GetMethod(
            "GetService",
            BindingFlags.Instance | BindingFlags.Public,
            [typeof(Type)])
            ?? throw new MissingMethodException(locatorType.FullName, "GetService");
        var platform = getServiceMethod.Invoke(locator, [typeof(IWindowingPlatform)])
            ?? throw new InvalidOperationException("The Avalonia windowing platform is not registered.");
        var nativePlatformType = Type.GetType(
            "Avalonia.Native.AvaloniaNativePlatform, Avalonia.Native",
            throwOnError: true)!;
        var processExitMethod = nativePlatformType.GetMethod(
            "OnProcessExit",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nativePlatformType.FullName, "OnProcessExit");

        processExitMethod.Invoke(platform, [null, EventArgs.Empty]);
    }
}