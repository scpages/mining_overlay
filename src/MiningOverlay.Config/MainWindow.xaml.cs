using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MiningOverlay.Core.Calibration;
using MiningOverlay.Core.Config;
using MiningOverlay.Core.Ocr;
using MiningOverlay.Windows;
using Region = MiningOverlay.Core.Calibration.Region;

namespace MiningOverlay.Config;

public partial class MainWindow : Window
{
    private static readonly string[] Modes = { "ship", "fps", "ground" };

    private readonly OverlaySettings _settings;
    private Process? _overlayProcess;

    public MainWindow()
    {
        InitializeComponent();
        _settings = ConfigStore.Load();
        PopulateMonitorBoxes();
        PopulateShipBox();
        LoadSettingsIntoUi();
    }

    private void PopulateMonitorBoxes()
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var b = screens[i].Bounds;
            var label = $"{i + 1}: {b.Width}x{b.Height}{(screens[i].Primary ? " (primary)" : "")}";
            GameMonitorBox.Items.Add(label);
            OverlayMonitorBox.Items.Add(label);
        }
        if (screens.Length == 0)
        {
            GameMonitorBox.Items.Add("1: (no monitors detected)");
            OverlayMonitorBox.Items.Add("1: (no monitors detected)");
        }
    }

    private void PopulateShipBox()
    {
        var ships = ShipProfiles.All.Keys.Select(k => k.Ship).Distinct().OrderBy(s => s).ToList();
        if (!ships.Contains(_settings.Ship, StringComparer.OrdinalIgnoreCase))
            ships.Add(_settings.Ship);
        foreach (var ship in ships)
            ShipBox.Items.Add(ship);
    }

    private void LoadSettingsIntoUi()
    {
        GameMonitorBox.SelectedIndex = Math.Clamp(_settings.GameMonitor - 1, 0, Math.Max(0, GameMonitorBox.Items.Count - 1));
        OverlayMonitorBox.SelectedIndex = Math.Clamp(_settings.OverlayMonitor - 1, 0, Math.Max(0, OverlayMonitorBox.Items.Count - 1));
        ShipBox.Text = _settings.Ship;
        ModeBox.SelectedIndex = Math.Max(0, Array.IndexOf(Modes, _settings.Mode));

        if (_settings.ManualRegion is { } r)
        {
            ManualRegionCheck.IsChecked = true;
            RegionXBox.Text = r.X.ToString("0.###");
            RegionYBox.Text = r.Y.ToString("0.###");
            RegionWBox.Text = r.W.ToString("0.###");
            RegionHBox.Text = r.H.ToString("0.###");
        }

        HistorySizeBox.Text = _settings.HistorySize.ToString();
        MaxRsBox.Text = _settings.MaxRs.ToString();
        DebugCheck.IsChecked = _settings.Debug;
    }

    private void ManualRegionCheck_Changed(object sender, RoutedEventArgs e) =>
        ManualRegionGrid.IsEnabled = ManualRegionCheck.IsChecked == true;

    /// <summary>Builds settings from the current UI state — shared by Test and Save.</summary>
    private OverlaySettings ReadSettingsFromUi()
    {
        Region? manual = null;
        if (ManualRegionCheck.IsChecked == true
            && double.TryParse(RegionXBox.Text, out var x)
            && double.TryParse(RegionYBox.Text, out var y)
            && double.TryParse(RegionWBox.Text, out var w)
            && double.TryParse(RegionHBox.Text, out var h))
        {
            manual = new Region(x, y, w, h);
        }

        return new OverlaySettings
        {
            GameMonitor = GameMonitorBox.SelectedIndex + 1,
            OverlayMonitor = OverlayMonitorBox.SelectedIndex + 1,
            Ship = string.IsNullOrWhiteSpace(ShipBox.Text) ? "golem" : ShipBox.Text.Trim(),
            ManualRegion = manual,
            Mode = Modes[Math.Max(0, ModeBox.SelectedIndex)],
            HistorySize = int.TryParse(HistorySizeBox.Text, out var hs) ? hs : _settings.HistorySize,
            MaxRs = int.TryParse(MaxRsBox.Text, out var mr) ? mr : _settings.MaxRs,
            Debug = DebugCheck.IsChecked == true,
            OverlayEnabled = _settings.OverlayEnabled,
        };
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = ReadSettingsFromUi();
        ConfigStore.Save(settings);
        TestResultText.Text = $"Saved to {ConfigStore.DefaultPath}";
    }

    private void TestCalibration_Click(object sender, RoutedEventArgs e)
    {
        var settings = ReadSettingsFromUi();
        try
        {
            var monitor = ScreenCapture.GetMonitor(settings.GameMonitor);
            var resolution = RegionResolver.Resolve(settings.ManualRegion, settings.Ship, monitor.Bounds.Width, monitor.Bounds.Height);

            var r = resolution.Region;
            var region = new System.Drawing.Rectangle(
                monitor.Bounds.X + (int)(monitor.Bounds.Width * r.X),
                monitor.Bounds.Y + (int)(monitor.Bounds.Height * r.Y),
                (int)(monitor.Bounds.Width * r.W),
                (int)(monitor.Bounds.Height * r.H));

            using var capture = ScreenCapture.CaptureRegion(region);
            var result = RsOcrReader.Read(capture, settings.Mode, settings.MaxRs);

            var lines = new List<string> { $"Region: {resolution.Description}", $"Digits: '{result.Digits}'" };
            if (result.Rs is int rs)
            {
                lines.Add($"RS: {rs:N0}");
                foreach (var m in result.Matches.Take(3))
                    lines.Add($"  [{m.Resource.Tier}] {m.Resource.Name} {m.Nodes}x  {m.Label}  {m.Confidence}%");
            }
            else
            {
                lines.Add("No confident match.");
            }
            TestResultText.Text = string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            TestResultText.Text = $"Test failed: {ex.Message}";
        }
    }

    private void ToggleOverlay_Click(object sender, RoutedEventArgs e)
    {
        if (_overlayProcess is { HasExited: false })
        {
            _overlayProcess.CloseMainWindow();
            if (!_overlayProcess.WaitForExit(2000))
                _overlayProcess.Kill();
            _overlayProcess = null;
            ToggleOverlayButton.Content = "Start overlay";
            OverlayStatusText.Text = "Stopped";
            return;
        }

        var exePath = FindOverlayExe();
        if (exePath is null)
        {
            TestResultText.Text =
                "Could not find MiningOverlay.Overlay.exe. Build the whole solution " +
                "(Build > Build Solution), or publish both apps into the same folder — see README.md.";
            return;
        }

        // Make sure the overlay picks up whatever is currently configured, even if unsaved.
        ConfigStore.Save(ReadSettingsFromUi());

        _overlayProcess = Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = false });
        ToggleOverlayButton.Content = "Stop overlay";
        OverlayStatusText.Text = "Running";
    }

    /// <summary>
    /// Looks next to Config's own exe first (the layout after `dotnet publish` into a
    /// shared folder), then falls back to Overlay's own per-project build output (the
    /// layout while debugging each project separately in Visual Studio).
    /// </summary>
    private static string? FindOverlayExe()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "MiningOverlay.Overlay.exe"),
            Path.Combine(
                AppContext.BaseDirectory.Replace("MiningOverlay.Config", "MiningOverlay.Overlay"),
                "MiningOverlay.Overlay.exe"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
