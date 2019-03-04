using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace RoslynPad.Runtime
{
    internal static class WindowsNativeMethods
    {
        internal static void DisableWer()
        {
            if (Environment.OSVersion.Version >= new Version(6, 1, 0, 0))
            {
                SetErrorMode(GetErrorMode() | ErrorMode.SEM_FAILCRITICALERRORS | ErrorMode.SEM_NOOPENFILEERRORBOX |
                             ErrorMode.SEM_NOGPFAULTERRORBOX);
            }
        }

        [DllImport("kernel32", PreserveSig = true)]
        private static extern ErrorMode SetErrorMode(ErrorMode mode);

        [DllImport("kernel32", PreserveSig = true)]
        private static extern ErrorMode GetErrorMode();

        [Flags]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum ErrorMode
        {
            SEM_FAILCRITICALERRORS = 0x0001,

            SEM_NOGPFAULTERRORBOX = 0x0002,

            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

            SEM_NOOPENFILEERRORBOX = 0x8000,
        }
    }
}
