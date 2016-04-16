using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RoslynPad.Host
{
    internal sealed class ChildProcessManager : IDisposable
    {
        private SafeJobHandle _handle;
        private bool _disposed;

        public ChildProcessManager()
        {
            _handle = new SafeJobHandle(CreateJobObject(IntPtr.Zero, null));

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = 0x2000
            };

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = info
            };

            var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            var extendedInfoPtr = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!SetInformationJobObject(_handle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr,
                    (uint)length))
            {
                throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _handle.Dispose();
            _handle = null;
            _disposed = true;
        }

        private void ValidateDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ChildProcessManager));
        }

        public void AddProcess(SafeProcessHandle processHandle)
        {
            ValidateDisposed();
            if (!AssignProcessToJobObject(_handle, processHandle))
            {
                throw new InvalidOperationException("Unable to add the process");
            }
        }

        public void AddProcess(Process process)
        {
            AddProcess(process.SafeHandle);
        }

        public void AddProcess(int processId)
        {
            using (var process = Process.GetProcessById(processId))
            {
                AddProcess(process);
            }
        }

        #region Safe Handle

        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeJobHandle(IntPtr handle) : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }

            [DllImport("kernel32", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            private static extern bool CloseHandle(IntPtr hObject);
        }

        #endregion

        #region Win32
        // ReSharper disable InconsistentNaming

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr a, string lpName);

        [DllImport("kernel32")]
        private static extern bool SetInformationJobObject(SafeJobHandle hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(SafeJobHandle job, SafeProcessHandle process);

        [StructLayout(LayoutKind.Sequential)]
        internal struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        public enum JobObjectInfoType
        {
            AssociateCompletionPortInformation = 7,
            BasicLimitInformation = 2,
            BasicUIRestrictions = 4,
            EndOfJobTimeInformation = 6,
            ExtendedLimitInformation = 9,
            SecurityLimitInformation = 5,
            GroupInformation = 11
        }

        // ReSharper restore InconsistentNaming
        #endregion
    }
}
