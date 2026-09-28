using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SkiaSharp;

namespace MiningOverlay.Windows;

/// <summary>
/// Windows-only screen capture, shared by MiningOverlay.Overlay's live polling loop and
/// MiningOverlay.Config's one-off "test calibration" button, so there's exactly one
/// implementation of "grab a screen region as an SKBitmap" instead of two.
/// </summary>
public static class ScreenCapture
{
    /// <summary>Config uses 1-based monitor numbers (matches the old config.ini docs: "1 = primary monitor").</summary>
    public static Screen GetMonitor(int oneBasedIndex)
    {
        var screens = Screen.AllScreens;
        int idx = oneBasedIndex - 1;
        return idx >= 0 && idx < screens.Length ? screens[idx] : screens[0];
    }

    public static SKBitmap CaptureRegion(Rectangle region)
    {
        using var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(region.Location, Point.Empty, region.Size);

        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var info = new SKImageInfo(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var view = new SKBitmap();
            view.InstallPixels(info, data.Scan0, data.Stride);
            return view.Copy(); // deep copy — safe to use after UnlockBits/dispose
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
