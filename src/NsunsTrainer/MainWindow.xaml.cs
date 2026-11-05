using System.Windows;
using System.Windows.Media;
using NsunsTrainer.Services;
using NsunsTrainer.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace NsunsTrainer;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel = new();
    private HotkeyService? _hotkeys;
    private System.Windows.Forms.NotifyIcon? _tray;
    private bool _exiting;

    public MainWindow()
    {
        // Hanko red-orange accent for toggles and primary buttons.
        ApplicationAccentColorManager.Apply(Color.FromRgb(0xE4, 0x57, 0x2E), ApplicationTheme.Dark);

        DataContext = _viewModel;
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hotkeys = new HotkeyService(this);
        _viewModel.AttachHotkeys(_hotkeys);
        CreateTray();
    }

    private void OnToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is IToggleRow row)
            Sfx.PlayWhenSettled(row, toggle.IsChecked == true);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Game connected and something is on: hide to the tray instead of closing. Otherwise close for real.
        if (!_exiting && _tray is not null && _viewModel.ShouldStayInTray)
        {
            e.Cancel = true;
            Hide();
            _tray.Visible = true;
            _tray.ShowBalloonTip(2000, "NSUNS Trainer", "Still running. Open or exit from this icon.", System.Windows.Forms.ToolTipIcon.Info);
            return;
        }

        base.OnClosing(e);
    }

    private void CreateTray()
    {
        try
        {
            System.Windows.Application.Current.SessionEnding += (_, _) => _exiting = true;

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => ShowFromTray());
            menu.Items.Add("Exit", null, (_, _) => ExitFromTray());

            var tray = new System.Windows.Forms.NotifyIcon { Text = "NSUNS Trainer", ContextMenuStrip = menu, Visible = false };
            string? path = Environment.ProcessPath;
            if (path is not null) tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            tray.DoubleClick += (_, _) => ShowFromTray();
            _tray = tray;
        }
        catch
        {
            _tray = null;
        }
    }

    public void ShowFromTray()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        if (_tray is not null) _tray.Visible = false;
    }

    private void ExitFromTray()
    {
        _exiting = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Shutdown();
        _hotkeys?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        base.OnClosed(e);
    }
}