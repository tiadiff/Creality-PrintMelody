using PrinterMelody.Core;
using Xunit;

namespace PrinterMelody.Tests;

public class MelodyTests
{
    [Fact]
    public void A4_Is_440Hz() => Assert.Equal(440.0, Melody.Frequency("A", 4), 3);

    [Fact]
    public void C6_Is_About_1046Hz() => Assert.Equal(1046.50, Melody.Frequency("C", 6), 1);

    [Fact]
    public void Json_RoundTrip_Preserves_Notes()
    {
        var m = new Melody { Bpm = 100, Notes = { new Note("C", 5, 1), new Note("R", 0, 0.5) } };
        var back = Melody.FromJson(m.ToJson());
        Assert.Equal(100, back.Bpm);
        Assert.Equal(m.Notes, back.Notes);
    }
}

public class ScriptGeneratorTests
{
    private const string Original =
        "#!/bin/sh\nmode1_sequence()\n{\n    echo one\n}\n\nmode2_sequence()\n{\n    echo two\n}\n\nmode3_sequence()\n{\n    echo three\n}\n";

    [Fact]
    public void Body_Uses_Period_And_Half_Duty()
    {
        var m = new Melody { Bpm = 120, Notes = { new Note("A", 4, 1) } };
        string body = ScriptGenerator.BuildBody(m);
        Assert.Contains("play 2272727 1136363 0.470", body); // 440 Hz, 0.5 s minus 30 ms gap
    }

    [Fact]
    public void Rest_Becomes_Sleep()
    {
        var m = new Melody { Bpm = 60, Notes = { new Note("R", 0, 2) } };
        Assert.Contains("sleep 2.000", ScriptGenerator.BuildBody(m));
    }

    [Fact]
    public void Patch_Only_Replaces_Selected_Modes()
    {
        var m = new Melody { Notes = { new Note("C", 5, 1) } };
        string patched = ScriptGenerator.Patch(Original, m, new[] { 1 });
        Assert.DoesNotContain("echo one", patched);
        Assert.Contains("echo two", patched);
        Assert.Contains("echo three", patched);
    }

    [Fact]
    public void Patch_Throws_When_Function_Missing() =>
        Assert.Throws<InvalidOperationException>(() =>
            ScriptGenerator.Patch("#!/bin/sh\n", new Melody(), new[] { 1 }));
}
