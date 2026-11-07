using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Numerics;
using NsunsTrainer.Data;
using NsunsTrainer.Models;
using NsunsTrainer.Native;
using NsunsTrainer.Pointers;
using NsunsTrainer.Scanning;
using NsunsTrainer.Services;

namespace NsunsTrainer.ViewModels;

/// <summary>One value card (e.g. Gold Ticket, Team Bar): read, Set, Lock, optional auto-lock.</summary>
public partial class FeatureItem : ObservableObject, IToggleRow
{
    private const double MinValue = 0;
    private const double MaxValue = 1_000_000_000;

    private readonly SavedAddress _saved;
    private readonly TrainerSession _session;
    private readonly List<PointerChain> _chains = new();
    private readonly string? _autoLock;
    private ulong _frozenRaw;
    private ulong? _lastRaw;
    private int _autoPid;
    private volatile bool _freezeHeld;
    private readonly long? _maxInput;

    public FeatureItem(SavedAddress saved, TrainerSession session, string? autoLock)
    {
        _saved = saved;
        _session = session;
        _autoLock = autoLock;
        if (GameData.MaxInputs.TryGetValue(saved.Label, out long maxInput)) _maxInput = maxInput;
        if (!string.IsNullOrWhiteSpace(autoLock)) { _inputText = autoLock; _hotkeyText = "F5"; }
        foreach (var line in saved.Chains)
        {
            if (PointerChain.TryParse(line, out var chain)) _chains.Add(chain);
        }
    }

    /// <summary>True when the value is only locked on or off (auto-lock): shown as a hotkey row.</summary>
    public bool IsSwitch => !string.IsNullOrWhiteSpace(_autoLock);
    public string Label => _saved.Label;

    /// <summary>Helper cards (e.g. HP for Instant Awakening) are updated and lockable but not listed under VALUES.</summary>
    public bool IsHidden { get; init; }

    /// <summary>Switch view of the lock, so the card can sit in the shared switch list.</summary>
    public bool IsOn
    {
        get => IsFrozen;
        set => IsFrozen = value;
    }
    public string Title => IsSwitch ? "Unlimited " + _saved.Label : _saved.Label;
    [ObservableProperty] private string _hotkeyText = "";
    public event Action? HotkeyEdited;
    partial void OnHotkeyTextChanged(string value) => HotkeyEdited?.Invoke();
    public void HotkeyToggle() => IsFrozen = !IsFrozen;

    [ObservableProperty] private string _valueText = "-";
    [ObservableProperty] private string _statusText = "Waiting for the game...";
    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private string _actionText = "";
    [ObservableProperty] private bool _isFrozen;
    [ObservableProperty] private string _hotkeyStatus = "";

