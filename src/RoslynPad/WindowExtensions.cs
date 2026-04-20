using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace RoslynPad;

public static partial class WindowExtensions
{
    // DWMWA_USE_IMMERSIVE_DARK_MODE requires Windows 10 version 2004 (build 19041) or later
    private static readonly bool s_immersiveDarkModeSupported = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);

    public static void UseImmersiveDarkMode(this Window window, bool value)
    {
        if (!s_immersiveDarkModeSupported)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        DwmSetWindowAttribute(
            hwnd,
            DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
            value,
            Marshal.SizeOf<bool>());
    }

    [LibraryImport("dwmapi")]
    private static partial int DwmSetWindowAttribute(
        IntPtr hwnd,
        DwmWindowAttribute attribute,
        [MarshalAs(UnmanagedType.Bool)] in bool pvAttribute,
        int cbAttribute);

    private enum DwmWindowAttribute
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
    }
}
