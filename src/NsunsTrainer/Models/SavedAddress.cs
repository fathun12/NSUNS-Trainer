namespace NsunsTrainer.Models;

/// <summary>One value in the game: where it lives (chains, then signatures) and its patches.</summary>
public sealed class SavedAddress
{
    public string Label { get; set; } = "";
    public ScanValueType ValueType { get; set; }
    public ulong MainModuleSize { get; set; }
    public List<string> Chains { get; set; } = new();
    public List<SignatureRef> Signatures { get; set; } = new();
    public List<CodePatch> CodePatches { get; set; } = new();
}