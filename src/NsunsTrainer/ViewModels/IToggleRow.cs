namespace NsunsTrainer.ViewModels;

/// <summary>One switch row in the list (code patch, lock-only value, or Instant Awakening).</summary>
public interface IToggleRow
{
    string Title { get; }
    string HotkeyText { get; set; }
    string HotkeyStatus { get; set; }
    bool IsOn { get; set; }
    void HotkeyToggle();
    event Action? HotkeyEdited;
}