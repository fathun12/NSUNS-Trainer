using NsunsTrainer.Models;

namespace NsunsTrainer.Data;

/// <summary>Everything this trainer knows about NSUNSC, embedded in the exe (no profile files needed).</summary>
public static class GameData
{
    public const string ProcessName = "NSUNSC";

    /// <summary>Exe size when the addresses were found. A different size means the game was updated.</summary>
    public const ulong ExpectedModuleSize = 166223872;

    /// <summary>Value cards (Set and Lock). All other addresses only exist to find the player for patches.</summary>
    public static readonly string[] FeatureLabels = { "Gold Ticket", "Team Bar", "HP" };

    /// <summary>Value that is locked automatically once the game is found.</summary>
    public static readonly Dictionary<string, string> AutoLock = new() { ["Team Bar"] = "100" };

    /// <summary>Helper cards that are not listed under VALUES (HP is needed by Instant Awakening).</summary>
    public static readonly string[] HiddenLabels = { "HP" };

    /// <summary>HP that Instant Awakening locks (the game needs HP 40 before Awakening can start).</summary>
    public const string AwakeningHp = "40";

    /// <summary>Order of the switch list. Titles not listed go last, in data order.</summary>
    public static readonly string[] ToggleOrder =
    {
        "God Mode", "Unlimited Chakra", "Damage Multiplier", "One Hit Kill", "EXP Multiplier",
        "Instant Awakening", "Unlimited Awakening",
        "Team Tag", "Tag Switch", "Unlimited Kawarimi", "Unlimited Team Bar"
    };

    private const string PlayerSig = "24 10 57 48 83 EC 20 48 8B 3D ?? ?? ?? ?? 33 F6 48 8B";

