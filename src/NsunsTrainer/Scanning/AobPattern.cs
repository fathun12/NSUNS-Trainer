using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace NsunsTrainer.Scanning;

/// <summary>Byte pattern with wildcards, e.g. "48 8B ?? 58".</summary>
public sealed class AobPattern
{
    public const int MaxLength = 256;

    private AobPattern(byte[] bytes, bool[] wildcard, int anchorIndex)
    {
        Bytes = bytes;
        Wildcard = wildcard;
        AnchorIndex = anchorIndex;
    }

    public byte[] Bytes { get; }
    public bool[] Wildcard { get; }
    public int Length => Bytes.Length;
    public int AnchorIndex { get; }

    public static bool TryParse(string? text, [NotNullWhen(true)] out AobPattern? pattern, out string error)
    {
        pattern = null;
        error = "";

        var tokens = (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            error = "Pola kosong.";
            return false;
        }
        if (tokens.Length > MaxLength)
        {
            error = $"Pola terlalu panjang (maksimal {MaxLength} byte).";
            return false;
        }

        var bytes = new byte[tokens.Length];
        var wild = new bool[tokens.Length];
        int anchor = -1;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (token == "?" || token == "??")
            {
                wild[i] = true;
                continue;
            }

            if (token.Length != 2 ||
                !byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
            {
                error = $"\"{token}\" bukan byte yang valid.";
                return false;
            }

            if (anchor < 0) anchor = i;
        }

        if (anchor < 0)
        {
            error = "Pola hanya berisi ??.";
            return false;
        }

        pattern = new AobPattern(bytes, wild, anchor);
        return true;
    }

    public bool MatchesAt(byte[] data, int offset)
    {
        if (offset < 0 || offset + Bytes.Length > data.Length) return false;
        for (int i = 0; i < Bytes.Length; i++)
            if (!Wildcard[i] && data[offset + i] != Bytes[i]) return false;
        return true;
    }

    public override string ToString() =>
        string.Join(' ', Bytes.Select((b, i) => Wildcard[i] ? "??" : b.ToString("X2")));
}