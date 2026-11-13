using System.Text.Json;

namespace PrinterMelody.Core;

/// <summary>A single note or rest. Name is "C", "C#", "D", ... "B" or "R" for a rest.</summary>
public sealed record Note(string Name, int Octave, double Beats)
{
    public bool IsRest => Name.Equals("R", StringComparison.OrdinalIgnoreCase);

    public override string ToString() =>
        IsRest ? $"Rest  {Beats} beat(s)" : $"{Name}{Octave}  {Beats} beat(s)";
}

public sealed class Melody
{
    public static readonly string[] NoteNames =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public int Bpm { get; set; } = 120;
    public List<Note> Notes { get; set; } = new();

    public double BeatSeconds => 60.0 / Bpm;

    public double TotalSeconds => Notes.Sum(n => n.Beats) * BeatSeconds;

    /// <summary>Frequency in Hz using equal temperament (A4 = 440 Hz).</summary>
    public static double Frequency(string name, int octave)
    {
        int semitone = Array.IndexOf(NoteNames, name.ToUpperInvariant());
        if (semitone < 0) throw new ArgumentException($"Unknown note '{name}'.", nameof(name));
        int midi = 12 * (octave + 1) + semitone;
        return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
    }

    public string ToJson() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static Melody FromJson(string json) =>
        JsonSerializer.Deserialize<Melody>(json) ?? new Melody();
}
