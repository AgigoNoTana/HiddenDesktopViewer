using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;

namespace HiddenDesktopViewer
{

    class WindowStation
    {
        public delegate bool EnumWindowStationsDelegate(string windowsStation, IntPtr lParam);
        public delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindowStations(
            EnumWindowStationsDelegate lpEnumFunc,
            IntPtr lParam
        );

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr OpenWindowStation(string name, bool fInherit, uint needAccess);

        private delegate bool EnumDesktopsDelegate(string desktop, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumDesktops(IntPtr hwinsta, EnumDesktopsDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool CloseWindowStation(
            IntPtr winStation
        );


        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenDesktop(
            string DesktopName,
            uint Flags,
            bool Inherit,
            uint Access
        );

        [DllImport("user32.dll")]
        public static extern bool CloseDesktop(
            IntPtr hDesktop
        );

        [DllImport("user32.dll")]
        public static extern bool EnumDesktopWindows(
            IntPtr hDesktop,
            EnumDesktopWindowsDelegate EnumFunc,
            IntPtr lParam
        );

        [DllImport("user32", SetLastError = true)]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpWindowText,
            int nMaxCount
        );

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(
            IntPtr hwnd
        );

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(
            IntPtr hWnd,
            out IntPtr ProcessId
        );
        [DllImport("kernel32.dll", SetLastError = true)]
        private extern static bool Beep(uint dwFreq, uint dwDuration);

        [DllImport("user32.dll")]
        public static extern bool SwitchDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessWindowStation(IntPtr hWinSta);
        public static List<string> desktopsList = new List<string>();

        static bool DispDeskFunc(string DesktopName, IntPtr lParam)
        {
            desktopsList.Add(DesktopName);
            return true;
        }

        private static StringBuilder returnName = new StringBuilder();
        public static List<string> EnumerateDesktops(string winStationName)
        {
            IntPtr hWinSta = OpenWindowStation(winStationName, true, 0x37);
            bool bEnum = EnumDesktops(hWinSta, DispDeskFunc, IntPtr.Zero);
            var ret_List = new List<string>(desktopsList);

            desktopsList = new List<string>();
            return ret_List;
        }
    }
}


