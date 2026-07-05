//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        public const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        /// <summary>
        /// Win32 WINDOWPOS struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        /// <summary>
        /// A point structure to match the Win32 POINT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        };

        /// <summary>
        /// A rect structure to match the Win32 RECT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        };

        /// <summary>
        /// Win32 MONITORINFO Struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        };

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromPoint(POINT pt, int flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO monitorInfo);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        internal static extern IntPtr SetFocus(IntPtr hwnd);
    }
}
