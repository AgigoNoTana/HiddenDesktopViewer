using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HiddenDesktopViewer
{
    public static class HandleClass
    {
        public static IEnumerable<string> EnumDesktopHandlesOpened(int pid)
        {
            using (var proc = Process.GetProcessById(pid))
            {
                IntPtr hProcess = proc.Handle;

                foreach (var hItem in EnumHandles((int)pid))
                {
                    IntPtr hObj = IntPtr.Zero;
                    try
                    {
                        if (!NT_SUCCESS(NtDuplicateObject(hProcess, hItem.HandleValue, Process.GetCurrentProcess().Handle, out hObj, 0, 0, 0)))
                        {
                            continue;
                        }

                        using (var nto1 = new NtObject(hObj, ObjectInformationClass.ObjectTypeInformation, typeof(OBJECT_TYPE_INFORMATION)))
                        {
                            var oti = ObjectTypeInformation_FromBuffer(nto1.Buffer);

                            if (oti.Name.ToString() != "Desktop")
                            {
                                continue;
                            }
                        }

                        if (hItem.GrantedAccess == 0x0012019f
                          || hItem.GrantedAccess == 0x001a019f
                          || hItem.GrantedAccess == 0x00100000
                          || hItem.GrantedAccess == 0x00160001
                          || hItem.GrantedAccess == 0x00100001
                          || hItem.GrantedAccess == 0x00100020)
                        {
                            continue;
                        }

                        using (var noje = new NtObject(hObj, ObjectInformationClass.ObjectNameInformation, typeof(OBJECT_NAME_INFORMATION)))
                        {
                            var ObjNInfo = ObjectNameInformation_FromBuffer(noje.Buffer);
                            yield return Get_RegularFileName_FromDevice(ObjNInfo.Name.ToString());
                        }
                    }
                    finally
                    {
                        CloseHandle(hObj);
                    }
                }
            }
        }

        static OBJECT_TYPE_INFORMATION ObjectTypeInformation_FromBuffer(IntPtr buffer)
        {
#if USE_SAFE_CODE_ONLY
      return (OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(buffer, typeof(OBJECT_TYPE_INFORMATION));
#else
            unsafe { return *(OBJECT_TYPE_INFORMATION*)buffer.ToPointer(); }
#endif
        }

        static OBJECT_NAME_INFORMATION ObjectNameInformation_FromBuffer(IntPtr buffer)
        {
#if USE_SAFE_CODE_ONLY
      return (OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(buffer, typeof(OBJECT_NAME_INFORMATION));
#else
            unsafe
            {
                return *(OBJECT_NAME_INFORMATION*)buffer.ToPointer();
            }
#endif
        }

        class NtObject : IDisposable
        {
            public NtObject(IntPtr hObj, ObjectInformationClass infoClass, Type type)
            {
                Init(hObj, infoClass, Marshal.SizeOf(type));
            }

            public NtObject(IntPtr hObj, ObjectInformationClass infoClass, int estimatedSize)
            {
                Init(hObj, infoClass, estimatedSize);
            }

            public void Init(IntPtr hObj, ObjectInformationClass infoClass, int estimatedSize)
            {
                Close();
                Buffer = Query(hObj, infoClass, estimatedSize);
            }

            public void Close()
            {
                if (Buffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(Buffer);
                    Buffer = IntPtr.Zero;
                }
            }

            public void Dispose()
            {
                Close();
            }

            public IntPtr Buffer { get; private set; }


            public enum FileType : uint
            {
                FILE_TYPE_UNKNOWN = 0x0000,
                FILE_TYPE_DISK = 0x0001,
                FILE_TYPE_CHAR = 0x0002,
                FILE_TYPE_PIPE = 0x0003,
                FILE_TYPE_REMOTE = 0x8000,
            }

            public enum STDHandle : uint
            {
                STD_INPUT_HANDLE = unchecked((uint)-10),
                STD_OUTPUT_HANDLE = unchecked((uint)-11),
                STD_ERROR_HANDLE = unchecked((uint)-12),
            }

            [DllImport("Kernel32.dll")]
            static public extern FileType GetFileType(IntPtr hFile);


            public static IntPtr Query(IntPtr hObj, ObjectInformationClass infoClass, int estimatedSize)
            {
                int size = estimatedSize;
                IntPtr buf = Marshal.AllocCoTaskMem(size);
                int retsize = 0;

                while (true)
                {
                    FileType ret_FileType = (FileType)GetFileType(hObj);

                    if (ret_FileType == FileType.FILE_TYPE_PIPE)
                    {
                        Marshal.FreeCoTaskMem(buf);
                        return IntPtr.Zero;
                    }
                    else
                    {
                        var ret = NtQueryObject(hObj, infoClass, buf, size, out retsize);

                        if (NT_SUCCESS(ret))
                        {
                            return buf;
                        }
                        if (ret == NT_STATUS.INFO_LENGTH_MISMATCH || ret == NT_STATUS.BUFFER_OVERFLOW)
                        {
                            buf = Marshal.ReAllocCoTaskMem(buf, retsize);
                            size = retsize;
                        }
                        else
                        {
                            Marshal.FreeCoTaskMem(buf);
                            return IntPtr.Zero;
                        }
                    }
                }
            }
        }

        static readonly string NETWORK_PREFIX = @"\Device\Mup\";

        static string Get_RegularFileName_FromDevice(string strRawName)
        {
            if (strRawName.StartsWith(NETWORK_PREFIX))
            {
                return @"\\" + strRawName.Substring(NETWORK_PREFIX.Length);
            }

            string strFileName = strRawName;

            foreach (var drvPath in Environment.GetLogicalDrives())
            {
                var drv = drvPath.Substring(0, 2);

                var sb = new StringBuilder(MAX_PATH);

                if (QueryDosDevice(drv, sb, MAX_PATH) == 0)
                {
                    return strRawName;
                }

                string drvRoot = sb.ToString();

                if (strFileName.StartsWith(drvRoot))
                {
                    strFileName = drv + strFileName.Substring(drvRoot.Length);

                    break;
                }
            }
            return strFileName;
        }

        static SYSTEM_EXTENDED_HANDLE SystemExtendedHandle_FromPtr(IntPtr ptr, int offset)
        {
#if USE_SAFE_CODE_ONLY
      return (SYSTEM_EXTENDED_HANDLE)Marshal.PtrToStructure(buffer.Offset(offset), typeof(SYSTEM_EXTENDED_HANDLE));
#else
            unsafe
            {
                var p = (byte*)ptr.ToPointer() + offset;
                return *(SYSTEM_EXTENDED_HANDLE*)p;
            }
#endif
        }

        static int lastSizeUsed = 0x10000;

        static IEnumerable<SYSTEM_EXTENDED_HANDLE> EnumHandles(int processId)
        {
            int size = lastSizeUsed;
            IntPtr buffer = Marshal.AllocCoTaskMem(size);
            try
            {
                int required;
                while (NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, size, out required) == NT_STATUS.INFO_LENGTH_MISMATCH)
                {
                    size = required;
                    buffer = Marshal.ReAllocCoTaskMem(buffer, size);
                }

                if (lastSizeUsed < size)
                    lastSizeUsed = size;

                int entrySize = Marshal.SizeOf(typeof(SYSTEM_EXTENDED_HANDLE));
                int offset = Marshal.SizeOf(typeof(IntPtr)) * 2;
                int handleCount = Marshal.ReadInt32(buffer);

                for (int i = 0; i < handleCount; i++)
                {
                    var shi = SystemExtendedHandle_FromPtr(buffer, offset + entrySize * i);
                    if (shi.UniqueProcessId != new IntPtr(processId))
                        continue;

                    yield return shi;
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(buffer);
            }
        }


        [DllImport("kernel32.dll")]
        static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        const int MAX_PATH = 260;

        [DllImport("ntdll.dll")]
        static extern NT_STATUS NtQuerySystemInformation(
          int SystemInformationClass,
          IntPtr SystemInformation,
          int SystemInformationLength,
          out int ReturnLength);

        const int SystemHandleInformation = 16;

        struct SYSTEM_EXTENDED_HANDLE
        {
            public IntPtr Object;
            public IntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;

            public SYSTEM_EXTENDED_HANDLE(IntPtr _Object, IntPtr _UniqueProcessId, IntPtr _HandleValue, uint _GrantedAccess, ushort _CreatorBackTraceIndex, ushort _ObjectTypeIndex, uint _HandleAttributes, uint _Reserved)
            {
                Object = _Object;
                UniqueProcessId = _UniqueProcessId;
                HandleValue = _HandleValue;
                GrantedAccess = _GrantedAccess;
                CreatorBackTraceIndex = _CreatorBackTraceIndex;
                ObjectTypeIndex = _ObjectTypeIndex;
                HandleAttributes = _HandleAttributes;
                Reserved = _Reserved;
            }
        }

        const int SystemExtendedHandleInformation = 64;

        enum NT_STATUS : uint
        {
            SUCCESS = 0x00000000,
            BUFFER_OVERFLOW = 0x80000005,
            INFO_LENGTH_MISMATCH = 0xC0000004
        }

        static bool NT_SUCCESS(NT_STATUS status)
        {
            return ((uint)status & 0x80000000) == 0;
        }

        [DllImport("ntdll.dll")]
        static extern NT_STATUS NtDuplicateObject(
          IntPtr SourceProcessHandle,
          IntPtr SourceHandle,
          IntPtr TargetProcessHandle,
          out IntPtr TargetHandle,
          uint DesiredAccess, uint Attributes, uint Options);

        [DllImport("ntdll.dll")]
        static extern NT_STATUS NtQueryObject(
          IntPtr ObjectHandle,
          ObjectInformationClass ObjectInformationClass,
          IntPtr ObjectInformation,
          int ObjectInformationLength,
          out int returnLength);

        enum ObjectInformationClass : int
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        struct OBJECT_NAME_INFORMATION
        { // Information Class 1
            public UNICODE_STRING Name;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct UNICODE_STRING
        {
            private IntPtr reserved;
            public IntPtr Buffer;

            public ushort Length
            {
                get { return (ushort)(reserved.ToInt64() & 0xffff); }
            }
            public ushort MaximumLength
            {
                get { return (ushort)(reserved.ToInt64() >> 16); }
            }

            public override string ToString()
            {
                if (Buffer == IntPtr.Zero)
                    return "";
                return Marshal.PtrToStringUni(Buffer, Wcslen());
            }

            public int Wcslen()
            {
                unsafe
                {
                    ushort* p = (ushort*)Buffer.ToPointer();
                    for (ushort i = 0; i < Length; i++)
                    {
                        if (p[i] == 0)
                            return i;
                    }
                    return Length;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GENERIC_MAPPING
        {
            public int GenericRead;
            public int GenericWrite;
            public int GenericExecute;
            public int GenericAll;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct OBJECT_TYPE_INFORMATION
        {
            public UNICODE_STRING Name;
            public uint TotalNumberOfObjects;
            public uint TotalNumberOfHandles;
            public uint TotalPagedPoolUsage;
            public uint TotalNonPagedPoolUsage;
            public uint TotalNamePoolUsage;
            public uint TotalHandleTableUsage;
            public uint HighWaterNumberOfObjects;
            public uint HighWaterNumberOfHandles;
            public uint HighWaterPagedPoolUsage;
            public uint HighWaterNonPagedPoolUsage;
            public uint HighWaterNamePoolUsage;
            public uint HighWaterHandleTableUsage;
            public uint InvalidAttributes;
            public GENERIC_MAPPING GenericMapping;
            public uint ValidAccess;
            public byte SecurityRequired;
            public byte MaintainHandleCount;
            public ushort MaintainTypeList;
            public int PoolType;
            public int PagedPoolUsage;
            public int NonPagedPoolUsage;
        }
    }
}