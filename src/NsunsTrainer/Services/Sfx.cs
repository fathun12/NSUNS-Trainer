using System.Media;
using System.Reflection;
using System.Windows.Threading;
using NsunsTrainer.ViewModels;

namespace NsunsTrainer.Services;

/// <summary>Plays the on/off click sounds (WAV files embedded in the exe).</summary>
public static class Sfx
{
    private static readonly SoundPlayer? On = Load("Sounds.enable.wav");
    private static readonly SoundPlayer? Off = Load("Sounds.disable.wav");

    private static SoundPlayer? Load(string name)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (stream is null) return null;

            var player = new SoundPlayer(stream);
            player.Load();
            return player;
        }
        catch
        {
            return null;
        }
    }

    public static void Play(bool on)
    {
        try { (on ? On : Off)?.Play(); }
        catch { }
    }

    /// <summary>
    /// Plays the sound after the switch settled. If the switch was refused and went back to its old
    /// position (for example Damage Multiplier while One Hit Kill is on), nothing is played.
    /// </summary>
    public static void PlayWhenSettled(IToggleRow row, bool wanted)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (row.IsOn == wanted) Play(wanted);
        }));
    }
}