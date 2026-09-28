# SC Mining Overlay

A real-time Star Citizen mining assistant. It watches your screen while you mine, reads the Resource Signature (RS) value from your HUD automatically, and identifies what resource it is — including how many nodes are in the cluster. The last 5 unique confirmed readings are kept on screen so you can track what you've scanned.

Inspired by [rainbowramen.github.io/sc-mining-hud](https://rainbowramen.github.io/sc-mining-hud/) — this is a live overlay version that reads your HUD automatically so you never have to type anything.

> **Work in progress** — this tool is still in development and not yet finished. It has only been tested on the **DRAKE Golem** ship. Other ships, resolutions, or HUD layouts may not work correctly. Feedback and bug reports are welcome.

---

## Install — step by step

You only need to do this once.

### 1. Download the overlay

Click the green **Code** button on this page → **Download ZIP** → extract the folder somewhere (e.g. your Desktop).

### 2. Install Python

Go to [python.org/downloads](https://www.python.org/downloads/) and download the latest version.

**Important:** on the first screen of the installer, tick **"Add Python to PATH"** before clicking Install.

### 3. Install Tesseract OCR

Go to [this link](https://github.com/UB-Mannheim/tesseract/wiki) and download the Windows installer.

Run it and keep all the default options — just click Next through everything.

### 4. Run the overlay

Open the extracted folder and double-click **`launch.bat`**.

It will automatically check that everything is installed and install any missing Python packages. Once all checks pass, the console disappears and the overlay appears on your screen.

---

## How to use it

- Point your mining laser at a rock — when the RS value appears in your HUD, the overlay identifies the resource automatically
- Only confirmed matches are shown — if the reading doesn't match any known resource exactly, it is ignored
- The last 5 unique confirmed readings are shown, newest at the top, older ones faded
- If a reading could match more than one resource, all possibilities are listed on the same row
- Click the **SHIP / FPS / GROUND** button to switch mining mode
- Drag the overlay anywhere by clicking and holding it
- Close it with the **✕** button in the top right

---

## Configuration

All settings are in the **`config.ini`** file in the same folder as the overlay. It is created automatically the first time you run `launch.bat` — just open it in Notepad to edit.

| Setting | Default | What it does |
|---|---|---|
| `game_monitor` | `1` | Which monitor the game runs on |
| `overlay_monitor` | `2` | Which monitor to show the overlay on (set to `1` if single monitor) |
| `ship` | `golem` | Selects a known-good capture region for your ship + resolution (see below) |
| `x / y / w / h` | *(unset)* | Optional manual override of the capture region — only needed if there's no profile for your ship yet |
| `history_size` | `5` | How many readings to keep on screen |
| `max_rs` | `100000` | Readings above this value are ignored |
| `debug` | `false` | Set to `true` to save debug images while running |

### Ship profiles

The RS number's position on your HUD is different for every ship, so the overlay looks up a known-good capture region from `profiles.py` based on the `ship` setting and your detected screen resolution.

| Ship | Resolution | Status |
|---|---|---|
| DRAKE Golem | 2560×1440 | Verified |
| Others | any | Not calibrated yet — falls back to the Golem region, which likely won't line up |

If your ship/resolution isn't in the table yet, the overlay will silently fall back to the Golem's region (wrong for other ships) and print a warning if run from a terminal (`python main.py` instead of `launch.bat`).

**To calibrate a new profile:**

1. Get in your ship and start mining so the RS number is visible on your HUD.
2. Run `python debug_capture.py --ship <yourship> --loop` — it re-captures every 3 seconds so you can test while moving, since the RS number can drift a little within its HUD bracket.
3. Pass `--x --y --w --h` (fractions of screen size, 0.0–1.0) to try different regions until the digits read cleanly. Size the box generously enough to keep the number inside it even as it drifts — don't crop pixel-tight.
4. Once it reads confidently, it prints a ready-to-paste profile line — add it to the `PROFILES` dict in `profiles.py` (and consider opening a PR so others with the same ship benefit).

Alternatively, set `x/y/w/h` directly in `config.ini` as a manual override — that always takes priority over any profile.

---

## Mining modes

Switch modes by clicking the button in the top-left of the overlay.

| Mode   | Use for                         |
|--------|---------------------------------|
| SHIP   | Ship mining (Prospector, MOLE)  |
| FPS    | FPS hand tool mining            |
| GROUND | Ground vehicle mining           |

The RS value in the HUD is scaled differently depending on the tool you use — this setting makes sure the calculation is correct.

---

## Tier guide

| Tier | Colour | Examples                        |
|------|--------|---------------------------------|
| S    | Gold   | Quantainium, Bexalite           |
| A    | Green  | Gold, Taranite, Laranite, Beryl |
| B    | Orange | Borase, Titanium, Tungsten      |
| C    | Grey   | Iron, Copper, Quartz, Ice       |

---

## Troubleshooting

**Nothing is detected / wrong resources showing:**
The capture region needs to match where the RS number appears on your screen — this is very likely if you're not on the DRAKE Golem, since only that ship is calibrated so far. Run `python debug_capture.py --ship <yourship> --loop` to see what the overlay is actually reading and tune the region live. See [Ship profiles](#ship-profiles) above.

**I only have one monitor:**
Open `config.ini` and set `overlay_monitor = 1`.

**The overlay appears on the wrong monitor:**
Open `config.ini` and set `game_monitor` and `overlay_monitor` to the correct numbers (1 = primary monitor).

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
