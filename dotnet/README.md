# SC Mining Overlay (.NET)

A rewrite of the Python overlay in C#/.NET — same idea (reads the Resource Signature
value from your HUD and identifies the resource), split into four pieces:

| Project | What it is |
|---|---|
| `MiningOverlay.Core` | Resource table, ship calibration profiles, OCR pipeline — no OS-specific code, runs on Linux and Windows |
| `MiningOverlay.Windows` | Screen-capture helper shared by the two Windows apps below |
| `MiningOverlay.Cli` | Command-line calibration/debug tool — runs anywhere, including Linux |
| `MiningOverlay.Overlay` | The always-on-top HUD (Windows only) |
| `MiningOverlay.Config` | Settings GUI: pick monitors/ship, test calibration, start/stop the overlay (Windows only) |

> **Status:** `Core` and `Cli` are built and verified against real screenshots.
> `Overlay` and `Config` build cleanly but haven't been run on an actual Windows
> machine yet — please test them and report back anything that looks off.

---

## Install

1. Install the **.NET 8 SDK** (or just the runtime, if only running published binaries): https://dotnet.microsoft.com/download/dotnet/8.0
2. Install **Tesseract-OCR**: https://github.com/UB-Mannheim/tesseract/wiki — same requirement as the Python version. Make sure `tesseract.exe` is on your `PATH`, or leave it in the default install location (`C:\Program Files\Tesseract-OCR\`), which the apps also check automatically.

## Running the apps

Both GUI apps need to be **in the same folder** (`Config`'s "Start overlay" button looks
for `MiningOverlay.Overlay.exe` next to itself):

```
dotnet publish src/MiningOverlay.Overlay -c Release -r win-x64 --self-contained false -o publish
dotnet publish src/MiningOverlay.Config  -c Release -r win-x64 --self-contained false -o publish
```

Then run `publish/MiningOverlay.Config.exe` — set your monitors and ship there, hit
**Test calibration** to confirm the region reads correctly without needing to relaunch,
then **Start overlay**. Settings are saved to `%APPDATA%\MiningOverlay\config.json`.

## Calibrating a new ship (works on Linux too)

This is the replacement for the Python version's `debug_capture.py`, and it's the same
tool whether you run it on Windows or — like it was actually used during development —
on a Linux box with a folder of screenshots and no game installed at all:

```
dotnet run --project src/MiningOverlay.Cli -- calibrate --image screenshot.png --ship prospector --x 0.465 --y 0.375 --w 0.05 --h 0.03
dotnet run --project src/MiningOverlay.Cli -- calibrate --batch ./screenshots      --ship prospector --x 0.465 --y 0.375 --w 0.05 --h 0.03
```

`--x/--y/--w/--h` are fractions (0.0–1.0) of the screenshot's own dimensions. Adjust
them until the digits read cleanly and confidently across your screenshots — the RS
number can drift a little within its HUD bracket, so size the box generously rather
than pixel-tight. Once it's confident, the CLI prints a ready-to-paste entry for
`src/MiningOverlay.Core/Calibration/ShipProfiles.cs`.

## Building

```
dotnet build                              # whole solution
dotnet build src/MiningOverlay.Cli        # just Core + Cli (works on Linux)
```

`Overlay` and `Config` target `net8.0-windows` and need the Windows Desktop workload —
on Linux, `EnableWindowsTargeting` is set so they still build (useful for catching
reference/compile errors early), but they can't actually run outside Windows.
