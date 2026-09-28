namespace MiningOverlay.Core.Resources;

public sealed record ResourceEntry(int Rs, string Name, string Type, string Tier, string Notes);

public sealed record RsMatch(
    ResourceEntry Resource,
    int Nodes,
    int Confidence,
    double Remainder,
    double RemPct,
    double ExpectedRs,
    string Label);

/// <summary>
/// Port of resources.py — the known Resource Signature table and the node/confidence
/// matching math. Keep this in lockstep with the Python version's RESOURCES list.
/// </summary>
public static class ResourceTable
{
    public static readonly IReadOnlyList<ResourceEntry> All = new List<ResourceEntry>
    {
        new(2000, "Debris",       "Debris",         "C", "Not minable."),
        new(3170, "Quantainium",  "Volatile Gem",   "S", "Highly volatile. Sell FAST after mining."),
        new(3185, "Stileron",     "Gem",            "A", "Rare gem deposit."),
        new(3200, "Savrilium",    "Gem",            "A", "High-value gem."),
        new(3370, "Ouratite",     "Gem",            "A", "Sought for crafting."),
        new(3385, "Riccite",      "Mineral",        "B", "Mid-tier mineral."),
        new(3400, "Lindinium",    "Mineral",        "B", "Industrial mineral."),
        new(3540, "Beryl",        "Gem",            "A", "Gemstone deposit."),
        new(3555, "Taranite",     "Mineral",        "A", "High-value mineral."),
        new(3570, "Borase",       "Mineral",        "B", "Common mid-tier."),
        new(3585, "Gold",         "Precious Metal", "A", "Precious metal. Strong market."),
        new(3600, "Bexalite",     "Mineral",        "S", "Top-tier. Highly sought."),
        new(3825, "Laranite",     "Mineral",        "A", "Dense, valuable mineral."),
        new(3840, "Aslarite",     "Mineral",        "B", "Mid-range deposit."),
        new(3855, "Titanium",     "Metal",          "B", "Industrial metal."),
        new(3870, "Tungsten",     "Metal",          "B", "Heavy industrial."),
        new(3885, "Agricium",     "Mineral",        "A", "Crafting resource."),
        new(3900, "Torite",       "Mineral",        "B", "Common industrial."),
        new(4180, "Hephestanite", "Mineral",        "B", "Moderate value."),
        new(4195, "Tin",          "Metal",          "C", "Common base metal."),
        new(4210, "Quartz",       "Mineral",        "C", "Very common."),
        new(4225, "Corundum",     "Mineral",        "C", "Abrasive mineral."),
        new(4240, "Copper",       "Metal",          "C", "Common metal."),
        new(4255, "Silicon",      "Mineral",        "C", "Electronics base."),
        new(4270, "Iron",         "Metal",          "C", "Most common metal."),
        new(4285, "Aluminium",    "Metal",          "C", "Lightweight metal."),
        new(4300, "Ice",          "Volatile",       "C", "FPS/surface deposits."),
    };

    // The HUD RS reading is (base_rs * multiplier) per node.
    public static readonly IReadOnlyDictionary<string, int> ModeMultipliers = new Dictionary<string, int>
    {
        ["ship"]   = 1,
        ["fps"]    = 3000,
        ["ground"] = 4000,
    };

    /// <summary>
    /// For each resource, calculate node count + confidence matching the web app formula.
    /// remPct = remainder / base_rs (normalised per-node, not per total).
    /// Sorted: remPct asc, then nodes asc (smaller clusters win ties).
    /// </summary>
    public static IReadOnlyList<RsMatch> AnalyzeRs(int totalRs, string mode = "ship")
    {
        int mult = ModeMultipliers.TryGetValue(mode, out var m) ? m : 1;

        var results = new List<RsMatch>(All.Count);
        foreach (var r in All)
        {
            double baseRs    = r.Rs * mult;
            int    nodes     = Math.Max(1, (int)Math.Round(totalRs / baseRs, MidpointRounding.ToEven));
            double expected  = nodes * baseRs;
            double remainder = Math.Abs(totalRs - expected);
            double remPct    = remainder / baseRs;
            int    confidence = Math.Max(0, (int)Math.Round((1 - Math.Min(remPct / 0.15, 1)) * 100, MidpointRounding.ToEven));

            string label = remainder == 0 ? "EXACT"
                : remPct < 0.05 ? "CLOSE"
                : remPct < 0.15 ? "APPROX"
                : "ROUGH";

            results.Add(new RsMatch(r, nodes, confidence, remainder, remPct, expected, label));
        }

        results.Sort((a, b) =>
        {
            int cmp = a.RemPct.CompareTo(b.RemPct);
            return cmp != 0 ? cmp : a.Nodes.CompareTo(b.Nodes);
        });

        int exactCount = results.Count(r => r.Label == "EXACT");
        return results.Take(Math.Max(exactCount, 5)).ToList();
    }
}
