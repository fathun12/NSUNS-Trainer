using NsunsTrainer.Models;
using NsunsTrainer.Native;
using NsunsTrainer.Scanning;

namespace NsunsTrainer.Cave;

public enum PatchState { Original, Patched, Mismatch, Unreadable }

/// <summary>Finds the patch location in the game code through its byte pattern.</summary>
public static class CodePatchLocator
{
    /// <summary>Scans the main module (can take seconds): run it on a background thread.</summary>
    public static (ulong? Address, string Message) Find(ProcessMemory memory, CodePatch patch, CancellationToken ct)
    {
        var module = memory.MainModule;
        if (module is null) return (null, "The game's main module could not be read.");

        if (!AobPattern.TryParse(patch.Pattern, out var pattern, out string error))
            return (null, "The patch pattern is invalid: " + error);

        var hits = AobScanner.Scan(memory, module, pattern, 2, null, ct);
        if (hits.Count == 0)
            return (null, "The code location was not found. The game was probably updated; this trainer needs a new version.");
        if (hits.Count > 1)
            return (null, "The patch pattern matches in more than one place. This trainer needs a new version.");

        return (hits[0] + (ulong)patch.InstructionOffset, "");
    }
}