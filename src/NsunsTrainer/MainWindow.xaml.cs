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
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Shutdown();
        _hotkeys?.Dispose();
        base.OnClosed(e);
    }
}