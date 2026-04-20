using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace RoslynPad;

public static partial class WindowExtensions
{
    public static void UseImmersiveDarkMode(this Window window, bool value)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        DwmSetWindowAttribute(
            hwnd,
            DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
            value,
            Marshal.SizeOf<bool>());
        // Ignore errors - DWMWA_USE_IMMERSIVE_DARK_MODE is not supported on all Windows versions
        // (e.g. Windows Server 2016/2019), so a failure here is expected and should not crash the app.
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
