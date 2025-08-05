using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MRDX.Base.BattleSimulator;

/// <summary>
///     Allows processes to be automatically killed if this parent process unexpectedly quits.
///     This feature requires Windows 8 or greater. On Windows 7, nothing is done.
/// </summary>
/// <remarks>
///     References:
///     https://stackoverflow.com/a/4657392/386091
///     https://stackoverflow.com/a/9164742/386091
/// </remarks>
public sealed partial class ChildProcessManager : IDisposable
{
    private bool _disposed;
    private SafeJobHandle? _handle;

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
                (uint)length)) throw new InvalidOperationException("Unable to set information", new Win32Exception());
    }

    public void Dispose()
    {
        if (_disposed) return;

        _handle?.Dispose();
        _handle = null;
        _disposed = true;
    }

    [MemberNotNull(nameof(_handle))]
    private void ValidateDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed || _handle is null, this);
    }

    public void AddProcess(SafeProcessHandle processHandle)
    {
        ValidateDisposed();
        if (!AssignProcessToJobObject(_handle, processHandle))
            throw new InvalidOperationException("Unable to add the process");
    }

    public void AddProcess(Process process)
    {
        AddProcess(process.SafeHandle);
    }

    public void AddProcess(int processId)
    {
        using var process = Process.GetProcessById(processId);
        AddProcess(process);
    }

    [LibraryImport("kernel32", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateJobObject(IntPtr a, string? lpName);

    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(SafeJobHandle hJob, JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

    [LibraryImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(SafeJobHandle job, SafeProcessHandle process);

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
        private static extern bool CloseHandle(IntPtr hObject);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
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
    private struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    private enum JobObjectInfoType
    {
        AssociateCompletionPortInformation = 7,
        BasicLimitInformation = 2,
        BasicUIRestrictions = 4,
        EndOfJobTimeInformation = 6,
        ExtendedLimitInformation = 9,
        SecurityLimitInformation = 5,
        GroupInformation = 11
    }
}