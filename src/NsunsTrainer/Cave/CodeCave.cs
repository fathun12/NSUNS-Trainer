using Iced.Intel;
using Decoder = Iced.Intel.Decoder;

namespace NsunsTrainer.Cave;

/// <summary>Writer instruction analysis: base register and displacement of the written address (e.g. [rcx+10h]).</summary>
public sealed record CaveInfo(int BaseReg, long Disp, int Length);

/// <summary>
/// "Tree" cave: the game instruction that writes a value is replaced by a jump to this cave.
/// The cave compares the base register of the writing object with up to N slots (addresses of
/// the player's side). Equal to any slot = same side, the write is skipped. Otherwise the
/// original instruction runs (enemy).
/// </summary>
public static class CodeCave
{
    public static bool TryAnalyze(byte[] original, out CaveInfo? info, out string error)
    {
        info = null;
        error = "";

        if (original.Length < 5)
        {
            error = "The instruction is shorter than 5 bytes, too small for a jump.";
            return false;
        }

        var decoder = Decoder.Create(64, new ByteArrayCodeReader(original));
        decoder.IP = 0x1000;
        var ins = decoder.Decode();

        if (ins.IsInvalid || ins.Length != original.Length)
        {
            error = "The patch bytes are not one complete instruction.";
            return false;
        }
        if (ins.FlowControl != FlowControl.Next)
        {
            error = "This instruction is a branch or call.";
            return false;
        }
        if (ins.IsIPRelativeMemoryOperand)
        {
            error = "The instruction uses a RIP-relative address.";
            return false;
        }
        if (ins.MemoryIndex != Register.None)
        {
            error = "The instruction uses an index register.";
            return false;
        }

        var b = ins.MemoryBase;
        if (b < Register.RAX || b > Register.R15)
        {
            error = "The instruction does not use a 64-bit register as its address base.";
            return false;
        }

        info = new CaveInfo(b - Register.RAX, unchecked((long)ins.MemoryDisplacement64), original.Length);
        return true;
    }

    /// <summary>
    /// Build the cave. Slots (8 bytes each) are at slotsOffset, filled by the caller.
    /// Data block (24 bytes) at dataOffset: +0 skipped count, +4 executed count,
    /// +8 last skipped object, +16 last executed object.
    /// </summary>
    public static byte[] BuildTreeDiag(byte[] original, CaveInfo info, int slotCount,
        ulong caveAddress, ulong instructionAddress, out int slotsOffset, out int dataOffset)
    {
        int n = original.Length;
        ulong ret = instructionAddress + (ulong)n;
        int reg = info.BaseReg;
        var code = new List<byte>(256);
        var fixups = new List<(int Pos, bool IsSlot, int Index)>();
        var jes = new List<int>();

        void RipRef(bool isSlot, int index)
        {
            fixups.Add((code.Count, isSlot, index));
            code.AddRange(new byte[4]);
        }

        code.Add(0x9C);                                        // pushfq
        for (int i = 0; i < slotCount; i++)
        {
            code.Add((byte)(0x48 | (reg >= 8 ? 4 : 0)));       // cmp reg,[rip+slot_i]
            code.Add(0x3B);
            code.Add((byte)(0x05 | ((reg & 7) << 3)));
            RipRef(true, i);
            code.Add(0x0F);                                    // je skip (rel32)
            code.Add(0x84);
            jes.Add(code.Count);
            code.AddRange(new byte[4]);
        }

        code.Add(0xFF);                                        // executed++
        code.Add(0x05);
        RipRef(false, 4);
        code.Add((byte)(0x48 | (reg >= 8 ? 4 : 0)));           // last executed object
        code.Add(0x89);
        code.Add((byte)(0x05 | ((reg & 7) << 3)));
        RipRef(false, 16);
        code.Add(0x9D);                                        // popfq
        code.AddRange(original);
        int j2 = code.Count;
        code.Add(0xE9);
        code.AddRange(BitConverter.GetBytes(Rel(ret, caveAddress + (ulong)j2 + 5)));

        int skipStart = code.Count;
        code.Add(0xFF);                                        // skipped++
        code.Add(0x05);
        RipRef(false, 0);
        code.Add((byte)(0x48 | (reg >= 8 ? 4 : 0)));           // last skipped object
        code.Add(0x89);
        code.Add((byte)(0x05 | ((reg & 7) << 3)));
        RipRef(false, 8);
        code.Add(0x9D);                                        // popfq
        int j1 = code.Count;
        code.Add(0xE9);
        code.AddRange(BitConverter.GetBytes(Rel(ret, caveAddress + (ulong)j1 + 5)));

        slotsOffset = (code.Count + 7) & ~7;
        dataOffset = slotsOffset + slotCount * 8;
        while (code.Count < dataOffset + 24) code.Add(0);

        var bytes = code.ToArray();
        foreach (int at in jes)
            BitConverter.GetBytes(skipStart - (at + 4)).CopyTo(bytes, at);
        foreach (var (pos, isSlot, index) in fixups)
        {
            int target = isSlot ? slotsOffset + index * 8 : dataOffset + index;
            BitConverter.GetBytes(target - (pos + 4)).CopyTo(bytes, pos);
        }
        return bytes;
    }

    private static int Rel(ulong target, ulong next) => checked((int)((long)target - (long)next));
}