using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NsunsTrainer.Data;
using NsunsTrainer.Services;

namespace NsunsTrainer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int FreezeIntervalMs = 10;

    private readonly DispatcherTimer _timer;
    private readonly FeatureItem[] _featureArray;
    private readonly PatchItem[] _patchArray;
    private readonly InstantAwakeningItem _instant;
    private HotkeyService? _hotkeys;

    public TrainerSession Session { get; } = new();
    public ObservableCollection<FeatureItem> Features { get; } = new();
    public ObservableCollection<PatchItem> Patches { get; } = new();
    public IEnumerable<FeatureItem> ValueFeatures => Features.Where(f => !f.IsSwitch && !f.IsHidden);

    /// <summary>All switch rows (patches, Unlimited Team Bar, Instant Awakening) in the order of GameData.ToggleOrder.</summary>
    public ObservableCollection<IToggleRow> Toggles { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasVersionWarning))]
    private string _versionWarning = "";

    public bool HasMessage => !string.IsNullOrEmpty(Message);
    public bool HasVersionWarning => !string.IsNullOrEmpty(VersionWarning);

    /// <summary>True when the game is connected and the player turned something on (a switch or a value lock).</summary>
    public bool ShouldStayInTray => Session.IsConnected && (Toggles.Any(t => t.IsOn) || Features.Any(f => f.IsFrozen));

    public MainViewModel()
    {
        foreach (var address in GameData.Addresses)
        {
            if (GameData.FeatureLabels.Contains(address.Label))
            {
                GameData.AutoLock.TryGetValue(address.Label, out var auto);
                Features.Add(new FeatureItem(address, Session, auto) { IsHidden = GameData.HiddenLabels.Contains(address.Label) });
            }

            foreach (var codePatch in address.CodePatches)
                Patches.Add(new PatchItem(codePatch, address, Session));
        }

        _featureArray = Features.ToArray();
        _patchArray = Patches.ToArray();
        _instant = new InstantAwakeningItem(Features, Patches);

        var rows = new List<IToggleRow>();
        rows.AddRange(Patches);
        rows.AddRange(Features.Where(f => f.IsSwitch));
        rows.Add(_instant);
        foreach (var row in rows.OrderBy(r => OrderOf(r.Title)))
            Toggles.Add(row);

        LoadHotkeys();
        foreach (var t in Toggles) t.HotkeyEdited += OnHotkeyEdited;

        LoadMultipliers();
        foreach (var p in Patches.Where(p => p.ShowMultiplier)) p.MultiplierEdited += SaveMultipliers;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();

        // Lock and patch upkeep run on a background thread so they are not limited by the UI timer.
        var thread = new Thread(FreezeLoop) { IsBackground = true, Name = "FreezeLoop" };
        thread.Start();

        TryAutoConnect();
    }

    // ---------- background thread ----------

    private void FreezeLoop()
    {
        timeBeginPeriod(1);
        try
        {
            while (true)
            {
                try
                {
                    foreach (var feature in _featureArray)
                        feature.FreezeTick();

                    foreach (var patch in _patchArray)
                        patch.Tick();
                }
                catch
                {
                    // One tick failed (e.g. the game just closed): carry on with the next one.
                }

                Thread.Sleep(FreezeIntervalMs);
            }
        }
        finally
        {
            timeEndPeriod(1);
        }
    }

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint period);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint period);

    // ---------- hotkeys ----------

    public void AttachHotkeys(HotkeyService hotkeys)
    {
        _hotkeys = hotkeys;

        foreach (var row in Toggles)
        {
            if (string.IsNullOrWhiteSpace(row.HotkeyText)) continue;

            string? error = _hotkeys.Register(row.HotkeyText, () =>
            {
                bool wanted = !row.IsOn;
                row.HotkeyToggle();
                Sfx.PlayWhenSettled(row, wanted);
            });
            row.HotkeyStatus = error is null ? $"Hotkey: {row.HotkeyText}" : $"Hotkey {row.HotkeyText}: {error}";
        }
    }

    private static int OrderOf(string title)
    {
        int i = Array.IndexOf(GameData.ToggleOrder, title);
        return i < 0 ? int.MaxValue : i;
    }

    // ---------- hotkey editing ----------

    private static readonly string HotkeyFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NSUNS Trainer", "hotkeys.txt");

    private void OnHotkeyEdited()
    {
        if (_hotkeys is null) return;

        foreach (var t in Toggles) t.HotkeyStatus = "";
        _hotkeys.UnregisterAll();
        AttachHotkeys(_hotkeys);
        SaveHotkeys();
    }

    private void LoadHotkeys()
    {
        try
        {
            if (!System.IO.File.Exists(HotkeyFile)) return;
            foreach (var line in System.IO.File.ReadAllLines(HotkeyFile))
            {
                int eq = line.IndexOf('='); 
                if (eq <= 0) continue;
                string title = line.Substring(0, eq);
                string key = line.Substring(eq + 1).Trim();
                foreach (var t in Toggles) if (t.Title == title) t.HotkeyText = key;
            }
        }
        catch { }
    }

    private void SaveHotkeys()
    {
        try
        {
            var lines = Toggles.Select(t => t.Title + "=" + t.HotkeyText);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(HotkeyFile)!);
            System.IO.File.WriteAllLines(HotkeyFile, lines);
        }
        catch { }
    }

    // ---------- damage multiplier settings ----------

    private static readonly string MultiplierFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NSUNS Trainer", "multiplier.txt");

    private void LoadMultipliers()
    {
        try
        {
            if (!System.IO.File.Exists(MultiplierFile)) return;
            foreach (var line in System.IO.File.ReadAllLines(MultiplierFile))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string title = line.Substring(0, eq);
                string value = line.Substring(eq + 1).Trim();
                foreach (var p in Patches) if (p.Title == title) p.LoadMultiplier(value);
            }
        }
        catch { }
    }

    private void SaveMultipliers()
    {
        try
        {
            var lines = Patches.Where(p => p.ShowMultiplier).Select(p => p.Title + "=" + p.MultiplierText);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MultiplierFile)!);
            System.IO.File.WriteAllLines(MultiplierFile, lines);
        }
        catch { }
    }

    // ---------- shutdown ----------

    /// <summary>Puts every patch back to the original bytes. Called when the trainer closes.</summary>
    public void Shutdown()
    {
        if (_instant.IsOn) _instant.IsOn = false;
        foreach (var patch in Patches)
            patch.Restore();
    }

    // ---------- 1 second timer ----------

    private void OnTick()
    {
        if (!Session.IsConnected) TryAutoConnect();
        RefreshVersionWarning();

        var memory = Session.Memory;
        foreach (var feature in Features)
            feature.Update(memory);
        foreach (var patch in Patches)
            patch.Update(memory);
        _instant.Refresh();
    }

    private void TryAutoConnect()
    {
        if (Session.IsConnected) return;

        int? pid = null;
        foreach (var process in Process.GetProcessesByName(GameData.ProcessName))
        {
            pid ??= process.Id;
            process.Dispose();
        }

        if (pid is null)
        {
            Session.StatusMessage = $"Waiting for {GameData.ProcessName} to start...";
            return;
        }

        try
        {
            Session.Connect(pid.Value);
            Message = "";
        }
        catch (InvalidOperationException ex)
        {
            Message = ex.Message;
        }
    }

    private void RefreshVersionWarning()
    {
        var main = Session.Memory?.MainModule;
        if (main is null)
        {
            VersionWarning = "";
            return;
        }

        VersionWarning = main.Size != GameData.ExpectedModuleSize
            ? "The game exe size differs from the version this trainer was made for. The game was probably updated, so some features may not work."
            : "";
    }
}