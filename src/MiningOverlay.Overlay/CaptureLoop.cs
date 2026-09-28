using System.Drawing;
using MiningOverlay.Core.Calibration;
using MiningOverlay.Core.Config;
using MiningOverlay.Core.Ocr;
using MiningOverlay.Windows;

namespace MiningOverlay.Overlay;

/// <summary>
/// Port of main.py's Overlay._loop — polls the game-monitor capture region and reports
/// every confident RS reading. Dedup against recent history is the caller's job
/// (<see cref="HistoryModel"/>), not this loop's, matching the Python split.
/// </summary>
public sealed class CaptureLoop
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly OverlaySettings _settings;
    private readonly Func<string> _getMode;
    private readonly Action<int> _onReading;
    private readonly Action<string>? _onWarning;

    private Thread? _thread;
    private volatile bool _running;

    public CaptureLoop(OverlaySettings settings, Func<string> getMode, Action<int> onReading, Action<string>? onWarning = null)
    {
        _settings = settings;
        _getMode = getMode;
        _onReading = onReading;
        _onWarning = onWarning;
    }

    public void Start()
    {
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Name = "MiningOverlay.CaptureLoop" };
        _thread.Start();
    }

    public void Stop() => _running = false;

    private void Run()
    {
        // Everything here — including one-time setup — must not let an exception
        // escape: an unhandled exception on this background thread would otherwise
        // take the whole process down instead of just failing gracefully.
        try
        {
            var monitor = ScreenCapture.GetMonitor(_settings.GameMonitor);
            var resolution = RegionResolver.Resolve(_settings.ManualRegion, _settings.Ship, monitor.Bounds.Width, monitor.Bounds.Height);
            if (!resolution.Ok)
                _onWarning?.Invoke(resolution.Description);

            var r = resolution.Region;
            var region = new Rectangle(
                monitor.Bounds.X + (int)(monitor.Bounds.Width * r.X),
                monitor.Bounds.Y + (int)(monitor.Bounds.Height * r.Y),
                (int)(monitor.Bounds.Width * r.W),
                (int)(monitor.Bounds.Height * r.H));

            while (_running)
            {
                try
                {
                    using var capture = ScreenCapture.CaptureRegion(region);
                    var result = RsOcrReader.Read(capture, _getMode(), _settings.MaxRs);
                    if (result.Rs is int rs)
                        _onReading(rs);
                }
                catch (Exception ex)
                {
                    // Transient capture/OCR failures are expected occasionally (e.g. game
                    // window not focused) — keep polling rather than crashing the loop.
                    _onWarning?.Invoke($"capture/OCR error (will keep retrying): {ex.Message}");
                }

                Thread.Sleep(PollInterval);
            }
        }
        catch (Exception ex)
        {
            _onWarning?.Invoke($"capture loop stopped unexpectedly: {ex}");
        }
    }
}
