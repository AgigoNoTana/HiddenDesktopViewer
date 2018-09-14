using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Management;
using System.Runtime.CompilerServices;

namespace HiddenDesktopViewer
{
    public partial class Form1 : Form
    {
        public class ListViewItemComparer : IComparer
        {
            private int _column;

            public ListViewItemComparer(int col)
            {
                _column = col;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemx = (ListViewItem)x;
                ListViewItem itemy = (ListViewItem)y;

                return string.Compare(itemx.SubItems[_column].Text, itemy.SubItems[_column].Text);
            }
        }

        [SuppressUnmanagedCodeSecurityAttribute]
        internal static class SafeNativeMethods
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeWindowStationHandle GetProcessWindowStation();

            [return: MarshalAs(UnmanagedType.Bool)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool CloseWindowStation(IntPtr hWinsta);
        }


        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetThreadDesktop(uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetUserObjectInformation(SafeWindowStationHandle hObj, int nIndex,
           [Out] string pvInfo, uint nLength, out uint lpnLengthNeeded);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "GetUserObjectInformationW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetUserObjectInformation(IntPtr hObj, UOI nIndex, StringBuilder pvInfo, int nLength, ref int lpnLengthNeeded);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
           [Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll")]
        private static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll", SetLastError = true)]

        static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("kernel32.dll")]
        private extern static int TerminateProcess(IntPtr hProcess, UInt32 uExitCode);

        enum UOI : uint
        {
            Flags = 1,
            Name = 2,
            Type = 3,
            UserSID = 4,
            HeapSize = 5,
            IO =  6
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        private const int UOI_FLAGS = 1;
        private const int UOI_NAME = 2;
        private const int UOI_TYPE = 3;
        private const int UOI_USER_SID = 4;
        private const int UOI_HEAPSIZE = 5;
        private const int UOI_IO = 6;

        public string OwnPath = "";
        public string OwnProcessName = "";

        public Form1()
        {
            InitializeComponent();

            OwnPath = Application.ExecutablePath;

            System.Diagnostics.Process pOwn = System.Diagnostics.Process.GetCurrentProcess();

            OwnProcessName = pOwn.ProcessName;

            var timer = new Timer();
            timer.Tick += new EventHandler(this.CheckDesktopTimer);
            timer.Interval = 3000;

            timer.Start();
        }

        public void CheckDesktopTimer(object sender, EventArgs e)
        {
            try
            {
                if (checkBox1.Checked == true)
                {

                    List<string> POwnPIDList = new List<string>();

                    POwnPIDList = Get_OwnProcessesIdList();

                    IntPtr NowDsk = IntPtr.Zero;
                    Get_CurrentInputDesktop(out NowDsk);

                    StringBuilder DeskName = new StringBuilder();
                    int lpnLength = 0;

                    GetUserObjectInformation(NowDsk, UOI.Name, DeskName, 100, ref lpnLength);


                    IntPtr winStaHandle = GetProcessWindowStation();

                    StringBuilder stationName = new StringBuilder();
                    int lpnLength2 = 0;

                    GetUserObjectInformation(winStaHandle, UOI.Name, stationName, 100, ref lpnLength2);

                    string WinStaDeskName = stationName + "\\" + DeskName.ToString();

                    bool ExistOwnProcess = false;

                    if (POwnPIDList != null)
                    {
                        foreach (string tpid in POwnPIDList)
                        {
                            Process hProcess = Process.GetProcessById(int.Parse(tpid));

                            ExistOwnProcess = Check_AlreadyExistOnTheDesktop(hProcess, WinStaDeskName);

                            if (ExistOwnProcess == true)
                            {
                                break;
                            }
                        }
                    }

                    if (ExistOwnProcess == false)
                    {
                        CreateProcessWithDesktop(OwnPath, WinStaDeskName);
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message.ToString());
            }
        }

        public List<string> GetProcessListByNameWithWmic(string inputPname)
        {
            try
            {
                List<string> ret = new List<string>();
                
                ManagementClass processClass = new ManagementClass("Win32_Process");
                ManagementObjectCollection classObjects = processClass.GetInstances();

                inputPname = inputPname + ".exe";

                foreach (ManagementObject classObject in classObjects)
                {
                    string nowPname = classObject.GetPropertyValue("Name").ToString();

                    if (inputPname.ToLower() == nowPname.ToLower())
                    {
                        string PID = classObject.GetPropertyValue("ProcessId").ToString();
                        ret.Add(PID);
                    }
                }
                return ret;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message.ToString());
                return null;
            }
        }

        public List<string> Get_OwnProcessesIdList()
        {
            try
            {
                List<string> PIDList = new List<string>();

                PIDList = null;

                PIDList = GetProcessListByNameWithWmic(OwnProcessName);

                return PIDList;

            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message.ToString());
                return null;
            }
        }

        public bool Check_AlreadyExistOnTheDesktop(Process p, string DesktopName)
        {
            string WSstr = "";
            string DTstr = "";

            Get_WS_and_DT_FromPEB(p, out WSstr, out DTstr);

            string WSstrAndDTstr = WSstr + "\\" + DTstr;

            if (WSstrAndDTstr.ToLower() == DesktopName.ToLower())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool save_tomost = false;

        private async void button1_Click(object sender, EventArgs e)
        {
            label3.Text = "Now processing. Please wait a little longer . . . ";

            if (checkBox2.Checked == true)
            {
                save_tomost = true;
            }
            else
            {
                save_tomost = false;
            }

            checkBox2.Checked = false;

            Application.UseWaitCursor = true;

            await Task.Delay(1000);

            listView1.Items.Clear();

            GetProcessList();

            label3.Text = "Completed.";

            Application.UseWaitCursor = false;

            if (save_tomost == true)
            {
                checkBox2.Checked = true;
            }
            else
            {
                checkBox2.Checked = false;
            }
        }

        public void Get_WS_and_DT_FromPEB(Process p, out string WSstr, out string DTstr)
        {
            try
            {
                WSstr = "-";
                DTstr = "-";

                string Str_DesktopInfo;

                Str_DesktopInfo = ProcessClass.ProcessUtilities.GetDesktopInfo(p.Id);

                if (Str_DesktopInfo == null)
                {
                    WSstr = "- (return null)";
                    DTstr = "- (return null)";
                }
                else if (Str_DesktopInfo.Contains(@"\") == true)
                {
                    int SepL = Str_DesktopInfo.IndexOf(@"\");

                    string WinStaName_str = Str_DesktopInfo.Substring(0, SepL);
                    string Desktop_name_str = Str_DesktopInfo.Substring(SepL + 1);

                    if (Desktop_name_str == null)
                    {
                        Desktop_name_str = "- (return null)";
                    }

                    WSstr = WinStaName_str;
                    DTstr = Desktop_name_str;
                }
                else
                {
                    WSstr = "-";
                    DTstr = "-";
                }
            }
            catch (Exception ex)
            {
                WSstr = "- (error:" + ex.Message.ToString() + ")";
                DTstr = "- (error:" + ex.Message.ToString() + ")";
            }
        }

        public string Get_WinTitle_From_WinHandle(IntPtr WinHnd)
        {
            try
            {
                int textLen = GetWindowTextLength(WinHnd);

                if (0 < textLen)
                {
                    StringBuilder tsb = new StringBuilder(textLen + 1);
                    GetWindowText(WinHnd, tsb, tsb.Capacity);

                    return tsb.ToString();
                }
                else
                {
                    return " - (titile length is 0)";
                }
            }
            catch (Exception err)
            {
                return " - (" + err.Message.ToString() + ")";
            }
        }

        public void GetProcessList()
        {
            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcesses();
            int count = 0;

            listView1.BeginUpdate();

            foreach (System.Diagnostics.Process p in ps)
            {
                try
                {
                    count = count + 1;

                    decimal persent_int = (decimal)count / (decimal)ps.Count();
                    string persent_str = ((int)(persent_int * 100)).ToString();

                    label3.Text = "Now processing. Please wait a little longer . . . " + persent_str + "%" + " ( " + count + "/" + ps.Count().ToString() + " Process )";

                    Application.DoEvents();

                    string WSstr = "-";
                    string DTstr = "-";
                    string SettionID = p.SessionId.ToString();

                    Get_WS_and_DT_FromPEB(p, out WSstr, out DTstr);

                    IEnumerable<string> DeskHandleList = HandleClass.EnumDesktopHandlesOpened(p.Id);

                    foreach (string deskhandle in DeskHandleList)
                    {
                        foreach (ProcessThread thread in p.Threads)
                        {
                            try
                            {
                                string DesktopNameFromTID = "-";

                                Get_WindowsInfoFromTID((uint)thread.Id, out DesktopNameFromTID);

                                IntPtr[] windows = GetWindowHandlesForThread(thread.Id);

                                if (windows != null && windows.Length > 0)
                                {
                                    foreach (IntPtr hWnd in windows)
                                    {
                                        try
                                        {
                                            string wintitle = Get_WinTitle_From_WinHandle(hWnd);
                                            AddToList(p.Id.ToString(), p.ProcessName.ToString(), thread.Id.ToString(), wintitle, SettionID, WSstr, DesktopNameFromTID, DTstr, deskhandle);
                                        }
                                        catch (Exception)
                                        {
                                            AddToList(p.Id.ToString(), p.ProcessName.ToString(), thread.Id.ToString(), "- (cannot get window title)", SettionID, WSstr, DesktopNameFromTID, DTstr, deskhandle);
                                        }
                                    }
                                }
                                else if (windows == null)
                                {
                                    AddToList(p.Id.ToString(), p.ProcessName.ToString(), thread.Id.ToString(), "- (no window handles)", SettionID, WSstr, DesktopNameFromTID, DTstr, deskhandle);

                                }
                                else
                                {
                                    AddToList(p.Id.ToString(), p.ProcessName.ToString(), thread.Id.ToString(), "- (no window title)", SettionID, WSstr, DesktopNameFromTID, DTstr, deskhandle);
                                }

                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            listView1.EndUpdate();
        }

        private IntPtr[] GetWindowHandlesForThread(int threadHandle)
        {
            _results.Clear();
            EnumWindows(WindowEnum, threadHandle);
            return _results.ToArray();
        }

        private delegate int EnumWindowsProc(IntPtr hwnd, int lParam);

        [DllImport("user32.Dll")]
        private static extern int EnumWindows(EnumWindowsProc x, int y);
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private List<IntPtr> _results = new List<IntPtr>();

        private int WindowEnum(IntPtr hWnd, int lParam)
        {
            int processID = 0;
            int threadID = GetWindowThreadProcessId(hWnd, out processID);
            if (threadID == lParam) _results.Add(hWnd);
            return 1;
        }

        public void AddToList(string str1, string str2, string str3, string str4, string str5, string str6, string str7, string str8, string str9)
        {
            try
            {
                string[] item1 = { str1, str2, str3, str4, str5, str6, str7, str8, str9 };

                listView1.Items.Add(new ListViewItem(item1));

                if (str9.ToLower() != @"\default" && str9.ToLower() != @"\disconnect" && str9.ToLower() != @"\winlogon")
                {
                    int count = listView1.Items.Count - 1;
                    listView1.Items[count].BackColor = Color.Pink;
                }

                if ((str4.Length > 1 && !str4.Contains("- (")) && (OwnDesktopName.ToLower() != str8.ToLower()) && ("-" != str8) && !str8.Contains("- ("))
                {
                    int count = listView1.Items.Count - 1;
                    listView1.Items[count].BackColor = Color.Orange;
                }

                if (str5 == "0")
                {
                    int count = listView1.Items.Count - 1;
                    listView1.Items[count].BackColor = Color.Gainsboro;
                }

                if (str8.ToLower() != "default" & (!str8.Contains("-") && (!str8.Contains("error"))))
                {
                    int count = listView1.Items.Count - 1;
                    listView1.Items[count].BackColor = Color.SkyBlue;
                }
            }
            catch (Exception)
            {

            }
        }

        public void AddToList2(string str1, string str2)
        {
            try
            {
                string[] item1 = { str1, str2 };
                listView2.Items.Add(new ListViewItem(item1));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        public void Get_WindowsInfoFromTID(uint ThreadID, out string DesktopName)
        {
            try
            {
                IntPtr hDesk = new IntPtr();
                hDesk = GetThreadDesktop(ThreadID);

                int errCode = Marshal.GetLastWin32Error();
                string errCode_str = errCode.ToString();

                StringBuilder DeskName = new StringBuilder();
                int lpnLength = 0;
                DeskName.Length = 0;

                GetUserObjectInformation(hDesk, UOI.Name, DeskName, 100, ref lpnLength);

                if (errCode == 0 && DeskName.Length > 0)
                {
                    DesktopName = DeskName.ToString();
                }
                else if (errCode == 5)
                {
                    DesktopName = "- (ERROR_ACCESS_DENIED)";
                }
                else if (errCode != 0)
                {
                    DesktopName = "- (error code: " + errCode_str + ")";
                }
                else
                {
                    DesktopName = "- (failed)";
                }
            }
            catch (Exception errw)
            {
                DesktopName = "- ( err:" + errw.Message.ToString() + ")";
            }
        }

        public sealed class SafeWindowStationHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeWindowStationHandle()
                : base(true)
            {

            }

            protected override bool ReleaseHandle()
            {
                return SafeNativeMethods.CloseWindowStation(handle);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindowStations(EnumWindowStationsDelegate lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowStationsDelegate(string windowsStation, IntPtr lParam);
        private static bool EnumWindowStationsCallback(string windowStation, IntPtr lParam)
        {
            GCHandle gch = GCHandle.FromIntPtr(lParam);
            IList<string> list = gch.Target as List<string>;

            if (null == list)
            {
                return (false);
            }

            list.Add(windowStation);

            return (true);
        }

        public void enumwinstation()
        {
            IList<string> list = new List<string>();
            GCHandle gch = GCHandle.Alloc(list);
            EnumWindowStationsDelegate childProc = new EnumWindowStationsDelegate(EnumWindowStationsCallback);

            EnumWindowStations(childProc, GCHandle.ToIntPtr(gch));

            foreach (string ws in list)
            {
                List<string> desktops = WindowStation.EnumerateDesktops(ws);

                foreach (string desktop in desktops)
                {
                    try
                    {
                        AddToList2(ws, desktop);
                    }
                    catch (Exception)
                    {
                        AddToList2("-", "-");
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            enumwinstation();
        }

        enum DESKTOP_ACCESS : uint
        {
            DESKTOP_NONE = 0,
            DESKTOP_READOBJECTS = 0x0001,
            DESKTOP_CREATEWINDOW = 0x0002,
            DESKTOP_CREATEMENU = 0x0004,
            DESKTOP_HOOKCONTROL = 0x0008,
            DESKTOP_JOURNALRECORD = 0x0010,
            DESKTOP_JOURNALPLAYBACK = 0x0020,
            DESKTOP_ENUMERATE = 0x0040,
            DESKTOP_WRITEOBJECTS = 0x0080,
            DESKTOP_SWITCHDESKTOP = 0x0100,

            GENERIC_ALL = (DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW | DESKTOP_CREATEMENU |
                            DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK |
                            DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP),
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("KERNEL32.DLL", SetLastError = true)]
        public static extern int CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttribute,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO si,
            ref PROCESS_INFORMATION pi
        );

        public void CreateProcessWithDesktop(string filepath, string desktopname)
        {
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            STARTUPINFO si = new STARTUPINFO();

            si.dwFlags = 0x00000004;
            si.lpDesktop = desktopname;

            CreateProcess(filepath, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, ref pi);
            int errCode = Marshal.GetLastWin32Error();
        }

        public string GetOwnDesktopName()
        {
            IntPtr SavedDefaultDesktop = GetThreadDesktop(GetCurrentThreadId());
            StringBuilder DeskName = new StringBuilder();
            int lpnLength = 0;
            DeskName.Length = 0;

            GetUserObjectInformation(SavedDefaultDesktop, UOI.Name, DeskName, 100, ref lpnLength);
            return DeskName.ToString();
        }

        public void ChangeDesktop5sec(string WinStationName, string DesktopName)
        {
            IntPtr SavedDefaultDesktop = GetThreadDesktop(GetCurrentThreadId());
            IntPtr hwinstaCurrent = GetProcessWindowStation();

            IntPtr hWinSta = WindowStation.OpenWindowStation(WinStationName, true, 0x37);
            WindowStation.SetProcessWindowStation(hWinSta);

            IntPtr OpenDesk = WindowStation.OpenDesktop(DesktopName, 0, false, (uint)DESKTOP_ACCESS.GENERIC_ALL);
            bool ret_switch = WindowStation.SwitchDesktop(OpenDesk);

            if (ret_switch == true)
            {
                if (checkBox3.Checked == false)
                {
                    System.Threading.Thread.Sleep(5000);

                    WindowStation.SetProcessWindowStation(hwinstaCurrent);
                    WindowStation.SwitchDesktop(SavedDefaultDesktop);
                    WindowStation.SetThreadDesktop(SavedDefaultDesktop);
                }
            }
            else
            {
                WindowStation.SetProcessWindowStation(hwinstaCurrent);
                MessageBox.Show("Switching to the selected desktop has been rejected.", "Faild to switch the desktop", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string OwnDesktopName = "";

        private void Form1_Load(object sender, EventArgs e)
        {
            label3.Text = "";

            if (checkBox2.Checked == true)
            {
                this.TopMost = true;
            }
            else
            {
                this.TopMost = false;
            }

            if (IntPtr.Size == 4 && System.Environment.Is64BitOperatingSystem)
            {
                MessageBox.Show("Please use the 64-bit version application.");
                Application.Exit();
            }

            listView1.ColumnClick +=  new ColumnClickEventHandler(listView1_ColumnClick);

            OwnDesktopName = GetOwnDesktopName();

            string title = this.Text;
            this.Text = title + " --- ( The owner desktop of this process is [" + OwnDesktopName + "] )";
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            listView1.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        public bool Get_CurrentInputDesktop(out IntPtr Sent)
        {
            try
            {
                Sent = IntPtr.Zero;
                Sent = OpenInputDesktop(0, true, (uint)DESKTOP_ACCESS.GENERIC_ALL);

                return true;
            }
            catch (Exception)
            {
                Sent = IntPtr.Zero;
                return false;
            }
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem itemx = listView2.SelectedItems[0];

            DialogResult result = MessageBox.Show("Are you sure you want to switch to selected desktop?" + Environment.NewLine + "  - Desktop Name : [ " + itemx.SubItems[1].Text + " ]",
                "Desktop Switch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                ChangeDesktop5sec(itemx.Text, itemx.SubItems[1].Text);
            }
            else if (result == DialogResult.No)
            {
                //none
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                this.TopMost = true;
            }
            else
            {
                this.TopMost = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem itemx = listView1.SelectedItems[0];

            DialogResult result = MessageBox.Show("Are you sure you want to terminate selected process?" + Environment.NewLine + Environment.NewLine
                + "  - PID : " + itemx.Text + Environment.NewLine
                + "  - ProcessName : " + itemx.SubItems[1].Text,
                "Process Kill",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                Process oProcess = Process.GetProcessById(int.Parse(itemx.Text));
                TerminateProcess(oProcess.Handle, 1);
            }
            else if (result == DialogResult.No)
            {
                //none
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {
                label2.Text = "Please press Refresh button. If you double-click a Desktop Name, you can switch to the clicked desktop forever.";
            }
            else
            {
                label2.Text = "Please press Refresh button. If you double-click a Desktop Name, you can switch to the clicked desktop for 5 sec.";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }
    }
}
