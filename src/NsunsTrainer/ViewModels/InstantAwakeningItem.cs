using CommunityToolkit.Mvvm.ComponentModel;
using NsunsTrainer.Data;

namespace NsunsTrainer.ViewModels;

/// <summary>
/// One switch: turns God Mode off, sets HP to 40 and locks it, and turns on Unlimited Awakening.
/// The Awakening trigger in the game is still pressed by the player.
/// </summary>
public partial class InstantAwakeningItem : ObservableObject, IToggleRow
{
    public const string CardLabel = "Instant Awakening";
    private const string HpLabel = "HP";
    private const string GodModeLabel = "God Mode";
    private const string DurationLabel = "Unlimited Awakening";

    private readonly FeatureItem? _hp;
    private readonly PatchItem? _god;
    private readonly PatchItem? _duration;

    public InstantAwakeningItem(IEnumerable<FeatureItem> features, IEnumerable<PatchItem> patches)
    {
        _hp = features.FirstOrDefault(f => f.Label == HpLabel);
        _god = patches.FirstOrDefault(p => p.Title == GodModeLabel);
        _duration = patches.FirstOrDefault(p => p.Title == DurationLabel);

        _statusText = _hp is null || _duration is null
            ? "Not available: the HP value or the Unlimited Awakening patch is missing."
            : "Turns God Mode off, sets HP to 40 and locks it, and removes the Awakening time limit.";
    }

    public string Title => CardLabel;
    public string ActionText => "";

    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private string _statusText;
    [ObservableProperty] private string _hotkeyText = "";
    [ObservableProperty] private string _hotkeyStatus = "";

    public event Action? HotkeyEdited;
    partial void OnHotkeyTextChanged(string value) => HotkeyEdited?.Invoke();

    public void HotkeyToggle() => IsOn = !IsOn;

    partial void OnIsOnChanged(bool value)
    {
        StatusText = value ? Start() : Stop();
    }

    private string Start()
    {
        if (_hp is null || _duration is null)
            return "Not available: the HP value or the Unlimited Awakening patch is missing.";

        if (_god is { IsOn: true }) _god.IsOn = false;

        if (_hp.IsFrozen) _hp.IsFrozen = false;
        _hp.InputText = GameData.AwakeningHp;
        _hp.IsFrozen = true;

        _duration.IsOn = true;

        return $"HP: {_hp.ActionText} | Awakening timer: {_duration.StatusText}";
    }

    private string Stop()
    {
        if (_hp is not null)
        {
            if (_hp.IsFrozen) _hp.IsFrozen = false;
            if (_hp.InputText == GameData.AwakeningHp) _hp.InputText = "";
        }
        if (_duration is not null && _duration.IsOn) _duration.IsOn = false;

        return "Turned off: HP lock released and the Awakening patch is off. God Mode is not turned back on.";
    }

    /// <summary>Called by the 1 second timer: if this is on but the HP lock or the patch turned off or failed, release everything.</summary>
    public void Refresh()
    {
        if (!IsOn) return;
        if (_hp is { IsFrozen: true } && _duration is { IsOn: true }) return;

        string before = StatusText;
        IsOn = false;
        StatusText = "Released automatically because a part turned off or failed. Before: " + before;
    }
}