    public static readonly List<SavedAddress> Addresses = new()
    {
        new SavedAddress
        {
            Label = "EXP",
            ValueType = ScanValueType.Int32,
            MainModuleSize = ExpectedModuleSize,
            CodePatches =
            {
                new CodePatch
                {
                    Label = "EXP Multiplier",
                    Pattern = "41 8D 04 3F 41 89 86 E0 00 00 00 41 8D 1C 3F 44 8B 5D 93 41 03 DB B8 7F 96 98 00 3B D8 0F 43 D8",
                    InstructionOffset = 0,
                    OriginalBytes = "41 8D 04 3F 41 89 86 E0 00 00 00 41 8D 1C 3F 44 8B 5D 93 41 03 DB",
                    Mode = "Exp",
                    Multiplier = 2,
                    Adjustable = true,
                    Hotkey = ""
                }
            }
        },
        new SavedAddress
        {
            Label = "Gold Ticket",
            ValueType = ScanValueType.Int32,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21DA00C|",
                "NSUNSC.exe|21934D0|100,148,4C",
                "NSUNSC.exe|2317820|18,2D8,4C",
                "NSUNSC.exe|2335D48|328,1A8,4C",
                "NSUNSC.exe|2335D50|228,1A8,4C",
                "NSUNSC.exe|2335D58|B8,1A8,4C",
                "NSUNSC.exe|2335D60|258,1A8,4C",
                "NSUNSC.exe|21D7560|70,2C8,2D8,4C",
                "NSUNSC.exe|2335CD8|80,30,108,4C",
                "NSUNSC.exe|2335D10|D8,388,1A8,4C",
                "NSUNSC.exe|2335D18|98,388,1A8,4C",
                "NSUNSC.exe|2335D28|8,388,1A8,4C",
                "NSUNSC.exe|2335D48|B0,328,1A8,4C",
                "NSUNSC.exe|2335D50|18,1F8,1A8,4C",
                "NSUNSC.exe|2335D58|18,48,1A8,4C",
                "NSUNSC.exe|2335D60|48,1F8,1A8,4C",
                "NSUNSC.exe|2338400|8,388,1A8,4C"
            }
        },
        new SavedAddress
        {
            Label = "HP",
            ValueType = ScanValueType.Float,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21D74B8|250,0,38",
                "NSUNSC.exe|21D74C0|170,0,38",
                "NSUNSC.exe|21D74C8|60,0,38",
                "NSUNSC.exe|21D7550|80,0,38",
                "NSUNSC.exe|21D74A8|320,3B0,0,38",
                "NSUNSC.exe|21D74B0|30,3B0,0,38",
                "NSUNSC.exe|21D74B8|30,1D0,0,38",
                "NSUNSC.exe|21D74C0|30,E0,0,38",
                "NSUNSC.exe|21D7508|2E0,3B0,0,38",
                "NSUNSC.exe|21D7510|120,3B0,0,38",
                "NSUNSC.exe|21D7570|200,3B0,0,38"
            },
            Signatures =
            {
                new SignatureRef { Pattern = PlayerSig, DispOffset = 10, InstructionEnd = 14, StaticDelta = 0, Offsets = "250,0,38" }
            },
            CodePatches =
            {
                new CodePatch
                {
                    Label = "God Mode",
                    Pattern = "?? ?? ?? ?? ?? 76 ?? C7 41 10 00 00 00",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 11 41 10",
                    Hotkey = "F2"
                },
                new CodePatch
                {
                    Label = "Unlimited Awakening",
                    Pattern = "44 8D 42 FB ?? ?? ?? ?? ?? 85 C0 75 ?? 8B 4F 10 85 C9 78 49 F3 0F 2C 05 ?? ?? ?? ?? 2B C8 89 4F 10 85 C9 7F 38 48 8B CF 89 6F 10 E8 ?? ?? ?? ??",
                    InstructionOffset = 4,
                    OriginalBytes = "E8 16 45 05 00",
                    Mode = "TreeOwner",
                    BaseDisp = 16,
                    OwnerOffset = 48,
                    Hotkey = ""
                },
                new CodePatch
                {
                    Label = "Damage Multiplier",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 76 ?? C7 41 10 00 00 00",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 5C C2 0F 2F C8",
                    Mode = "TreeDamage",
                    BaseDisp = 16,
                    Multiplier = 2,
                    Adjustable = true,
                    Hotkey = ""
                },
                new CodePatch
                {
                    Label = "One Hit Kill",
                    Pattern = "?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 76 ?? C7 41 10 00 00 00",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 5C C2 0F 2F C8",
                    Mode = "TreeDamage",
                    BaseDisp = 16,
                    Multiplier = 1000000000,
                    Adjustable = false,
                    Hotkey = ""
                }
            }
        },
        new SavedAddress
        {
            Label = "Chakra",
            ValueType = ScanValueType.Float,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21D74B8|250,0,40",
                "NSUNSC.exe|21D74C0|170,0,40",
                "NSUNSC.exe|21D74C8|60,0,40",
                "NSUNSC.exe|21D7550|80,0,40",
                "NSUNSC.exe|21D74A8|320,3B0,0,40",
                "NSUNSC.exe|21D74B0|30,3B0,0,40",
                "NSUNSC.exe|21D74B8|30,1D0,0,40",
                "NSUNSC.exe|21D74C0|30,E0,0,40",
                "NSUNSC.exe|21D7508|2E0,3B0,0,40",
                "NSUNSC.exe|21D7510|120,3B0,0,40",
                "NSUNSC.exe|21D7570|200,3B0,0,40"
            },
            Signatures =
            {
                new SignatureRef { Pattern = PlayerSig, DispOffset = 10, InstructionEnd = 14, StaticDelta = 0, Offsets = "250,0,40" }
            },
            CodePatches =
            {
                new CodePatch
                {
                    Label = "Unlimited Chakra",
                    Pattern = "?? ?? ?? ?? ?? 76 ?? C7 41 18 00 00 00",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 11 41 18",
                    Hotkey = "F3"
                }
            }
        },
        new SavedAddress
        {
            Label = "Team Tag",
            ValueType = ScanValueType.Float,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21D74B0|740,10,54",
                "NSUNSC.exe|21D74B8|250,10,54",
                "NSUNSC.exe|21D74C0|170,10,54",
                "NSUNSC.exe|21D74C8|60,10,54",
                "NSUNSC.exe|21D7550|80,10,54",
                "NSUNSC.exe|21D74A0|4F0,7A0,10,54",
                "NSUNSC.exe|21D74A8|230,7A0,10,54",
                "NSUNSC.exe|21D74B0|18,710,10,54",
                "NSUNSC.exe|21D74B8|30,1D0,10,54",
                "NSUNSC.exe|21D74C0|30,E0,10,54",
                "NSUNSC.exe|21D7508|1F0,7A0,10,54",
                "NSUNSC.exe|21D7510|30,7A0,10,54",
                "NSUNSC.exe|21D7570|110,7A0,10,54"
            },
            Signatures =
            {
                new SignatureRef { Pattern = "38 7F 00 48 8B 35 ?? ?? ?? ?? 48 85 F6 74", DispOffset = 6, InstructionEnd = 10, StaticDelta = 0, Offsets = "740,10,54" }
            },
            CodePatches =
            {
                new CodePatch
                {
                    Label = "Team Tag",
                    Pattern = "?? ?? ?? ?? ?? 76 ?? C7 40 2C 00 00 00",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 11 40 2C",
                    Hotkey = "F4"
                },
                new CodePatch
                {
                    Label = "Tag Switch",
                    Pattern = "F5 FF FF 48 85 C0 74 ?? ?? ?? ?? ?? ?? 0F 28 74 24 20 48 83 C4 38 C3 CC CC CC CC CC CC CC CC CC CC CC 48 83 EC",
                    InstructionOffset = 8,
                    OriginalBytes = "F3 0F 11 70 2C",
                    Hotkey = "F7"
                }
            }
        },
        new SavedAddress
        {
            Label = "Team Bar",
            ValueType = ScanValueType.Float,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21D74B0|754",
                "NSUNSC.exe|21D74B8|264",
                "NSUNSC.exe|21D74C0|184",
                "NSUNSC.exe|21D74C8|74",
                "NSUNSC.exe|21D7550|94",
                "NSUNSC.exe|21D2C48|320,264",
                "NSUNSC.exe|21D2C80|290,264",
                "NSUNSC.exe|21D5048|320,264",
                "NSUNSC.exe|21D7448|320,264",
                "NSUNSC.exe|21D74A0|4F0,7B4",
                "NSUNSC.exe|21D74A8|230,7B4",
                "NSUNSC.exe|21D74B0|18,724",
                "NSUNSC.exe|21D74B8|30,1E4",
                "NSUNSC.exe|21D74C0|30,F4",
                "NSUNSC.exe|21D7508|1F0,7B4",
                "NSUNSC.exe|21D7510|30,7B4",
                "NSUNSC.exe|21D7570|110,7B4",
                "NSUNSC.exe|21C8990|90,580,7B4",
                "NSUNSC.exe|21D08A8|58,610,264",
                "NSUNSC.exe|21D2C48|48,2E0,264"
            }
        },
        new SavedAddress
        {
            Label = "Kawarimi",
            ValueType = ScanValueType.Float,
            MainModuleSize = ExpectedModuleSize,
            Chains =
            {
                "NSUNSC.exe|21D74B0|740,0,48",
                "NSUNSC.exe|21D74B8|250,0,48",
                "NSUNSC.exe|21D74C0|170,0,48",
                "NSUNSC.exe|21D74C8|60,0,48",
                "NSUNSC.exe|21D7518|6C8,90,638",
                "NSUNSC.exe|21D7520|648,90,638",
                "NSUNSC.exe|21D7528|5F8,90,638",
                "NSUNSC.exe|21D7530|4C8,90,638",
                "NSUNSC.exe|21D7538|1C8,90,638",
                "NSUNSC.exe|21D7540|A8,90,638",
                "NSUNSC.exe|21D7550|80,0,48",
                "NSUNSC.exe|21D7558|2A8,90,638",
                "NSUNSC.exe|21D75E0|58,90,638",
                "NSUNSC.exe|21D74A0|4F0,7A0,0,48",
                "NSUNSC.exe|21D74A8|230,7A0,0,48",
                "NSUNSC.exe|21D74B0|18,710,0,48",
                "NSUNSC.exe|21D74B8|30,1D0,0,48",
                "NSUNSC.exe|21D74C0|30,E0,0,48",
                "NSUNSC.exe|21D7508|1F0,7A0,0,48",
                "NSUNSC.exe|21D7510|30,7A0,0,48"
            },
            CodePatches =
            {
                new CodePatch
                {
                    Label = "Unlimited Kawarimi",
                    Pattern = "?? ?? ?? ?? ?? C3 CC CC CC CC CC CC CC CC CC CC CC CC 48 83 EC 38 0F 29 74 24 20 0F 28 F2 E8 ?? ?? ?? ?? 48 85 C0 74 ?? F3 0F 11 70 20 0F 28 74 24 20 48 83 C4",
                    InstructionOffset = 0,
                    OriginalBytes = "F3 0F 11 51 20",
                    Hotkey = "F9"
                }
            }
        }
    };
}