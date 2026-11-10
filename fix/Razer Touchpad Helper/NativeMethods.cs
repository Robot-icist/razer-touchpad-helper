using System;
using System.Runtime.InteropServices;

namespace RazerTouchpadHelper
{
    internal static class NativeMethods
    {
        // ---- Window Messages ----
        public const uint WM_INPUT = 0x00FF;

        // ---- Raw Input ----
        public const uint RID_INPUT = 0x10000003;
        public const int RIM_TYPEHID = 2;
        public const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWHID hid;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint numDevices, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        // ---- SendInput ----
        public const uint INPUT_MOUSE = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x1000;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        public const ushort VK_SHIFT = 0x10;
        public const ushort VK_CONTROL = 0x11;
        public const ushort VK_MENU = 0x12;
        public const ushort VK_TAB = 0x09;
        public const ushort VK_LEFT = 0x25;
        public const ushort VK_RIGHT = 0x27;
        public const ushort VK_A = 0x41;
        public const ushort VK_D = 0x44;
        public const ushort VK_S = 0x53;
        public const ushort VK_LWIN = 0x5B;

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk, wScan;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT p);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        [DllImport("user32.dll")]
        public static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ClipCursor(IntPtr lpRect);

        // ---- Pointer device enumeration ----
        public const int POINTER_DEVICE_PRODUCT_STRING_MAX = 520;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct POINTER_DEVICE_INFO
        {
            public uint displayOrientation;
            public IntPtr device;
            public int pointerDeviceType;
            public IntPtr monitor;
            public uint startingCursorId;
            public ushort maxActiveContacts;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = POINTER_DEVICE_PRODUCT_STRING_MAX)]
            public string productString;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerDevices(ref uint deviceCount, [Out] POINTER_DEVICE_INFO[] pointerDevices);

        // ---- SystemInformation helpers ----
        public const int SPI_GETMOUSESPEED = 0x0070;
        public const int SPI_SETMOUSESPEED = 0x0071;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);
    }
}