using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PrinterMelody.Core;

/// <summary>
/// Builds the shell code that plays a melody on the printer buzzer (PWM) and patches it into
/// the stock /usr/bin/beep_sound.sh, which exposes mode1/mode2/mode3 functions.
/// </summary>
public static class ScriptGenerator
{
    /// <summary>Silence inserted after every note so repeated notes can be told apart.</summary>
    public const double GapSeconds = 0.03;

    private static string Sec(double s) => s.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>Returns the body (shell statements) of a mode function.</summary>
    public static string BuildBody(Melody melody)
    {
        var sb = new StringBuilder();
        foreach (var note in melody.Notes)
        {
            double seconds = note.Beats * melody.BeatSeconds;
            if (note.IsRest)
            {
                sb.AppendLine($"    echo 0 > \"$PWM_PATH/enable\"; sleep {Sec(seconds)}");
                continue;
            }

            long period = (long)Math.Round(1e9 / Melody.Frequency(note.Name, note.Octave));
            double play = Math.Max(0.01, seconds - GapSeconds);
            sb.AppendLine($"    play {period} {period / 2} {Sec(play)}");
            sb.AppendLine($"    echo 0 > \"$PWM_PATH/enable\"; sleep {Sec(GapSeconds)}");
        }
        return sb.ToString();
    }

    /// <summary>Replaces the body of the given modes (1..3) in the original script.</summary>
    public static string Patch(string originalScript, Melody melody, IEnumerable<int> modes)
    {
        string script = originalScript.Replace("\r\n", "\n");
        string body = BuildBody(melody);

        foreach (int mode in modes.Distinct())
        {
            if (mode is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(modes));
            string name = $"mode{mode}_sequence";
            var regex = new Regex(Regex.Escape(name) + @"\(\)\n\{.*?\n\}\n", RegexOptions.Singleline);
            if (!regex.IsMatch(script))
                throw new InvalidOperationException($"Function {name} not found in the printer script.");
            string replacement = $"{name}()\n{{\n{body}}}\n";
            script = regex.Replace(script, _ => replacement, 1);
        }
        return script;
    }
}

