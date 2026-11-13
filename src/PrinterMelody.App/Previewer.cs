using NAudio.Wave;
using PrinterMelody.Core;

namespace PrinterMelody.App;

/// <summary>Local preview: renders the melody as a square wave, like the printer buzzer.</summary>
public sealed class Previewer : IDisposable
{
    private const int SampleRate = 44100;
    private WaveOutEvent? _output;

    public static byte[] Render(Melody melody)
    {
        var samples = new List<short>();
        foreach (var note in melody.Notes)
        {
            double seconds = note.Beats * melody.BeatSeconds;
            int total = (int)(seconds * SampleRate);
            int gap = note.IsRest ? total : Math.Min(total, (int)(ScriptGenerator.GapSeconds * SampleRate));
            int tone = total - gap;

            if (!note.IsRest)
            {
                double freq = Melody.Frequency(note.Name, note.Octave);
                for (int i = 0; i < tone; i++)
                {
                    double phase = (i * freq / SampleRate) % 1.0;
                    samples.Add((short)(phase < 0.5 ? 6000 : -6000));
                }
            }
            for (int i = 0; i < gap; i++) samples.Add(0);
        }

        var bytes = new byte[samples.Count * 2];
        Buffer.BlockCopy(samples.ToArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public void Play(Melody melody, Action? onStopped = null)
    {
        Stop();
        byte[] pcm = Render(melody);
        if (pcm.Length == 0) return;

        var stream = new RawSourceWaveStream(new MemoryStream(pcm), new WaveFormat(SampleRate, 16, 1));
        _output = new WaveOutEvent();
        _output.Init(stream);
        _output.PlaybackStopped += (_, _) => onStopped?.Invoke();
        _output.Play();
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
    }

    public void Dispose() => Stop();
}

