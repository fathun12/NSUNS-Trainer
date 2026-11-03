namespace NsunsTrainer.Models;

/// <summary>Byte pattern used to find game code again after an update.</summary>
public sealed class SignatureRef
{
    public string Pattern { get; set; } = "";
    public int DispOffset { get; set; } = -1;
    public int InstructionEnd { get; set; }
    public ulong StaticDelta { get; set; }
    public string Offsets { get; set; } = "";
}