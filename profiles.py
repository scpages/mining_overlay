"""
Known-good HUD capture regions, keyed by (ship, resolution).

Values are fractions of the screen (0.0-1.0), matching mss monitor geometry:
    x, y = top-left corner of the RS text box
    w, h = width / height of the box

Only entries that have been verified against real gameplay belong here.
To calibrate a new ship or resolution, run:

    python debug_capture.py --ship prospector --loop

while in-game with the RS value visible, tune --x/--y/--w/--h until the
printed digits are clean, then copy the profile line it prints into the
PROFILES dict below (and ideally contribute it back as a PR).
"""

PROFILES = {
    ("golem", "2560x1440"): {"x": 0.459, "y": 0.367, "w": 0.086, "h": 0.042},
}

# Used whenever no profile matches the configured ship + detected resolution.
DEFAULT_PROFILE_KEY = ("golem", "2560x1440")
