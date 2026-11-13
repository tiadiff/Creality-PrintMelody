using PrinterMelody.Core;

namespace PrinterMelody.App;

public sealed class MainForm : Form
{
    private static readonly double[] Durations = { 0.25, 0.5, 0.75, 1, 1.5, 2, 3, 4 };

    private readonly Melody _melody = new() { Bpm = 127 };
    private readonly Previewer _previewer = new();

    private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly ComboBox _note = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
    private readonly NumericUpDown _octave = new() { Minimum = 2, Maximum = 8, Value = 5, Width = 50 };
    private readonly ComboBox _duration = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 70 };
    private readonly NumericUpDown _bpm = new() { Minimum = 30, Maximum = 300, Value = 127, Width = 60 };

    private readonly TextBox _host = new() { Text = "192.168.78.54", Width = 120 };
    private readonly TextBox _user = new() { Text = "root", Width = 70 };
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Width = 120 };
    private readonly CheckBox[] _modes =
    {
        new() { Text = "Start print", Checked = true, AutoSize = true },
        new() { Text = "End print", Checked = true, AutoSize = true },
        new() { Text = "Alert", Checked = false, AutoSize = true },
    };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(4) };

    public MainForm()
    {
        Text = "Printer Melody";
        Width = 640;
        Height = 560;
        MinimumSize = new Size(620, 500);

        _note.Items.AddRange(Melody.NoteNames.Cast<object>().Append("R").ToArray());
        _note.SelectedIndex = 0;
        foreach (double d in Durations) _duration.Items.Add(d);
        _duration.SelectedItem = 1.0;

        Controls.Add(_list);
        Controls.Add(BuildBottomPanel());
        Controls.Add(BuildTopPanel());
        Controls.Add(_status);

        _bpm.ValueChanged += (_, _) => { _melody.Bpm = (int)_bpm.Value; UpdateStatus(); };
        FormClosed += (_, _) => _previewer.Dispose();
        UpdateStatus();
    }

    private Control BuildTopPanel()
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(6), WrapContents = true };
        p.Controls.Add(new Label { Text = "Note", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        p.Controls.Add(_note);
        p.Controls.Add(new Label { Text = "Octave", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        p.Controls.Add(_octave);
        p.Controls.Add(new Label { Text = "Beats", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        p.Controls.Add(_duration);
        p.Controls.Add(Button("Add", (_, _) => AddNote()));
        p.Controls.Add(Button("Remove", (_, _) => RemoveSelected()));
        p.Controls.Add(Button("Up", (_, _) => MoveSelected(-1)));
        p.Controls.Add(Button("Down", (_, _) => MoveSelected(1)));
        p.Controls.Add(Button("Clear", (_, _) => { _melody.Notes.Clear(); Refresh(); }));
        p.Controls.Add(new Label { Text = "BPM", AutoSize = true, Padding = new Padding(10, 6, 0, 0) });
        p.Controls.Add(_bpm);
        return p;
    }

    private Control BuildBottomPanel()
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 130, Padding = new Padding(6), WrapContents = true };
        p.Controls.Add(Button("Preview", (_, _) => _previewer.Play(_melody)));
        p.Controls.Add(Button("Stop", (_, _) => _previewer.Stop()));
        p.Controls.Add(Button("Save...", (_, _) => Save()));
        p.Controls.Add(Button("Load...", (_, _) => LoadMelody()));

        p.SetFlowBreak(p.Controls[^1], true);
        p.Controls.Add(new Label { Text = "Printer", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        p.Controls.Add(_host);
        p.Controls.Add(_user);
        p.Controls.Add(new Label { Text = "Password", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        p.Controls.Add(_password);

        p.SetFlowBreak(p.Controls[^1], true);
        foreach (var m in _modes) p.Controls.Add(m);

        p.SetFlowBreak(p.Controls[^1], true);
        p.Controls.Add(Button("Send to printer", async (_, _) => await SendAsync()));
        p.Controls.Add(Button("Test on printer", async (_, _) => await TestAsync()));
        p.Controls.Add(Button("Restore original", async (_, _) => await RestoreAsync()));
        return p;
    }

    private static Button Button(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, AutoSize = true };
        b.Click += onClick;
        return b;
    }

    private void AddNote()
    {
        string name = (string)_note.SelectedItem!;
        _melody.Notes.Add(new Note(name, (int)_octave.Value, (double)_duration.SelectedItem!));
        Refresh();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedIndex < 0) return;
        int i = _list.SelectedIndex;
        _melody.Notes.RemoveAt(i);
        Refresh();
        _list.SelectedIndex = Math.Min(i, _list.Items.Count - 1);
    }

    private void MoveSelected(int delta)
    {
        int i = _list.SelectedIndex, j = i + delta;
        if (i < 0 || j < 0 || j >= _melody.Notes.Count) return;
        (_melody.Notes[i], _melody.Notes[j]) = (_melody.Notes[j], _melody.Notes[i]);
        Refresh();
        _list.SelectedIndex = j;
    }

    public override void Refresh()
    {
        _list.Items.Clear();
        for (int i = 0; i < _melody.Notes.Count; i++)
            _list.Items.Add($"{i + 1,2}.  {_melody.Notes[i]}");
        UpdateStatus();
        base.Refresh();
    }

    private void UpdateStatus() =>
        _status.Text = $"{_melody.Notes.Count} notes, {_melody.TotalSeconds:0.0} s at {_melody.Bpm} BPM";

    private void Save()
    {
        using var dlg = new SaveFileDialog { Filter = "Melody (*.json)|*.json" };
        if (dlg.ShowDialog() == DialogResult.OK) File.WriteAllText(dlg.FileName, _melody.ToJson());
    }

    private void LoadMelody()
    {
        using var dlg = new OpenFileDialog { Filter = "Melody (*.json)|*.json" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var loaded = Melody.FromJson(File.ReadAllText(dlg.FileName));
        _melody.Bpm = loaded.Bpm;
        _melody.Notes = loaded.Notes;
        _bpm.Value = Math.Clamp(loaded.Bpm, (int)_bpm.Minimum, (int)_bpm.Maximum);
        Refresh();
    }

    private PrinterClient Client() => new(_host.Text.Trim(), _user.Text.Trim(), _password.Text);

    private int[] SelectedModes() =>
        _modes.Select((c, i) => (c, mode: i + 1)).Where(x => x.c.Checked).Select(x => x.mode).ToArray();

    private async Task SendAsync()
    {
        if (_melody.Notes.Count == 0) { _status.Text = "Add some notes first."; return; }
        var modes = SelectedModes();
        if (modes.Length == 0) { _status.Text = "Select at least one mode."; return; }
        await RunAsync("Sending...", async client =>
        {
            if (await client.IsPrintingAsync())
                return "The printer is printing: sending is blocked until the print is finished.";
            return await Task.Run(() => client.Install(_melody, modes));
        });
    }

    private Task TestAsync() => RunAsync("Playing...", async client =>
    {
        if (await client.IsPrintingAsync())
            return "The printer is printing: test blocked.";
        await Task.Run(() => client.Play(SelectedModes().FirstOrDefault(1)));
        return "Played on the printer.";
    });

    private Task RestoreAsync() => RunAsync("Restoring...", async client =>
    {
        if (await client.IsPrintingAsync())
            return "The printer is printing: restore blocked.";
        await Task.Run(client.Restore);
        return "Original script restored.";
    });

    private async Task RunAsync(string busy, Func<PrinterClient, Task<string>> action)
    {
        _status.Text = busy;
        try { _status.Text = await action(Client()); }
        catch (Exception ex) { _status.Text = "Error: " + ex.Message; }
    }
}
