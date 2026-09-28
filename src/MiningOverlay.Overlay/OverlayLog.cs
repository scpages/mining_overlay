using System.IO;

namespace MiningOverlay.Overlay;

/// <summary>
/// Best-effort file logger — the overlay has no console, so this is the only place
/// warnings/crashes end up regardless of how it was launched (Config's Start overlay
/// button, double-clicking the exe, or a debugger).
/// </summary>
internal static class OverlayLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiningOverlay", "overlay.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTime.Now:u}  {message}\n");
        }
        catch
        {
            // Logging must never itself crash the app.
        }
    }
}
