# SC Mining Overlay

A real-time Star Citizen mining assistant. It watches your screen while you mine, reads the Resource Signature (RS) value from your HUD automatically, and identifies what resource it is — including how many nodes are in the cluster. The last few unique confirmed readings are kept on screen so you can track what you've scanned.

Inspired by [rainbowramen.github.io/sc-mining-hud](https://rainbowramen.github.io/sc-mining-hud/) — this is a live overlay version that reads your HUD automatically so you never have to type anything.

> **Work in progress** — calibrated for the DRAKE Golem and MISC Prospector at 2560×1440 so far. Other ships or resolutions need calibrating (see below). Feedback and bug reports are welcome.

---

## Project layout

| Project | What it is |
|---|---|
| `src/MiningOverlay.Core` | Resource table, ship calibration profiles, OCR pipeline — no OS-specific code, runs on Linux and Windows |
| `src/MiningOverlay.Windows` | Screen-capture helper shared by the two Windows apps below |
| `src/MiningOverlay.Cli` | Command-line calibration/debug tool — runs anywhere, including Linux |
| `src/MiningOverlay.Overlay` | The always-on-top HUD (Windows only) |
| `src/MiningOverlay.Config` | Settings GUI: pick monitors/ship, test calibration, start/stop the overlay (Windows only) |

---

## Install

1. Install the **.NET 8 SDK** (or just the runtime, if only running published binaries): https://dotnet.microsoft.com/download/dotnet/8.0
2. Install **Tesseract-OCR**: https://github.com/UB-Mannheim/tesseract/wiki — make sure `tesseract.exe` is on your `PATH`, or leave it in the default install location (`C:\Program Files\Tesseract-OCR\`), which the app also checks automatically.

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

## How to use the overlay

- Point your mining laser at a rock — when the RS value appears in your HUD, the overlay identifies the resource automatically
- Only confirmed matches are shown — if the reading doesn't match any known resource exactly, it is ignored
- Recent unique confirmed readings are shown, newest at the top, older ones faded
- If a reading could match more than one resource, all possibilities are listed on the same row
- Click the **SHIP / FPS / GROUND** button to switch mining mode
- Drag the overlay by its title bar
- Close it with the **✕** button in the top right

## Calibrating a new ship (works on Linux too)

```
dotnet run --project src/MiningOverlay.Cli -- calibrate --image screenshot.png --ship prospector --x 0.465 --y 0.375 --w 0.05 --h 0.03
dotnet run --project src/MiningOverlay.Cli -- calibrate --batch ./screenshots      --ship prospector --x 0.465 --y 0.375 --w 0.05 --h 0.03
```

`--x/--y/--w/--h` are fractions (0.0–1.0) of the screenshot's own dimensions. Adjust
them until the digits read cleanly and confidently across your screenshots — the RS
number can drift a little within its HUD bracket, so size the box generously rather
than pixel-tight. Once it's confident, the CLI prints a ready-to-paste entry for
`src/MiningOverlay.Core/Calibration/ShipProfiles.cs`.

This is the same OCR pipeline the overlay uses, so a screenshot-based calibration
run means exactly what it says — no game or Windows machine required.

## Building

```
dotnet build                          # whole solution
dotnet build src/MiningOverlay.Cli    # just Core + Cli (works on Linux)
```

`Overlay` and `Config` target `net8.0-windows` and need the Windows Desktop workload —
on Linux, `EnableWindowsTargeting` is set so they still build (useful for catching
reference/compile errors early), but they can't actually run outside Windows.

---

## Mining modes

Switch modes by clicking the button in the top-left of the overlay.

| Mode   | Use for                         |
|--------|----------------------------------|
| SHIP   | Ship mining (Prospector, MOLE)  |
| FPS    | FPS hand tool mining             |
| GROUND | Ground vehicle mining            |

The RS value in the HUD is scaled differently depending on the tool you use — this setting makes sure the calculation is correct.

---

## Tier guide

| Tier | Colour | Examples                        |
|------|--------|----------------------------------|
| S    | Gold   | Quantainium, Bexalite            |
| A    | Green  | Gold, Taranite, Laranite, Beryl  |
| B    | Orange | Borase, Titanium, Tungsten       |
| C    | Grey   | Iron, Copper, Quartz, Ice        |

---

## Troubleshooting

**Nothing is detected / wrong resources showing:**
The capture region needs to match where the RS number appears on your screen — likely if your ship/resolution isn't calibrated yet (see the table in `src/MiningOverlay.Core/Calibration/ShipProfiles.cs`). Use `MiningOverlay.Cli calibrate` to find a working region.

**I only have one monitor:**
In `Config`, set the overlay monitor to `1`.

**The overlay appears on the wrong monitor:**
In `Config`, set the game/overlay monitor to the correct ones (1 = primary monitor).

---

## Resource table

| RS   | Resource      | Type           | Tier |
|------|---------------|----------------|------|
| 2000 | Debris        | Debris         | C    |
| 3170 | Quantainium   | Volatile Gem   | S    |
| 3185 | Stileron      | Gem            | A    |
| 3200 | Savrilium     | Gem            | A    |
| 3370 | Ouratite      | Gem            | A    |
| 3385 | Riccite       | Mineral        | B    |
| 3400 | Lindinium     | Mineral        | B    |
| 3540 | Beryl         | Gem            | A    |
| 3555 | Taranite      | Mineral        | A    |
| 3570 | Borase        | Mineral        | B    |
| 3585 | Gold          | Precious Metal | A    |
| 3600 | Bexalite      | Mineral        | S    |
| 3825 | Laranite      | Mineral        | A    |
| 3840 | Aslarite      | Mineral        | B    |
| 3855 | Titanium      | Metal          | B    |
| 3870 | Tungsten      | Metal          | B    |
| 3885 | Agricium      | Mineral        | A    |
| 3900 | Torite        | Mineral        | B    |
| 4180 | Hephestanite  | Mineral        | B    |
| 4195 | Tin           | Metal          | C    |
| 4210 | Quartz        | Mineral        | C    |
| 4225 | Corundum      | Mineral        | C    |
| 4240 | Copper        | Metal          | C    |
| 4255 | Silicon       | Mineral        | C    |
| 4270 | Iron          | Metal          | C    |
| 4285 | Aluminium     | Metal          | C    |
| 4300 | Ice           | Volatile       | C    |

RS values from SC 4.0+ community data.
