using CommunityToolkit.Mvvm.ComponentModel;
using NsunsTrainer.Cave;
using NsunsTrainer.Models;
using NsunsTrainer.Native;
using NsunsTrainer.Pointers;
using NsunsTrainer.Services;

namespace NsunsTrainer.ViewModels;

/// <summary>
/// One code patch (tree mode). The game instruction that writes the value is redirected to a cave
/// that skips the write for every character on the player's side and runs it for enemies.
/// </summary>
public partial class PatchItem : ObservableObject, IToggleRow
{
    private readonly CodePatch _patch;
    private readonly SavedAddress _owner;
    private readonly TrainerSession _session;
    private readonly List<PointerChain> _chains = new();
    private readonly object _lock = new();
    private readonly CaveInfo? _cave;
    private readonly string _caveError = "";

    private string _errorText = "";
    private DateTime _errorUntil = DateTime.MinValue;
    private ulong? _address;
    private ulong _caveAddr;
    private int _slotOffset;
    private int _dataOffset;
    private ulong[]? _lastTree;
    private int _pid;
    private int _searchedPid;
    private volatile bool _searching;
    private volatile bool _active;
    private string _searchError = "";
    private bool _syncing;

    public PatchItem(CodePatch patch, SavedAddress owner, TrainerSession session)
    {
        _patch = patch;
        _owner = owner;
        _hotkeyText = patch.Hotkey;
        _session = session;

        foreach (var line in owner.Chains)
        {
            if (PointerChain.TryParse(line, out var chain)) _chains.Add(chain);
        }

        string error = "";
        if (CodePatch.TryParseBytes(patch.OriginalBytes, out var original) &&
            TryAnalyzeAny(original, out var info, out error))
            _cave = info;
        else
            _caveError = error.Length > 0 ? error : "The patch bytes are invalid.";
    }

    public string Title => _patch.Label;
    [ObservableProperty] private string _hotkeyText = "";
    public event Action? HotkeyEdited;
    partial void OnHotkeyTextChanged(string value) => HotkeyEdited?.Invoke();
    private bool IsTreeOwner => string.Equals(_patch.Mode, "TreeOwner", StringComparison.OrdinalIgnoreCase);

    private string ActiveText => IsTreeOwner
        ? "Active: effect timers of your whole side are frozen, enemies are not."
        : "Active: your whole side is protected, enemies are not.";

    public string ActionText => "";

    public string Detail => IsTreeOwner
        ? "Effect timers (e.g. Awakening) of your whole side do not run out; enemy timers still do."
        : "Your whole side is protected; enemies are not affected.";

    /// <summary>Normal patches: the writer instruction. TreeOwner: one call instruction (E8 rel32).</summary>
    private bool TryAnalyzeAny(byte[] original, out CaveInfo? info, out string error)
    {
        if (!IsTreeOwner) return CodeCave.TryAnalyze(original, out info, out error);

        info = null;
        error = "";
        if (original.Length != 5 || original[0] != 0xE8)
        {
            error = "TreeOwner needs a single call instruction (5 bytes starting with E8).";
            return false;
        }

        info = new CaveInfo(1, _patch.BaseDisp, 5);
        return true;
    }

    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private string _statusText = "Waiting for the game...";
    [ObservableProperty] private string _hotkeyStatus = "";

    private ulong? Address
    {
        get { lock (_lock) return _address; }
        set { lock (_lock) _address = value; }
    }

    public void HotkeyToggle() => IsOn = !IsOn;

    // ---------- background thread: keeps the side slots correct ----------

    public void Tick()
    {
        if (!_active || _cave is null) return;

        var memory = _session.Memory;
        var writer = _session.Writer;
        if (memory is null || writer is null) return;

        ulong cave;
        int slotOffset;
        lock (_lock)
        {
            cave = _caveAddr;
            slotOffset = _slotOffset;
        }
        if (cave == 0) return;

        var tree = ComputeTreeSlots(memory);
        if (tree is null) return;

        ulong[]? prev;
        lock (_lock) prev = _lastTree;
        if (prev is not null && prev.AsSpan().SequenceEqual(tree)) return;

        var buf = new byte[tree.Length * 8];
        for (int i = 0; i < tree.Length; i++)
            BitConverter.GetBytes(tree[i]).CopyTo(buf, i * 8);
        if (writer.Write(cave + (ulong)slotOffset, buf))
            lock (_lock) _lastTree = tree;
    }

    /// <summary>Value of the base register for the player = value address minus the instruction displacement.</summary>
    private ulong? ComputeSlot(ProcessMemory memory)
    {
        var info = _cave;
        if (info is null) return null;

        var result = AddressResolver.Resolve(memory, _chains, _owner.Signatures, _owner.ValueType);
        return result.Status == ResolveStatus.Ok
            ? unchecked(result.Address - (ulong)info.Disp)
            : null;
    }

