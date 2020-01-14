using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WakaTime
{
    static class ProcessorArchitectureHelper
    {
        static readonly bool Is64BitProcess = (IntPtr.Size == 8);
        internal static bool Is64BitOperatingSystem = Is64BitProcess || InternalCheckIsWow64();

        public static bool InternalCheckIsWow64()
        {
            // Use fully managed 64-bit process detection on platforms other than Windows
            if (PlatformID.Win32NT != Environment.OSVersion.Platform)
            {
#if NET35
                return (IntPtr.Size == 8);
#else
                return  (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess);
#endif
            }

            if ((Environment.OSVersion.Version.Major != 5 || Environment.OSVersion.Version.Minor < 1) &&
                Environment.OSVersion.Version.Major < 6)
                return false;

            using (var p = Process.GetCurrentProcess())
            {
                bool retVal;
                return NativeMethods.IsWow64Process(p.Handle, out retVal) && retVal;
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);
    }
}

