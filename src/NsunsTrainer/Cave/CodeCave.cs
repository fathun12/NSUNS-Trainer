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

    /// <summary>
    /// "Owner" cave for a call hook (E8 rel32, 5 bytes): calls the original target; if the result is 0 and [rdi+18h]
    /// equals one of the slots (effect owner of the player's side), the result is forced to 1. Only rax is touched.
    /// Data block (24 bytes) at dataOffset: +0 forced, +4 left alone, +8 last forced owner, +16 last left-alone owner.
    /// </summary>
    public static byte[] BuildCallOwner(ulong callTarget, int slotCount, ulong caveAddress, ulong instructionAddress,
        out int slotsOffset, out int dataOffset)
    {
        ulong ret = instructionAddress + 5;
        var code = new List<byte>(256);
        var fixups = new List<(int Pos, bool IsSlot, int Index)>();
        var jes = new List<int>();

        void RipRef(bool isSlot, int index)
        {
            fixups.Add((code.Count, isSlot, index));
            code.AddRange(new byte[4]);
        }

        code.Add(0xE8);                                        // call original target
        code.AddRange(BitConverter.GetBytes(Rel(callTarget, caveAddress + 5)));
        code.Add(0x85); code.Add(0xC0);                        // test eax,eax
        code.Add(0x0F); code.Add(0x85);                        // jne done (result is already non-zero)
        int jneAt = code.Count;
        code.AddRange(new byte[4]);
        code.Add(0x48); code.Add(0x8B); code.Add(0x47); code.Add(0x18);   // mov rax,[rdi+18h]
        for (int i = 0; i < slotCount; i++)
        {
            code.Add(0x48); code.Add(0x3B); code.Add(0x05);    // cmp rax,[rip+slot_i]
            RipRef(true, i);
            code.Add(0x0F); code.Add(0x84);                    // je match
            jes.Add(code.Count);
            code.AddRange(new byte[4]);
        }
        code.Add(0xFF); code.Add(0x05);                        // left alone++
        RipRef(false, 4);
        code.Add(0x48); code.Add(0x89); code.Add(0x05);        // last left-alone owner
        RipRef(false, 16);
        code.Add(0x31); code.Add(0xC0);                        // xor eax,eax (original result 0)
        code.Add(0xE9);                                        // jmp done
        int jmpDoneAt = code.Count;
        code.AddRange(new byte[4]);

        int matchStart = code.Count;
        code.Add(0xFF); code.Add(0x05);                        // forced++
        RipRef(false, 0);
        code.Add(0x48); code.Add(0x89); code.Add(0x05);        // last forced owner
        RipRef(false, 8);
        code.Add(0xB8);                                        // mov eax,1
        code.AddRange(BitConverter.GetBytes(1));

        int doneStart = code.Count;
        code.Add(0xE9);                                        // jmp back to the game (after the call)
        int jmpRetAt = code.Count;
        code.AddRange(BitConverter.GetBytes(Rel(ret, caveAddress + (ulong)jmpRetAt + 4)));

        slotsOffset = (code.Count + 7) & ~7;
        dataOffset = slotsOffset + slotCount * 8;
        while (code.Count < dataOffset + 24) code.Add(0);

        var bytes = code.ToArray();
        BitConverter.GetBytes(doneStart - (jneAt + 4)).CopyTo(bytes, jneAt);
        BitConverter.GetBytes(doneStart - (jmpDoneAt + 4)).CopyTo(bytes, jmpDoneAt);
        foreach (int at in jes)
            BitConverter.GetBytes(matchStart - (at + 4)).CopyTo(bytes, at);
        foreach (var (pos, isSlot, index) in fixups)
        {
            int target = isSlot ? slotsOffset + index * 8 : dataOffset + index;
            BitConverter.GetBytes(target - (pos + 4)).CopyTo(bytes, pos);
        }
        return bytes;
    }

    /// <summary>The 7 bytes hooked by TreeDamage: subss xmm0,xmm2 followed by comiss xmm1,xmm0.</summary>
    public static readonly byte[] DamageSiteBytes = { 0xF3, 0x0F, 0x5C, 0xC2, 0x0F, 0x2F, 0xC8 };

    /// <summary>
    /// "TreeDamage" cave (hook on subss xmm0,xmm2 + comiss xmm1,xmm0, 7 bytes). If the base register equals one of the
    /// slots (player's side) the original code runs. Otherwise (enemy) and damage (xmm2) greater than 0, xmm2 is multiplied
    /// by k before the original code runs. No flags are saved: the original comiss sets them again before the next jbe.
    /// Data block (16 bytes) at dataOffset: +0 scaled count, +4 left-alone count, +8 float k.
    /// </summary>
    public static byte[] BuildTreeDamage(byte[] original, CaveInfo info, int slotCount, float k,
        ulong caveAddress, ulong instructionAddress, out int slotsOffset, out int dataOffset)
    {
        int n = original.Length;
        ulong ret = instructionAddress + (ulong)n;
        int reg = info.BaseReg;
        var code = new List<byte>(256);
        var fixups = new List<(int Pos, bool IsSlot, int Index)>();
        var sameJumps = new List<int>();

        void RipRef(bool isSlot, int index)
        {
            fixups.Add((code.Count, isSlot, index));
            code.AddRange(new byte[4]);
        }

        for (int i = 0; i < slotCount; i++)
        {
            code.Add((byte)(0x48 | (reg >= 8 ? 4 : 0)));       // cmp reg,[rip+slot_i]
            code.Add(0x3B);
            code.Add((byte)(0x05 | ((reg & 7) << 3)));
            RipRef(true, i);
            code.Add(0x0F);                                    // je same (rel32)
            code.Add(0x84);
            sameJumps.Add(code.Count);
            code.AddRange(new byte[4]);
        }

        code.Add(0x0F); code.Add(0x2F); code.Add(0xD1);        // comiss xmm2,xmm1 (xmm1 is 0 at the hook site)
        code.Add(0x0F); code.Add(0x86);                        // jbe original (damage <= 0 or NaN)
        int jbeAt = code.Count;
        code.AddRange(new byte[4]);
        code.Add(0xFF); code.Add(0x05);                        // scaled++
        RipRef(false, 0);
        code.Add(0xF3); code.Add(0x0F); code.Add(0x59); code.Add(0x15);   // mulss xmm2,[rip+k]
        RipRef(false, 8);

        int originalStart = code.Count;
        code.AddRange(original);                               // original subss + comiss
        code.Add(0xE9);                                        // jmp back to the game
        int jmpBackAt = code.Count;
        code.AddRange(BitConverter.GetBytes(Rel(ret, caveAddress + (ulong)jmpBackAt + 4)));

        int sameStart = code.Count;
        code.Add(0xFF); code.Add(0x05);                        // left alone++
        RipRef(false, 4);
        code.Add(0xE9);                                        // jmp original
        int jmpOrigAt = code.Count;
        code.AddRange(BitConverter.GetBytes(originalStart - (jmpOrigAt + 4)));

        slotsOffset = (code.Count + 7) & ~7;
        dataOffset = slotsOffset + slotCount * 8;
        while (code.Count < dataOffset + 16) code.Add(0);

        var bytes = code.ToArray();
        BitConverter.GetBytes(originalStart - (jbeAt + 4)).CopyTo(bytes, jbeAt);
        foreach (int at in sameJumps)
            BitConverter.GetBytes(sameStart - (at + 4)).CopyTo(bytes, at);
        foreach (var (pos, isSlot, index) in fixups)
        {
            int target = isSlot ? slotsOffset + index * 8 : dataOffset + index;
            BitConverter.GetBytes(target - (pos + 4)).CopyTo(bytes, pos);
        }
        BitConverter.GetBytes(k).CopyTo(bytes, dataOffset + 8);
        return bytes;
    }

    /// <summary>The 22 bytes hooked by the EXP patch: gain (r15d+edi) to [r14+0E0h], then new total ebx = gain + r11d.</summary>
    public static readonly byte[] ExpSiteBytes =
    {
        0x41, 0x8D, 0x04, 0x3F,
        0x41, 0x89, 0x86, 0xE0, 0x00, 0x00, 0x00,
        0x41, 0x8D, 0x1C, 0x3F,
        0x44, 0x8B, 0x5D, 0x93,
        0x41, 0x03, 0xDB
    };

    /// <summary>
    /// "Exp" cave (replaces the 22 bytes above): gain = r15d+edi is multiplied by k (float), stored in [r14+0E0h],
    /// and used for ebx = gain + r11d. xmm0 is saved and restored. Data block (16 bytes) at dataOffset:
    /// +0 applied count, +4 unused, +8 float k. No side slots.
    /// </summary>
    public static byte[] BuildExpMultiplier(float k, ulong caveAddress, ulong instructionAddress, out int dataOffset)
    {
        ulong ret = instructionAddress + (ulong)ExpSiteBytes.Length;
        var code = new List<byte>(128);
        var fixups = new List<(int Pos, int Index)>();

        void RipRef(int index)
        {
            fixups.Add((code.Count, index));
            code.AddRange(new byte[4]);
        }

        code.Add(0xFF); code.Add(0x05);                                        // applied++
        RipRef(0);
        code.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x10 });                  // sub rsp,10h
        code.AddRange(new byte[] { 0x0F, 0x11, 0x04, 0x24 });                  // movups [rsp],xmm0
        code.AddRange(new byte[] { 0x41, 0x8D, 0x04, 0x3F });                  // lea eax,[r15+rdi]
        code.AddRange(new byte[] { 0xF3, 0x48, 0x0F, 0x2A, 0xC0 });            // cvtsi2ss xmm0,rax
        code.AddRange(new byte[] { 0xF3, 0x0F, 0x59, 0x05 });                  // mulss xmm0,[rip+k]
        RipRef(8);
        code.AddRange(new byte[] { 0xF3, 0x48, 0x0F, 0x2C, 0xC0 });            // cvttss2si rax,xmm0
        code.AddRange(new byte[] { 0x0F, 0x10, 0x04, 0x24 });                  // movups xmm0,[rsp]
        code.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x10 });                  // add rsp,10h
        code.AddRange(new byte[] { 0x41, 0x89, 0x86, 0xE0, 0x00, 0x00, 0x00 }); // mov [r14+0E0h],eax
        code.AddRange(new byte[] { 0x89, 0xC3 });                              // mov ebx,eax
        code.AddRange(new byte[] { 0x44, 0x8B, 0x5D, 0x93 });                  // mov r11d,[rbp-6Dh]
        code.AddRange(new byte[] { 0x41, 0x03, 0xDB });                        // add ebx,r11d
        code.Add(0xE9);                                                        // jmp back to the game
        int j = code.Count;
        code.AddRange(BitConverter.GetBytes(Rel(ret, caveAddress + (ulong)j + 4)));

        dataOffset = (code.Count + 7) & ~7;
        while (code.Count < dataOffset + 16) code.Add(0);

        var bytes = code.ToArray();
        foreach (var (pos, index) in fixups)
            BitConverter.GetBytes(dataOffset + index - (pos + 4)).CopyTo(bytes, pos);
        BitConverter.GetBytes(k).CopyTo(bytes, dataOffset + 8);
        return bytes;
    }

    private static int Rel(ulong target, ulong next) => checked((int)((long)target - (long)next));
}