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

    /// <summary>"Tree" (default) or "TreeOwner" (hook on a call: effect timers of the player's side are skipped).</summary>
    public string Mode { get; set; } = "Tree";

    /// <summary>TreeOwner only: displacement of the player's value card instruction, used to find the player's node.</summary>
    public int BaseDisp { get; set; }

    /// <summary>TreeOwner only: offset of the effect owner pointer inside a character node.</summary>
    public int OwnerOffset { get; set; }

    /// <summary>TreeDamage only: damage factor applied to the enemy side (1,000,000,000 = one hit kill).</summary>
    public double Multiplier { get; set; }

    /// <summary>TreeDamage only: true when the player may change Multiplier in the UI (1 to 10).</summary>
    public bool Adjustable { get; set; }

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