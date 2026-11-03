using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NsunsTrainer.Pointers;

/// <summary>
/// Path to one address: Module|OffsetHex|off1,off2,...
/// No offsets: the address is Module+Offset itself. Otherwise read the pointer there,
/// add off1, read again, add off2, and so on; the last sum is the target address.
/// </summary>
public sealed record PointerChain(string ModuleName, ulong ModuleOffset, IReadOnlyList<ulong> Offsets)
{
    public static bool TryParse(string line, [NotNullWhen(true)] out PointerChain? chain)
    {
        chain = null;
        var parts = line.Split('|');
        if (parts.Length != 3) return false;

        if (!ulong.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var moduleOffset))
            return false;

        var offsets = new List<ulong>();
        if (parts[2].Length > 0)
        {
            foreach (var piece in parts[2].Split(','))
            {
                if (!ulong.TryParse(piece, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var offset))
                    return false;
                offsets.Add(offset);
            }
        }

        chain = new PointerChain(parts[0], moduleOffset, offsets);
        return true;
    }
}