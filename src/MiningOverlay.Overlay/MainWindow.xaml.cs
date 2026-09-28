using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MiningOverlay.Core.Config;
using MiningOverlay.Core.Resources;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;

namespace MiningOverlay.Overlay;

/// <summary>Port of main.py's Overlay class — draggable, always-on-top HUD showing recent RS readings.</summary>
public partial class MainWindow : Window
{
    private static readonly string[] Modes = { "ship", "fps", "ground" };

    private static readonly Color TextGray   = Color.FromRgb(0xaa, 0xaa, 0xaa);
    private static readonly Color TextDim    = Color.FromRgb(0x44, 0x44, 0x44);
    private static readonly Color TextMedium = Color.FromRgb(0x44, 0x44, 0x44);
    private static readonly Color TypeColor  = Color.FromRgb(0x66, 0x77, 0x88);
    private static readonly Color MetaColor  = Color.FromRgb(0x88, 0x99, 0xaa);
    private static readonly Color LineColor  = Color.FromRgb(0x2a, 0x2a, 0x4a);

    private static readonly Dictionary<string, Color> TierColor = new()
    {
        ["S"] = Color.FromRgb(0xFF, 0xD7, 0x00),
        ["A"] = Color.FromRgb(0x00, 0xCC, 0x55),
        ["B"] = Color.FromRgb(0xFF, 0xAA, 0x00),
        ["C"] = Color.FromRgb(0x88, 0x88, 0x88),
    };

    private readonly OverlaySettings _settings;
    private readonly HistoryModel _history;
    private readonly List<RowWidgets> _rows = new();
    private int _modeIndex;
    private CaptureLoop? _capture;
    private string? _statusMessage;

    public MainWindow()
    {
        InitializeComponent();

        _settings = ConfigStore.Load();
        _history = new HistoryModel(_settings.HistorySize);
        _modeIndex = Math.Max(0, Array.IndexOf(Modes, _settings.Mode));
        ModeButton.Text = Modes[_modeIndex].ToUpperInvariant();

        BuildRows(_settings.HistorySize);
        PlaceWindow();
        Redraw();

        Loaded += (_, _) => StartCapture();
        Closing += (_, _) => _capture?.Stop();
    }

    private void StartCapture()
    {
        _capture = new CaptureLoop(_settings, () => Modes[_modeIndex], OnReading, OnWarning);
        _capture.Start();
    }

    private void OnReading(int rs) =>
        Dispatcher.Invoke(() =>
        {
            if (_history.TryAdd(rs))
                Redraw();
        });

    private void OnWarning(string message) =>
        Dispatcher.Invoke(() =>
        {
            OverlayLog.Write(message); // full detail, including the calibrate-it hint
            _statusMessage = message.Contains("no calibrated profile", StringComparison.OrdinalIgnoreCase)
                ? "Ship not calibrated — see overlay.log"
                : message;
            Redraw();
        });

    private void PlaceWindow()
    {
        var m = MiningOverlay.Windows.ScreenCapture.GetMonitor(_settings.OverlayMonitor);
        Left = m.Bounds.Left + (m.Bounds.Width - Width) / 2;
        Top = m.Bounds.Top + 20;
    }

