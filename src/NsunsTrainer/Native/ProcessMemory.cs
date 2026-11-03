using System.Diagnostics;
using System.Runtime.InteropServices;
using NsunsTrainer.Models;

namespace NsunsTrainer.Native;

/// <summary>Read-only access to one game process.</summary>
public sealed class ProcessMemory : IDisposable
{
    private IntPtr _handle;
    private readonly Process _process;
    private readonly List<ModuleInfo> _modules = new();

    public int ProcessId { get; }
    public string ProcessName { get; }
    public IReadOnlyList<ModuleInfo> Modules => _modules;
    public ModuleInfo? MainModule => _modules.Count > 0 ? _modules[0] : null;

    public bool IsAlive
    {
        get
        {
            try { _process.Refresh(); return !_process.HasExited; }
            catch { return false; }
        }
    }

    private ProcessMemory(IntPtr handle, Process process)
    {
        _handle = handle;
        _process = process;
        ProcessId = process.Id;
        ProcessName = process.ProcessName;
    }

    public static ProcessMemory Attach(int processId)
    {
        Process process;
        try { process = Process.GetProcessById(processId); }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Process not found. The game may have been closed.");
        }

        var handle = NativeMethods.OpenProcess(
            NativeMethods.ProcessVmRead | NativeMethods.ProcessQueryInformation, false, processId);

        if (handle == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            process.Dispose();
            throw new InvalidOperationException(error == 5
                ? "Access denied. Run the trainer as Administrator."
                : $"Failed to open the process (Windows error code {error}).");
        }

        var memory = new ProcessMemory(handle, process);
        memory.RefreshModules();
        return memory;
    }

    public void RefreshModules()
    {
        _modules.Clear();
        try
        {
            _process.Refresh();
            foreach (ProcessModule module in _process.Modules)
            {
                _modules.Add(new ModuleInfo(
                    module.ModuleName,
                    (ulong)module.BaseAddress.ToInt64(),
                    (ulong)module.ModuleMemorySize));
            }
        }
        catch
        {
            // Protected process: leave the list empty.
        }
    }

    public byte[]? Read(ulong address, int size)
    {
        if (_handle == IntPtr.Zero || size <= 0) return null;

        var buffer = new byte[size];
        bool ok = NativeMethods.ReadProcessMemory(_handle, (nuint)address, buffer, (nuint)size, out nuint read);
        return ok && (int)read == size ? buffer : null;
    }

    public bool TryRead<T>(ulong address, out T value) where T : unmanaged
    {
        var bytes = Read(address, Marshal.SizeOf<T>());
        if (bytes is null)
        {
            value = default;
            return false;
        }
        value = MemoryMarshal.Read<T>(bytes);
        return true;
    }

    public ulong? ReadPointer(ulong address) =>
        TryRead<ulong>(address, out var value) ? value : null;

    public IEnumerable<MemoryRegion> EnumerateRegions(bool writableOnly)
    {
        uint mask = writableOnly ? NativeMethods.WritableMask : NativeMethods.ReadableMask;
        ulong address = 0;
        nuint size = (nuint)Marshal.SizeOf<NativeMethods.MemoryBasicInformation>();

        while (address < 0x7FFFFFFFFFFF)
        {
            if (NativeMethods.VirtualQueryEx(_handle, (nuint)address, out var info, size) == 0)
                break;

            bool usable = info.State == NativeMethods.MemCommit
                          && (info.Protect & NativeMethods.PageGuard) == 0
                          && (info.Protect & mask) != 0;

            if (usable)
            {
                bool writable = (info.Protect & NativeMethods.WritableMask) != 0;
                yield return new MemoryRegion(info.BaseAddress, info.RegionSize, info.Protect, writable);
            }

            address = info.BaseAddress + info.RegionSize;
        }
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
        _process.Dispose();
    }
}