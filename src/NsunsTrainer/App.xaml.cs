using System.Configuration;
using System.Data;
using System.Windows;

namespace NsunsTrainer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string MutexName = @"Local\NSUNS Trainer single instance";
    private const string ShowEventName = @"Local\NSUNS Trainer show window";

    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool first);
        if (!first)
        {
            // Another trainer is already running (maybe hidden in the tray): ask it to show its window.
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ShowEventName);
                existing.Set();
            }
            catch
            {
            }
            Shutdown();
            return;
        }

        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var listener = new Thread(() =>
        {
            try
            {
                while (_showEvent.WaitOne())
                    Dispatcher.BeginInvoke(new Action(() => (Current.MainWindow as NsunsTrainer.MainWindow)?.ShowFromTray()));
            }
            catch
            {
            }
        }) { IsBackground = true, Name = "ShowWindowListener" };
        listener.Start();

        base.OnStartup(e);
        new NsunsTrainer.MainWindow().Show();
    }
}

