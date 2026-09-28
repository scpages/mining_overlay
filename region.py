"""
Resolves the on-screen capture region (screen-fraction x/y/w/h), shared by
main.py and debug_capture.py so the two never drift out of sync.

Resolution order:
  1. Manual x/y/w/h in config.ini [region] — wins if all four are set.
  2. A known profile for the configured ship + detected screen resolution.
  3. DEFAULT_PROFILE_KEY, with ok=False so the caller can warn the user.
"""

from profiles import PROFILES, DEFAULT_PROFILE_KEY


def resolve_region(cfg, monitor, ship=None):
    """
    cfg: a configparser.ConfigParser with the [region] section loaded.
    monitor: an mss monitor dict (needs "width" / "height").
    ship: optional override for the configured ship (used by debug_capture.py).

    Returns (region_frac, description, ok).
    """
    if all(cfg.has_option("region", k) for k in ("x", "y", "w", "h")):
        region = {k: cfg.getfloat("region", k) for k in ("x", "y", "w", "h")}
        return region, "manual override", True

    ship = (ship or cfg.get("region", "ship", fallback="golem")).strip().lower()
    resolution = f'{monitor["width"]}x{monitor["height"]}'
    key = (ship, resolution)

    if key in PROFILES:
        return PROFILES[key], f"{ship} @ {resolution}", True

    fallback_ship, fallback_res = DEFAULT_PROFILE_KEY
    warning = (
        f"no calibrated profile for {ship} @ {resolution} — "
        f"falling back to {fallback_ship} @ {fallback_res}. "
        f"Run debug_capture.py --ship {ship} to calibrate one."
    )
    return PROFILES[DEFAULT_PROFILE_KEY], warning, False
