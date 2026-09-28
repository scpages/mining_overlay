namespace MiningOverlay.Core.Calibration;

/// <summary>
/// Port of profiles.py — known-good HUD capture regions, keyed by (ship, resolution).
///
/// Only entries that have been verified against real gameplay (or, for this rewrite,
/// against real captured screenshots run through MiningOverlay.Cli) belong here.
///
/// To calibrate a new ship or resolution:
///   miningoverlay-cli calibrate --batch &lt;screenshots-dir&gt; --ship &lt;name&gt; --x .. --y .. --w .. --h ..
/// then copy the printed profile line in here.
/// </summary>
public static class ShipProfiles
{
    public static readonly (string Ship, string Resolution) DefaultProfileKey = ("golem", "2560x1440");

    public static readonly IReadOnlyDictionary<(string Ship, string Resolution), Region> All =
        new Dictionary<(string, string), Region>
        {
            [("golem", "2560x1440")]      = new Region(0.459, 0.367, 0.086, 0.042),
            [("prospector", "2560x1440")] = new Region(0.465, 0.375, 0.05, 0.03),
        };
}