    /// <summary>A typed whole number above the game's own maximum becomes that maximum immediately (works for pasted numbers too).</summary>
    partial void OnInputTextChanged(string value)
    {
        if (_maxInput is null || string.IsNullOrWhiteSpace(value)) return;

        string t = value.Trim();
        foreach (char c in t)
            if (c < '0' || c > '9') return;

        if (BigInteger.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var typed) && typed > _maxInput.Value)
        {
            InputText = _maxInput.Value.ToString(CultureInfo.InvariantCulture);
            ActionText = $"Capped at the game's maximum ({InputText}).";
        }
    }

    private bool IsSane(ulong raw)
    {
        double v = ToDouble(_saved.ValueType, raw);
        return !double.IsNaN(v) && !double.IsInfinity(v) && v >= MinValue && v <= MaxValue;
    }

    // ---------- reading (1 second timer) ----------

    public void Update(ProcessMemory? memory)
    {
        _lastRaw = null;

        if (memory is null)
        {
            ValueText = "-";
            StatusText = "Waiting for the game...";
            if (IsFrozen) IsFrozen = false;
            return;
        }

        var result = AddressResolver.Resolve(memory, _chains, _saved.Signatures, _saved.ValueType);

        switch (result.Status)
        {
            case ResolveStatus.Ok:
                _lastRaw = result.Raw;
                ValueText = ValueCodec.Format(_saved.ValueType, result.Raw);
                StatusText = result.Source == ResolveSource.Chain
                    ? "Address found."
                    : "Address found through signature.";
                // Auto-lock is off: the toggle or the hotkey turns the lock on.
                return;

            case ResolveStatus.SignatureSearching:
                ValueText = "...";
                StatusText = "Searching for the address (once, can take a few seconds)...";
                return;
        }

        ValueText = "?";
        StatusText = "Address not found. The game may not have reached the screen that holds this value yet, or it was updated.";
    }

    /// <summary>Locks the value once per game process when an auto-lock value is defined.</summary>
    private void TryAutoLock(ProcessMemory memory)
    {
        if (string.IsNullOrWhiteSpace(_autoLock) || IsFrozen || _autoPid == memory.ProcessId) return;
        if (_lastRaw is null) return;

        _autoPid = memory.ProcessId;
        InputText = _autoLock;
        IsFrozen = true;
    }

    // ---------- Set ----------

    [RelayCommand]
    private void Apply()
    {
        ActionText = string.IsNullOrWhiteSpace(InputText)
            ? "Type a value first."
            : SetFromText(InputText);
    }

    private string SetFromText(string text) =>
        ValueCodec.TryParse(_saved.ValueType, text, out ulong raw)
            ? WriteOnce(raw)
            : "Type a valid number.";

    private string WriteOnce(ulong raw)
    {
        var type = _saved.ValueType;

        if (!IsSane(raw)) return $"Value rejected: outside the safe range ({MinValue} to {MaxValue}).";

        var memory = _session.Memory;
        var writer = _session.Writer;
        if (memory is null) return "The game is not connected.";
        if (writer is null)
            return _session.WriterError.Length > 0 ? _session.WriterError : "Write access is not available.";

        var result = AddressResolver.Resolve(memory, _chains, _saved.Signatures, type);
        if (result.Status != ResolveStatus.Ok)
            return "Address not found in this game, nothing was written.";

        if (!IsSane(result.Raw))
            return "The current value at the address is outside the safe range. The address is probably wrong, nothing was written.";

        if (!writer.Write(result.Address, ToBytes(type, raw)))
            return "Windows refused the write (memory may be protected).";

        if (IsFrozen) _frozenRaw = raw;

        int size = ValueCodec.SizeOf(type);
        var back = memory.Read(result.Address, size);
        if (back is null || ValueCodec.ReadRaw(back, 0, size) != raw)
            return "The value was written but reads back different. The game probably overwrote it.";

        return $"Written: {ValueCodec.Format(type, raw)}.";
    }

    // ---------- Lock ----------

    partial void OnIsFrozenChanged(bool value)
    {
        _freezeHeld = false;
        OnPropertyChanged(nameof(IsOn));

        if (!value)
        {
            ActionText = "Lock released.";
            return;
        }

        ulong raw;
        if (string.IsNullOrWhiteSpace(InputText))
        {
            if (_lastRaw is null)
            {
                Release("The current value is not readable yet, nothing was locked.");
                return;
            }

            raw = _lastRaw.Value;
            InputText = ValueCodec.Format(_saved.ValueType, raw);
        }
        else if (!ValueCodec.TryParse(_saved.ValueType, InputText, out raw))
        {
            Release("Type a valid number before locking.");
            return;
        }

        if (!IsSane(raw))
        {
            Release($"Value rejected: outside the safe range ({MinValue} to {MaxValue}).");
            return;
        }

        if (_session.Memory is null || _session.Writer is null)
        {
            Release(_session.WriterError.Length > 0 ? _session.WriterError : "The game is not connected.");
            return;
        }

        _frozenRaw = raw;
        ActionText = $"Value locked at {ValueCodec.Format(_saved.ValueType, raw)}.";
    }

    private void Release(string message)
    {
        ActionText = message;
        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            IsFrozen = false;
            ActionText = message;
        }));
    }

    private void Post(string message) =>
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() => ActionText = message));

    /// <summary>Called by the background thread. The address is resolved every tick, so a stale address is never written.</summary>
    public void FreezeTick()
    {
        if (!IsFrozen) return;

        var memory = _session.Memory;
        var writer = _session.Writer;
        if (memory is null || writer is null) return;

        var result = AddressResolver.Resolve(memory, _chains, _saved.Signatures, _saved.ValueType);
        if (result.Status != ResolveStatus.Ok) return;

        if (!IsSane(result.Raw))
        {
            if (!_freezeHeld)
            {
                _freezeHeld = true;
                Post("Lock paused: the value at this address is outside the safe range. Not writing.");
            }
            return;
        }

        if (_freezeHeld)
        {
            _freezeHeld = false;
            Post($"Value locked at {ValueCodec.Format(_saved.ValueType, _frozenRaw)}.");
        }

        if (result.Raw != _frozenRaw)
            writer.Write(result.Address, ToBytes(_saved.ValueType, _frozenRaw));
    }

    // ---------- helpers ----------

    private static double ToDouble(ScanValueType type, ulong raw) => type switch
    {
        ScanValueType.Int32 => (int)(uint)raw,
        ScanValueType.Float => BitConverter.UInt32BitsToSingle((uint)raw),
        ScanValueType.Int64 => (long)raw,
        _ => BitConverter.UInt64BitsToDouble(raw)
    };

    private static byte[] ToBytes(ScanValueType type, ulong raw) =>
        ValueCodec.SizeOf(type) == 4 ? BitConverter.GetBytes((uint)raw) : BitConverter.GetBytes(raw);
}