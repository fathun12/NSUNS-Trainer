using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NsunsTrainer.Native;

/// <summary>The only code in this program that writes to game memory.</summary>
public sealed class MemoryWriter : IDisposable
{
    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmWrite = 0x0020;
    private const uint PageExecuteReadWrite = 0x40;
    private const uint ThreadSuspendResume = 0x0002;

    private IntPtr _handle;
    private readonly int _processId;

    private MemoryWriter(IntPtr handle, int processId)
    {
        _handle = handle;
        _processId = processId;
    }

    public static MemoryWriter Open(int processId)
    {
        var handle = OpenProcess(ProcessVmWrite | ProcessVmOperation, false, processId);
        if (handle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(error == 5
                ? "Write access denied. Run the trainer as Administrator."
                : $"Failed to open write access (Windows error code {error}).");
        }

        return new MemoryWriter(handle, processId);
    }

    public bool Write(ulong address, byte[] data)
    {
        if (_handle == IntPtr.Zero || data.Length == 0) return false;

        return WriteProcessMemory(_handle, (nuint)address, data, (nuint)data.Length, out nuint written)
               && (int)written == data.Length;
    }

    /// <summary>Write to game CODE: game threads are paused, protection is changed and restored.</summary>
    public bool WriteCode(ulong address, byte[] data, out string error)
    {
        error = "";
        if (_handle == IntPtr.Zero || data.Length == 0)
        {
            error = "Write access is not available yet.";
            return false;
        }

        var suspended = SuspendAll();
        try
        {
            if (!VirtualProtectEx(_handle, (nuint)address, (nuint)data.Length, PageExecuteReadWrite, out uint oldProtect))
            {
                error = $"Windows refused to change the code protection (error code {Marshal.GetLastWin32Error()}).";
                return false;
            }

            bool ok = WriteProcessMemory(_handle, (nuint)address, data, (nuint)data.Length, out nuint written)
                      && (int)written == data.Length;

            VirtualProtectEx(_handle, (nuint)address, (nuint)data.Length, oldProtect, out _);
            FlushInstructionCache(_handle, (nuint)address, (nuint)data.Length);

            if (!ok) error = "Windows refused the write to game code.";
            return ok;
        }
        finally
        {
            ResumeAll(suspended);
        }
    }

    private List<IntPtr> SuspendAll()
    {
        var list = new List<IntPtr>();
        try
        {
            using var process = Process.GetProcessById(_processId);
            foreach (ProcessThread thread in process.Threads)
            {
                var h = OpenThread(ThreadSuspendResume, false, (uint)thread.Id);
                if (h == IntPtr.Zero) continue;

                if (SuspendThread(h) == uint.MaxValue) { CloseHandle(h); continue; }
                list.Add(h);
            }
        }
        catch
        {
            // Process is gone: nothing was paused.
        }
        return list;
    }

    private static void ResumeAll(List<IntPtr> handles)
    {
        foreach (var h in handles)
        {
            ResumeThread(h);
            CloseHandle(h);
        }
    }

    /// <summary>Allocate RWX memory within 32-bit jump range of an address. Null on failure.</summary>
    public ulong? AllocNear(ulong near, int size)
    {
        if (_handle == IntPtr.Zero) return null;
        const ulong step = 0x10000;
        for (ulong i = 1; i < 0x7000; i++)
        {
            ulong delta = i * step;
            if (near > delta)
            {
                var below = VirtualAllocEx(_handle, (nuint)(near - delta), (nuint)size, 0x3000, PageExecuteReadWrite);
                if (below != 0) return (ulong)below;
            }
            var above = VirtualAllocEx(_handle, (nuint)(near + delta), (nuint)size, 0x3000, PageExecuteReadWrite);
            if (above != 0) return (ulong)above;
        }
        return null;
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(
        IntPtr process, nuint baseAddress, byte[] buffer, nuint size, out nuint bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtectEx(
        IntPtr process, nuint address, nuint size, uint newProtect, out uint oldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlushInstructionCache(IntPtr process, nuint baseAddress, nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(uint desiredAccess, bool inheritHandle, uint threadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SuspendThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(IntPtr thread);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nuint VirtualAllocEx(IntPtr process, nuint address, nuint size, uint allocationType, uint protect);
}