    /// <summary>Whole player side = nodes linked from the player's object. Result: base register value per character, the rest zero.</summary>
    private ulong[]? ComputeTreeSlots(ProcessMemory memory)
    {
        var playerBase = ComputeSlot(memory);
        if (playerBase is null || playerBase.Value < SideTree.BaseFromNode) return null;

        var nodes = SideTree.Collect(memory, playerBase.Value - SideTree.BaseFromNode, SideTree.MaxNodes);
        if (nodes is null) return null;

        var slots = new ulong[SideTree.MaxNodes];
        int count = Math.Min(nodes.Count, slots.Length);

        if (IsTreeOwner)
        {
            // Owner mode: each slot holds the effect owner pointer of one character; unused slots hold an impossible value.
            Array.Fill(slots, ulong.MaxValue);
            for (int i = 0; i < count; i++)
            {
                var b = memory.Read(nodes[i] + (ulong)_patch.OwnerOffset, 8);
                if (b is null) continue;
                ulong v = BitConverter.ToUInt64(b, 0);
                if (v > 0x10000) slots[i] = v;
            }
            return slots;
        }

        for (int i = 0; i < count; i++)
            slots[i] = nodes[i] + SideTree.BaseFromNode;
        if (count > 2) Array.Sort(slots, 1, count - 1);
        return slots;
    }

    private string Diagnostic(ProcessMemory memory)
    {
        ulong cave;
        int data;
        lock (_lock) { cave = _caveAddr; data = _dataOffset; }
        if (cave == 0 || data == 0) return "";

        var bytes = memory.Read(cave + (ulong)data, 8);
        if (bytes is null) return "";

        uint skip = BitConverter.ToUInt32(bytes, 0);
        uint run = BitConverter.ToUInt32(bytes, 4);
        return IsTreeOwner
            ? $" | forced {skip}x, left alone {run}x"
            : $" | skipped {skip}x, ran {run}x";
    }

    // ---------- reading (1 second timer) ----------

    public void Update(ProcessMemory? memory)
    {
        if (memory is null)
        {
            Address = null;
            _pid = 0;
            _searchedPid = 0;
            _active = false;
            lock (_lock) { _caveAddr = 0; }
            StatusText = "Waiting for the game...";
            SetOn(false);
            return;
        }

        if (memory.ProcessId != _pid)
        {
            _pid = memory.ProcessId;
            Address = null;
            _active = false;
            lock (_lock) { _caveAddr = 0; }
            _searchError = "";
        }

        if (_cave is null)
        {
            StatusText = "This patch cannot be used: " + _caveError;
            SetOn(false);
            return;
        }

        var address = Address;
        if (address is null)
        {
            if (!_searching && _searchedPid != _pid) StartSearch(memory);

            StatusText = _searching || _searchedPid != _pid
                ? "Searching for the code location (once, can take a few seconds)..."
                : _searchError;
            return;
        }

        var state = Inspect(memory, address.Value, out string detail);
        switch (state)
        {
            case PatchState.Original:
                _active = false;
                StatusText = DateTime.UtcNow < _errorUntil ? _errorText : "Ready. The patch is off.";
                SetOn(false);
                break;
            case PatchState.Patched:
                _active = true;
                StatusText = ActiveText + Diagnostic(memory);
                SetOn(true);
                break;
            default:
                _active = false;
                StatusText = detail;
                SetOn(false);
                break;
        }
    }

    private void StartSearch(ProcessMemory memory)
    {
        _searching = true;
        _searchedPid = memory.ProcessId;
        int pid = memory.ProcessId;

        Task.Run(() =>
        {
            try
            {
                var (address, message) = CodePatchLocator.Find(memory, _patch, CancellationToken.None);
                if (_pid == pid)
                {
                    _searchError = message;
                    Address = address;
                }
            }
            catch (Exception ex)
            {
                _searchError = "Could not find the code location: " + ex.Message;
            }
            finally
            {
                _searching = false;
            }
        });
    }

    // ---------- check the bytes at the code location ----------

    private PatchState Inspect(ProcessMemory memory, ulong address, out string detail)
    {
        detail = "";
        if (!CodePatch.TryParseBytes(_patch.OriginalBytes, out var original))
        {
            detail = "The patch data is invalid.";
            return PatchState.Unreadable;
        }

        var now = memory.Read(address, original.Length);
        if (now is null)
        {
            detail = "The code at that location is not readable yet.";
            return PatchState.Unreadable;
        }

        if (now.AsSpan().SequenceEqual(original)) return PatchState.Original;

        ulong cave;
        lock (_lock) cave = _caveAddr;
        if (now[0] == 0xE9 && cave != 0)
        {
            int rel = BitConverter.ToInt32(now, 1);
            ulong target = unchecked(address + 5UL + (ulong)(long)rel);
            if (target == cave) return PatchState.Patched;
        }

        detail = $"The bytes at the code location differ (found {CodePatch.ToHex(now)}, expected {CodePatch.ToHex(original)}). " +
                 "The game was updated, or a patch from a previous trainer session is still in place (restart the game).";
        return PatchState.Mismatch;
    }