    // ── title bar ─────────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void ModeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _modeIndex = (_modeIndex + 1) % Modes.Length;
        ModeButton.Text = Modes[_modeIndex].ToUpperInvariant();
        Redraw();
    }

    private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        Close();
    }

    // ── rows ──────────────────────────────────────────────────────────────────

    private sealed class RowWidgets
    {
        public required TextBlock Rs { get; init; }
        public required TextBlock Nodes { get; init; }
        public required TextBlock Name { get; init; }
        public required TextBlock Tier { get; init; }
        public required TextBlock Conf { get; init; }
        public required TextBlock Meta { get; init; }
    }

    private void BuildRows(int count)
    {
        RowsPanel.Children.Clear();
        _rows.Clear();

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
                RowsPanel.Children.Add(new Border { Background = new SolidColorBrush(LineColor), Height = 1, Margin = new Thickness(0, 4, 0, 4) });

            var rs = new TextBlock { FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, FontSize = 13, Width = 66, TextAlignment = TextAlignment.Right };
            var nodes = new TextBlock { FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, FontSize = 13, Width = 26, TextAlignment = TextAlignment.Right };
            var name = new TextBlock
            {
                Text = i == 0 ? "Waiting…" : "",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = new SolidColorBrush(i == 0 ? TextMedium : TextDim),
                Margin = new Thickness(4, 0, 0, 0),
            };
            var tier = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 11, Margin = new Thickness(6, 0, 0, 0) };
            var conf = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 11 };

            var leftStack = new StackPanel { Orientation = Orientation.Horizontal };
            leftStack.Children.Add(rs);
            leftStack.Children.Add(nodes);
            leftStack.Children.Add(name);
            leftStack.Children.Add(tier);

            var top = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(conf, Dock.Right);
            top.Children.Add(conf);
            top.Children.Add(leftStack);

            var meta = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 10, Margin = new Thickness(2, 2, 0, 0) };

            var rowPanel = new StackPanel { Margin = new Thickness(0, 3, 0, 3) };
            rowPanel.Children.Add(top);
            rowPanel.Children.Add(meta);
            RowsPanel.Children.Add(rowPanel);

            _rows.Add(new RowWidgets { Rs = rs, Nodes = nodes, Name = name, Tier = tier, Conf = conf, Meta = meta });
        }
    }

    private void Redraw()
    {
        var mode = Modes[_modeIndex];
        var history = _history.Values;

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i < history.Count)
            {
                int rs = history[i];
                var matches = ResourceTable.AnalyzeRs(rs, mode);
                var exact = matches.Where(m => m.Label == "EXACT").ToList();
                double alpha = Math.Max(0.4, 1.0 - i * 0.15);
                FillRow(_rows[i], rs, exact, alpha);
            }
            else
            {
                ClearRow(_rows[i], i == 0 ? (_statusMessage ?? "Waiting…") : "");
            }
        }
    }

    private static void FillRow(RowWidgets row, int rsVal, IReadOnlyList<RsMatch> exact, double alpha)
    {
        row.Rs.Text = rsVal.ToString("N0");
        row.Rs.Foreground = new SolidColorBrush(Dim(TextGray, alpha));

        if (exact.Count > 0)
        {
            var m = exact[0];
            var nameColor = Dim(TierColor.GetValueOrDefault(m.Resource.Tier, Colors.White), alpha);
            var tierColor = Dim(nameColor, 0.65);

            row.Nodes.Text = $"{m.Nodes}×";
            row.Nodes.Foreground = new SolidColorBrush(nameColor);
            row.Name.Text = m.Resource.Name;
            row.Name.Foreground = new SolidColorBrush(nameColor);
            row.Tier.Text = $"[{m.Resource.Tier}]";
            row.Tier.Foreground = new SolidColorBrush(tierColor);
            row.Conf.Text = m.Resource.Type;
            row.Conf.Foreground = new SolidColorBrush(Dim(TypeColor, alpha));

            if (exact.Count > 1)
            {
                var parts = exact.Skip(1).Select(r => $"{r.Nodes}× {r.Resource.Name} [{r.Resource.Tier}]");
                row.Meta.Text = "  or: " + string.Join("  |  ", parts);
                row.Meta.Foreground = new SolidColorBrush(Dim(MetaColor, alpha));
            }
            else
            {
                row.Meta.Text = "";
            }
        }
        else
        {
            var dimFg = Dim(TextDim, alpha);
            row.Nodes.Text = "";
            row.Name.Text = "—";
            row.Name.Foreground = new SolidColorBrush(dimFg);
            row.Tier.Text = "";
            row.Conf.Text = "";
            row.Meta.Text = "";
        }
    }

    private static void ClearRow(RowWidgets row, string placeholder)
    {
        row.Rs.Text = "";
        row.Nodes.Text = "";
        row.Name.Text = placeholder;
        row.Name.Foreground = new SolidColorBrush(placeholder != "" ? TextMedium : Color.FromRgb(0x33, 0x33, 0x33));
        row.Tier.Text = "";
        row.Conf.Text = "";
        row.Meta.Text = "";
    }

    /// <summary>Blend a colour toward the window background to fade older entries — matches main.py's _dim.</summary>
    private static Color Dim(Color c, double alpha)
    {
        const byte br = 10, bg = 10, bb = 24; // window background is #0a0a18 = (10, 10, 24)
        return Color.FromRgb(
            (byte)(c.R * alpha + br * (1 - alpha)),
            (byte)(c.G * alpha + bg * (1 - alpha)),
            (byte)(c.B * alpha + bb * (1 - alpha)));
    }
}
