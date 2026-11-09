using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RblxExecutorUI
{
    public static class RblxCore
    {
        private const string DllName = "Syntax.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Initialize();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint FindRobloxProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Connect(uint pid);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void Disconnect();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetRobloxPid();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern void RedirConsole();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern UIntPtr GetDataModel();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetJobCount();

        [DllImport(DllName, EntryPoint = "ExecuteScript", CallingConvention = CallingConvention.StdCall)]
        private static extern int ExecuteScriptUtf8(byte[] source, int sourceLen);

        public static int ExecuteScript(string source)
        {
            var bytes = ExecutionPayload.Encode(source);
            return ExecuteScriptUtf8(bytes, bytes.Length);
        }

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetCoreOffsetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsExecutionReady();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetCoreExeSha256();
        public static string GetExpectedExeSha256() => Marshal.PtrToStringUTF8(GetCoreExeSha256()) ?? "";

        public static string GetOffsetVersion() => Marshal.PtrToStringUTF8(GetCoreOffsetVersion()) ?? "";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        private static extern int GetLastExecError(StringBuilder buffer, int bufLen);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool ReadMemory(UIntPtr address, IntPtr buffer, UIntPtr size);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool WriteMemory(UIntPtr address, IntPtr buffer, UIntPtr size);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool GetClientInfo(StringBuilder buffer, int maxSize);

        // Helper: get last error message
        public static string GetLastError()
        {
            var sb = new StringBuilder(1024);
            GetLastExecError(sb, sb.Capacity);
            return sb.ToString();
        }

        // Helper for easy memory reading in C#
        public static T Read<T>(UIntPtr address) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                if (ReadMemory(address, ptr, (UIntPtr)size))
                {
                    return Marshal.PtrToStructure<T>(ptr);
                }
                return default;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