    // ---------- on / off ----------

    partial void OnIsOnChanged(bool value)
    {
        if (_syncing) return;

        string error = Apply(value);
        if (error.Length == 0)
        {
            StatusText = value ? ActiveText : "Patch turned off.";
            return;
        }

        StatusText = error;
        _errorText = error;
        _errorUntil = DateTime.UtcNow.AddSeconds(20);
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
        {
            SetOn(!value);
            StatusText = error;
        }));
    }

    private string Apply(bool on)
    {
        var memory = _session.Memory;
        var writer = _session.Writer;
        if (memory is null) return "The game is not connected.";
        if (writer is null)
            return _session.WriterError.Length > 0 ? _session.WriterError : "Write access is not available.";

        var address = Address;
        if (address is null)
            return "The code location was not found yet. " + (_searchError.Length > 0 ? _searchError : "Wait for the search to finish.");

        var state = Inspect(memory, address.Value, out string detail);
        if (state is PatchState.Mismatch or PatchState.Unreadable)
            return detail + " Nothing was written.";

        if ((on && state == PatchState.Patched) || (!on && state == PatchState.Original))
        {
            _active = on;
            return "";
        }

        if (!CodePatch.TryParseBytes(_patch.OriginalBytes, out var original))
            return "The patch data is invalid.";

        byte[] bytes;
        if (!on)
        {
            bytes = original;
        }
        else
        {
            string prepared = PrepareCave(memory, writer, address.Value, original, out bytes);
            if (prepared.Length > 0) return prepared;
        }

        if (!writer.WriteCode(address.Value, bytes, out string error)) return error;

        var after = Inspect(memory, address.Value, out _);
        if (after != (on ? PatchState.Patched : PatchState.Original))
            return "The bytes were written but read back different. The game probably overwrote them.";

        _active = on;
        return "";
    }

    /// <summary>Builds the cave in game memory and returns the jump bytes that replace the writer instruction.</summary>
    private string PrepareCave(ProcessMemory memory, MemoryWriter writer, ulong site, byte[] original, out byte[] jump)
    {
        jump = Array.Empty<byte>();

        var info = _cave;
        if (info is null) return "This patch cannot be used: " + _caveError;

        var tree = ComputeTreeSlots(memory);
        if (tree is null)
            return "Your side was not found yet. Enter a match, move your character, then turn it on again.";

        ulong cave;
        lock (_lock) cave = _caveAddr;

        if (cave == 0)
        {
            var allocated = writer.AllocNear(site, 0x1000);
            if (allocated is null) return "Could not allocate memory near the game code for the patch.";
            cave = allocated.Value;
        }

        byte[] code;
        int slotOffset, dataOffset;
        if (IsTreeOwner)
        {
            if (original.Length != 5 || original[0] != 0xE8) return "The patch bytes are not a call (E8 + 4 bytes).";
            long callRel = BitConverter.ToInt32(original, 1);
            ulong callTarget = unchecked((ulong)((long)site + 5 + callRel));
            code = CodeCave.BuildCallOwner(callTarget, SideTree.MaxNodes, cave, site, out slotOffset, out dataOffset);
        }
        else
        {
            code = CodeCave.BuildTreeDiag(original, info, SideTree.MaxNodes, cave, site, out slotOffset, out dataOffset);
        }
        for (int i = 0; i < tree.Length; i++)
            BitConverter.GetBytes(tree[i]).CopyTo(code, slotOffset + i * 8);

        if (!writer.Write(cave, code)) return "Could not write the patch code into game memory.";

        long rel = (long)cave - (long)(site + 5);
        if (rel > int.MaxValue || rel < int.MinValue) return "The patch memory is too far from the game code.";

        jump = new byte[original.Length];
        Array.Fill(jump, (byte)0x90);
        jump[0] = 0xE9;
        BitConverter.GetBytes((int)rel).CopyTo(jump, 1);

        lock (_lock)
        {
            _caveAddr = cave;
            _slotOffset = slotOffset;
            _dataOffset = dataOffset;
            _lastTree = tree;
        }
        return "";
    }

    /// <summary>Puts the original bytes back (when the trainer closes). The cave is left in place, it is harmless.</summary>
    public void Restore()
    {
        try
        {
            _active = false;

            var memory = _session.Memory;
            var writer = _session.Writer;
            var address = Address;
            if (memory is null || writer is null || address is null) return;

            if (Inspect(memory, address.Value, out _) == PatchState.Patched &&
                CodePatch.TryParseBytes(_patch.OriginalBytes, out var original))
            {
                writer.WriteCode(address.Value, original, out _);
            }
        }
        catch
        {
            // The game is gone: nothing to restore.
        }
        finally
        {
            SetOn(false);
        }
    }

    private void SetOn(bool value)
    {
        if (IsOn == value) return;
        _syncing = true;
        try { IsOn = value; }
        finally { _syncing = false; }
    }
}