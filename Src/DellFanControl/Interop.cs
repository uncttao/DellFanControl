using System;
using System.Runtime.InteropServices;

namespace DellFanControl.DellFanControl
{
    public class Interop
    {

        public const uint INVALID_HANDLE_VALUE = 0;

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        public static extern bool PostMessage(
            IntPtr hWnd,
            uint msg,
            int wParam,
            int lParam
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        [
            return: MarshalAs(UnmanagedType.Bool)
        ]
        public static extern bool DeleteService(IntPtr hService);

        [StructLayout(LayoutKind.Sequential)]
        public class QUERY_SERVICE_CONFIG
        {
            UInt32 dwBytesNeeded;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwServiceType;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwStartType;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwErrorControl;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpBinaryPathName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpLoadOrderGroup;
            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTagID;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpDependencies;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpServiceStartName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public String lpDisplayName;
        };

        [Flags]
        public enum SERVICE_CONTROL : uint
        {
            STOP = 0x00000001,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [
            return: MarshalAs(UnmanagedType.Bool)
        ]
        public static extern bool ControlService(
            IntPtr hService,
            SERVICE_CONTROL dwControl,
            ref SERVICE_STATUS lpServiceStatus
        );

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
            String lpFileName,
            uint dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [Flags]
        public enum SCM_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SC_MANAGER_CONNECT = 0x00001,
            SC_MANAGER_CREATE_SERVICE = 0x00002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
            SC_MANAGER_LOCK = 0x00008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
            SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
            SC_MANAGER_CONNECT |
            SC_MANAGER_CREATE_SERVICE |
            SC_MANAGER_ENUMERATE_SERVICE |
            SC_MANAGER_LOCK |
            SC_MANAGER_QUERY_LOCK_STATUS |
            SC_MANAGER_MODIFY_BOOT_CONFIG
        }

        private const int SERVICE_QUERY_CONFIG = 0x0001;
        private const int SERVICE_CHANGE_CONFIG = 0x0002;
        private const int SERVICE_QUERY_STATUS = 0x0004;
        private const int SERVICE_ENUMERATE_DEPENDENTS = 0x0008;
        private const int SERVICE_START = 0x0010;
        private const int SERVICE_STOP = 0x0020;
        private const int SERVICE_PAUSE_CONTINUE = 0x0040;
        private const int SERVICE_INTERROGATE = 0x0080;
        private const int SERVICE_USER_DEFINED_CONTROL = 0x0100;

        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        public const uint SERVICE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
            SERVICE_QUERY_CONFIG |
            SERVICE_CHANGE_CONFIG |
            SERVICE_QUERY_STATUS |
            SERVICE_ENUMERATE_DEPENDENTS |
            SERVICE_START |
            SERVICE_STOP |
            SERVICE_PAUSE_CONTINUE |
            SERVICE_INTERROGATE |
            SERVICE_USER_DEFINED_CONTROL;

        public const int SERVICE_DEMAND_START = 0x00000003;
        public const int SERVICE_KERNEL_DRIVER = 0x00000001;
        public const int SERVICE_ERROR_NORMAL = 0x00000001;

        public const int FILE_SHARE_READ = 0x00000001;
        public const int FILE_SHARE_WRITE = 0x00000002;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const int FILE_ATTRIBUTE_NORMAL = 0x80;
        public const int OPEN_EXISTING = 3;
        public const uint BZH_DELL_SMM_IOCTL_KEY = 0xB42;
        public const uint FILE_DEVICE_BZH_DELL_SMM = 0x0000B424;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(
            string machineName,
            string databaseName,
            uint dwAccess
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        [
            return: MarshalAs(UnmanagedType.Bool)
        ]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenService(
            IntPtr hSCManager,
            string lpServiceName,
            uint dwDesiredAccess
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean QueryServiceConfig(IntPtr hService, IntPtr intPtrQueryConfig, UInt32 cbBufSize, out UInt32 pcbBytesNeeded);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean QueryServiceConfig(IntPtr hService, QUERY_SERVICE_CONFIG lpServiceConfig, UInt32 cbBufSize, out UInt32 pcbBytesNeeded);

        public const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
        );

        public static uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method));
        }

        public struct SMBIOS_PKG
        {
            public uint cmd;
            public uint data;
            public uint stat1;
            public uint stat2;
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            ref SMBIOS_PKG lpInBuffer,
            uint nInBufferSize,
            ref SMBIOS_PKG lpOutBuffer,
            uint nOutBufferSize,
            ref uint lpBytesReturned,
            IntPtr lpOverlapped
        );

    }
}