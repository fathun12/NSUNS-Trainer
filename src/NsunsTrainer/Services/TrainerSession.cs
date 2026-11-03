using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NsunsTrainer.Native;

namespace NsunsTrainer.Services;

/// <summary>Connection to the running game. Disconnects by itself when the game closes.</summary>
public partial class TrainerSession : ObservableObject
{
    private const string IdleMessage = "Waiting for NSUNSC to start...";

    private readonly DispatcherTimer _watchdog;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private ProcessMemory? _memory;

    [ObservableProperty]
    private string _statusMessage = IdleMessage;

    public bool IsConnected => Memory is not null;

    /// <summary>Write access. Null when it was denied; the reason is in WriterError.</summary>
    public MemoryWriter? Writer { get; private set; }

    [ObservableProperty]
    private string _writerError = "";

    public TrainerSession()
    {
        _watchdog = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _watchdog.Tick += (_, _) =>
        {
            if (Memory is not null && !Memory.IsAlive)
                Disconnect("The game was closed. Start it again, the trainer reconnects by itself.");
        };
        _watchdog.Start();
    }

    /// <summary>Throws InvalidOperationException (with a readable message) on failure.</summary>
    public void Connect(int processId)
    {
        Disconnect();

        var memory = ProcessMemory.Attach(processId);
        Memory = memory;

        try
        {
            Writer = MemoryWriter.Open(processId);
            WriterError = "";
        }
        catch (InvalidOperationException ex)
        {
            Writer = null;
            WriterError = ex.Message;
        }
        StatusMessage = $"Connected to {memory.ProcessName} (PID {memory.ProcessId}).";
    }

    public void Disconnect(string? message = null)
    {
        Memory?.Dispose();
        Memory = null;
        Writer?.Dispose();
        Writer = null;
        WriterError = "";
        StatusMessage = message ?? IdleMessage;
    }
}