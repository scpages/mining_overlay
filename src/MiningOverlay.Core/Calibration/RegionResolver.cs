namespace MiningOverlay.Core.Calibration;

public sealed record RegionResolution(Region Region, string Description, bool Ok);

/// <summary>
/// Port of region.py's resolve_region — shared by the CLI, Overlay, and Config app so
/// they can never drift out of sync on how a capture region is picked.
///
/// Resolution order:
///   1. manualOverride, if provided — always wins.
///   2. A known profile for (ship, detected resolution).
///   3. ShipProfiles.DefaultProfileKey, with Ok=false so the caller can warn.
/// </summary>
public static class RegionResolver
{
    public static RegionResolution Resolve(Region? manualOverride, string ship, int monitorWidth, int monitorHeight)
    {
        if (manualOverride is { } manual)
            return new RegionResolution(manual, "manual override", true);

        ship = ship.Trim().ToLowerInvariant();
        var resolution = $"{monitorWidth}x{monitorHeight}";
        var key = (ship, resolution);

        if (ShipProfiles.All.TryGetValue(key, out var region))
            return new RegionResolution(region, $"{ship} @ {resolution}", true);

        var (fallbackShip, fallbackRes) = ShipProfiles.DefaultProfileKey;
        var warning = $"no calibrated profile for {ship} @ {resolution} — " +
                      $"falling back to {fallbackShip} @ {fallbackRes}. " +
                      $"Run: miningoverlay-cli calibrate --ship {ship} to calibrate one.";
        return new RegionResolution(ShipProfiles.All[ShipProfiles.DefaultProfileKey], warning, false);
    }
}
