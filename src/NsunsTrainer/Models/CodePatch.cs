using System.Globalization;

namespace NsunsTrainer.Models;

/// <summary>One code patch: the game instruction that is redirected into a cave.</summary>
public sealed class CodePatch
{
    public string Label { get; set; } = "";
    public string Pattern { get; set; } = "";
    public int InstructionOffset { get; set; }
    public string OriginalBytes { get; set; } = "";
    public string Hotkey { get; set; } = "";

    public static string ToHex(IEnumerable<byte> bytes) => string.Join(' ', bytes.Select(b => b.ToString("X2")));

    public static bool TryParseBytes(string? text, out byte[] bytes)
    {
        var tokens = (text ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        bytes = new byte[tokens.Length];
        if (tokens.Length == 0) return false;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!byte.TryParse(tokens[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                return false;
        }
        return true;
    }
}