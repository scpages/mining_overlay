using System.Text.Json;
using System.Text.Json.Serialization;
using MiningOverlay.Core.Calibration;

namespace MiningOverlay.Core.Config;

/// <summary>Replaces config.ini/config.example.ini — JSON via the built-in serializer, no INI parser dependency.</summary>
public sealed class OverlaySettings
{
    public int GameMonitor { get; set; } = 1;
    public int OverlayMonitor { get; set; } = 2;
    public string Ship { get; set; } = "golem";
    public Region? ManualRegion { get; set; } = null;
    public int HistorySize { get; set; } = 5;
    public int MaxRs { get; set; } = 100_000;
    public bool Debug { get; set; } = false;
    public string Mode { get; set; } = "ship"; // ship | fps | ground
    public bool OverlayEnabled { get; set; } = false;
}

public static class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MiningOverlay", "config.json");

    public static OverlaySettings Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path))
            return new OverlaySettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<OverlaySettings>(json, JsonOptions) ?? new OverlaySettings();
    }

    public static void Save(OverlaySettings settings, string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
