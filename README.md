# Creality SPARKX Melody

Compose a custom start/end print melody for the **Creality SPARKX i7** 3D printer and install it over SSH, replacing the stock buzzer tunes. Includes a local audio preview.

> Unofficial community tool. Not affiliated with or endorsed by Creality.

<img width="626" height="553" alt="Screenshot 2026-11-13 125507" src="https://github.com/user-attachments/assets/9dbdcaf1-de0b-42e1-bdb9-3d8ba60a4923" />

## How it works

The printer does not play audio files for these sounds. `/usr/bin/beep_sound.sh` drives a PWM
buzzer and contains three tunes (`mode1_sequence` start, `mode2_sequence` end, `mode3_sequence`
alert). Creality SPARKX Melody:

1. builds a list of notes (note, octave, length in beats, BPM);
2. converts them to PWM `period` / `duty_cycle` values (period = 1e9 / frequency, 50% duty);
3. backs up the stock script once to `/mnt/UDISK/audio_backup/beep_sound.sh.orig`;
4. patches the selected mode functions, checks the result with `sh -n`, and installs it.

A local preview renders the melody as a square wave (NAudio).

## Requirements

- Windows, .NET 8 SDK
- Printer with root access enabled (SPARKX i7: *Settings > Root access*)

## Build and test

```
dotnet build -c Release
dotnet test -c Release
```

## Usage

1. Add notes with the list controls (note or rest, octave, beats) and set the BPM.
2. Use **Preview** to listen locally.
3. Enter the printer IP, user and password, choose the modes to replace, then **Send to printer**.
4. **Test on printer** plays the first selected mode. **Restore original** reverts to the backup.

The app refuses to send, test or restore while a print is running (checked via Moonraker).
The password is never stored.

## Notes and limits

- The buzzer plays one note at a time, so only simple monophonic melodies are possible.
- This modifies a system file on your printer and may affect your warranty. Use at your own risk.
- Only tested on the Creality SPARKX i7; other Creality models may use a different script or PWM channel.

## License

MIT

