using System.Text;
using System.Text.Json;
using Renci.SshNet;

namespace PrinterMelody.Core;

/// <summary>Talks to the printer over SSH (script install) and Moonraker HTTP (print state).</summary>
public sealed class PrinterClient
{
    public const string ScriptPath = "/usr/bin/beep_sound.sh";
    public const string BackupDir = "/mnt/UDISK/audio_backup";
    public const string BackupPath = BackupDir + "/beep_sound.sh.orig";

    private readonly string _host;
    private readonly string _user;
    private readonly string _password;

    public PrinterClient(string host, string user, string password)
    {
        _host = host;
        _user = user;
        _password = password;
    }

    /// <summary>True when Klipper reports an active print or an active virtual SD card job.</summary>
    public async Task<bool> IsPrintingAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        string json = await http.GetStringAsync(
            $"http://{_host}:7125/printer/objects/query?print_stats&virtual_sdcard", ct);
        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("result").GetProperty("status");
        string state = status.GetProperty("print_stats").GetProperty("state").GetString() ?? "";
        bool active = status.GetProperty("virtual_sdcard").GetProperty("is_active").GetBoolean();
        return active || state is "printing";
    }

    private SshClient Connect()
    {
        var ssh = new SshClient(_host, _user, _password);
        ssh.Connect();
        return ssh;
    }

    private static string Run(SshClient ssh, string command)
    {
        using var cmd = ssh.CreateCommand(command);
        cmd.Execute();
        return cmd.Result;
    }

    /// <summary>Installs the melody on the chosen modes. The original script is backed up once.</summary>
    public string Install(Melody melody, IEnumerable<int> modes)
    {
        using var ssh = Connect();

        Run(ssh, $"mkdir -p {BackupDir}; [ -e {BackupPath} ] || cp -p {ScriptPath} {BackupPath}");
        string original = Run(ssh, $"cat {ScriptPath}");
        string patched = ScriptGenerator.Patch(original, melody, modes);

        UploadText(ssh, "/tmp/beep_new.sh", patched);
        string check = Run(ssh, "sh -n /tmp/beep_new.sh 2>&1 && echo SYNTAX_OK");
        if (!check.Contains("SYNTAX_OK"))
        {
            Run(ssh, "rm -f /tmp/beep_new.sh");
            throw new InvalidOperationException("Generated script failed the syntax check:\n" + check);
        }

        Run(ssh, $"cat /tmp/beep_new.sh > {ScriptPath} && sync && rm -f /tmp/beep_new.sh");
        return "Installed. Backup: " + BackupPath;
    }

    /// <summary>Restores the original script from the backup.</summary>
    public void Restore()
    {
        using var ssh = Connect();
        string result = Run(ssh, $"[ -e {BackupPath} ] && cp -p {BackupPath} {ScriptPath} && echo RESTORED");
        if (!result.Contains("RESTORED")) throw new InvalidOperationException("No backup found on the printer.");
    }

    /// <summary>Plays a mode on the printer buzzer (starts in background on the printer).</summary>
    public void Play(int mode)
    {
        using var ssh = Connect();
        Run(ssh, $"{ScriptPath} {mode}");
    }

    private static void UploadText(SshClient ssh, string remotePath, string content)
    {
        // The printer has no sftp/scp/base64: write the file with a quoted here-document.
        const string marker = "PRINTER_MELODY_EOF";
        if (content.Contains(marker)) throw new InvalidOperationException("Unexpected marker in script.");
        string text = content.Replace("\r\n", "\n").TrimEnd('\n');
        Run(ssh, $"cat > {remotePath} <<'{marker}'\n{text}\n{marker}\n");
    }
}