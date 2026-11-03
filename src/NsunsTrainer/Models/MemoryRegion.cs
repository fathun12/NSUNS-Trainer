namespace NsunsTrainer.Models;

public readonly record struct MemoryRegion(ulong BaseAddress, ulong Size, uint Protect, bool IsWritable);