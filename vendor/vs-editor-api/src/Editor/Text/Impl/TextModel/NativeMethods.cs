using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Text.Implementation
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetFileInformationByHandle(
            Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation
        );

        [DllImport("libc", EntryPoint = "fstat")]
        internal static extern int DarwinStat(int fd, out darwin_stat_t buf);

        [StructLayout(LayoutKind.Explicit, Size = 144)]
        internal struct darwin_stat_t
        {
            [FieldOffset(10)] public ushort st_nlink;
        }
    }
}