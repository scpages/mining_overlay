"""
Calibrate or verify the RS capture region for your ship + resolution.

Run this while Star Citizen is open with the RS number visible. It captures
the exact same region main.py would use, runs it through main.py's real OCR
pipeline, and tells you whether the reading is confident. Adjust --x/--y/--w/--h
until it reads cleanly, then copy the printed profile line into profiles.py.

The RS number can drift a little within its HUD bracket during movement, so
size the box generously enough to keep it inside the region even as it moves
— don't crop it pixel-tight around one snapshot.

Usage:
    python debug_capture.py                              # use config.ini's current region
    python debug_capture.py --ship prospector             # start from a known profile
    python debug_capture.py --x 0.47 --y 0.35 --w 0.07 --h 0.05 --loop
                                                            # test a custom region, repeatedly
"""

import argparse
import configparser
import sys
import time

import mss
from PIL import Image

sys.path.insert(0, ".")
from resources import analyze_rs
from region import resolve_region
from main import _ocr, _best_rs


def _grab(monitor_idx: int, region_frac: dict):
    with mss.MSS() as sct:
        monitors = sct.monitors
        m = monitors[monitor_idx if monitor_idx < len(monitors) else 1]
        region = {
            "left":   int(m["width"]  * region_frac["x"]) + m["left"],
            "top":    int(m["height"] * region_frac["y"]) + m["top"],
            "width":  int(m["width"]  * region_frac["w"]),
            "height": int(m["height"] * region_frac["h"]),
        }
        shot = sct.grab(region)
        img  = Image.frombytes("RGB", shot.size, shot.bgra, "raw", "BGRX")
        resolution = f'{m["width"]}x{m["height"]}'
    return img, resolution


def capture_once(monitor_idx: int, region_frac: dict, ship: str, save_raw: bool):
    img, resolution = _grab(monitor_idx, region_frac)

    if save_raw:
        img.save("step0_raw.png")

    digits = _ocr(img)
    val    = _best_rs(digits)

    print(f"\nRegion: {region_frac}   Resolution: {resolution}")
    print(f"Digits read: '{digits}'")

    if val is not None:
        results = analyze_rs(val)
        print(f"Best RS: {val:,}")
        for r in results[:3]:
            print(f"  [{r['tier']}] {r['name']:15} {r['nodes']}x  {r['label']:6}  {r['confidence']}%")
        print("\nProfile line for profiles.py:")
        print(
            f'    ("{ship}", "{resolution}"): '
            f'{{"x": {region_frac["x"]}, "y": {region_frac["y"]}, '
            f'"w": {region_frac["w"]}, "h": {region_frac["h"]}}},'
        )
    else:
        print("No confident match — widen/move the region with --x/--y/--w/--h and try again.")

    return val is not None


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--ship", default=None, help="Ship to calibrate/verify (default: config.ini's [region] ship)")
    parser.add_argument("--monitor", type=int, default=None, help="Game monitor index (default: config.ini's game_monitor)")
    parser.add_argument("--x", type=float, default=None)
    parser.add_argument("--y", type=float, default=None)
    parser.add_argument("--w", type=float, default=None)
    parser.add_argument("--h", type=float, default=None)
    parser.add_argument("--loop", action="store_true", help="Re-capture every 3s instead of once, so you can test while moving")
    args = parser.parse_args()

    cfg = configparser.ConfigParser()
    cfg.read("config.ini")

    monitor_idx = args.monitor if args.monitor is not None else cfg.getint("monitors", "game_monitor", fallback=1)
    ship = args.ship or cfg.get("region", "ship", fallback="golem")

    manual = (args.x, args.y, args.w, args.h)
    if all(v is not None for v in manual):
        region_frac = {"x": args.x, "y": args.y, "w": args.w, "h": args.h}
        print(f"Using region from --x/--y/--w/--h for ship '{ship}'.")
    else:
        region_frac, desc, ok = resolve_region(cfg, _game_monitor(monitor_idx), ship=ship)
        print(f"Using region: {desc}")

    if not args.loop:
        print("\nSwitch to Star Citizen now — capturing in 5 seconds...")
        for i in range(5, 0, -1):
            print(f"  {i}...")
            time.sleep(1)
        capture_once(monitor_idx, region_frac, ship, save_raw=True)
    else:
        print("\nLoop mode — capturing every 3s. Move/mine normally to test drift. Ctrl+C to stop.")
        try:
            while True:
                capture_once(monitor_idx, region_frac, ship, save_raw=False)
                time.sleep(3)
        except KeyboardInterrupt:
            pass


def _game_monitor(monitor_idx: int) -> dict:
    with mss.MSS() as sct:
        monitors = sct.monitors
        return monitors[monitor_idx if monitor_idx < len(monitors) else 1]


if __name__ == "__main__":
    main()
