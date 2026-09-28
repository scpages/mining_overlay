using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MiningOverlay.Overlay;

public partial class App : System.Windows.Application
{
    public App()
    {
        // Log first, so we have a record even if the process still goes down —
        // there's no console attached to see this otherwise.
        DispatcherUnhandledException += (_, e) =>
        {
            OverlayLog.Write($"UI CRASH: {e.Exception}");
            MessageBox.Show(
                $"The overlay hit an unexpected error and needs to close:\n\n{e.Exception.Message}\n\n" +
                "Details were saved to overlay.log next to your settings (%APPDATA%\\MiningOverlay).",
                "SC Mining Overlay", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                OverlayLog.Write($"FATAL: {ex}");
        };
    }
}
