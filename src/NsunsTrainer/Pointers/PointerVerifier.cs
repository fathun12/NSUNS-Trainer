using NsunsTrainer.Native;

namespace NsunsTrainer.Pointers;

public static class PointerVerifier
{
    /// <summary>Final address of a chain using the module base right now. Null if any step cannot be read.</summary>
    public static ulong? Resolve(ProcessMemory memory, PointerChain chain)
    {
        var module = memory.Modules.FirstOrDefault(
            m => m.Name.Equals(chain.ModuleName, StringComparison.OrdinalIgnoreCase));
        if (module is null) return null;

        ulong location = module.BaseAddress + chain.ModuleOffset;
        if (chain.Offsets.Count == 0) return location;

        ulong? current = memory.ReadPointer(location);
        for (int i = 0; i < chain.Offsets.Count; i++)
        {
            if (current is null) return null;

            ulong address = current.Value + chain.Offsets[i];
            if (i == chain.Offsets.Count - 1) return address;

            current = memory.ReadPointer(address);
        }

        return null;
    }
}