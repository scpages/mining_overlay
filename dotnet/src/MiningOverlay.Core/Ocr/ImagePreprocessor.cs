using SkiaSharp;

namespace MiningOverlay.Core.Ocr;

/// <summary>
/// Port of main.py's _preprocess — crops the captured region down to just the RS text,
/// then binarizes + upscales it so Tesseract reads it cleanly. Keep the constants here
/// in sync with the Python version.
/// </summary>
public static class ImagePreprocessor
{
    public const double CropTop    = 0.20; // fraction of region height trimmed off the top
    public const double CropBottom = 0.62; // fraction of region height kept, measured from the top
    public const double CropRight  = 0.95; // fraction of region width kept, measured from the left
    public const byte   Threshold  = 140;  // grayscale cutoff for black/white binarization

    private const int Scale = 4;
    private const int Pad   = 20;

    public static SKBitmap Preprocess(SKBitmap source)
    {
        int w = source.Width, h = source.Height;
        int cropTopY = (int)(h * CropTop);
        int cropW    = (int)(w * CropRight);
        int cropH    = (int)(h * CropBottom) - cropTopY;

        int scaledW = cropW * Scale;
        int scaledH = cropH * Scale;

        using var scaled = new SKBitmap(new SKImageInfo(scaledW, scaledH, SKColorType.Bgra8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(scaled))
        {
            var srcRect = new SKRect(0, cropTopY, cropW, cropTopY + cropH);
            var dstRect = new SKRect(0, 0, scaledW, scaledH);
            canvas.DrawBitmap(source, srcRect, dstRect, new SKSamplingOptions(SKCubicResampler.Mitchell));
        }

        var padded = new SKBitmap(new SKImageInfo(scaledW + Pad * 2, scaledH + Pad * 2, SKColorType.Gray8, SKAlphaType.Opaque));

        unsafe
        {
            byte* dst = (byte*)padded.GetPixels().ToPointer();
            int dstStride = padded.RowBytes;
            for (int i = 0; i < dstStride * padded.Height; i++)
                dst[i] = 255; // white background

            byte* src = (byte*)scaled.GetPixels().ToPointer();
            int srcStride = scaled.RowBytes; // BGRA8888 = 4 bytes/pixel

            for (int y = 0; y < scaledH; y++)
            {
                byte* srcRow = src + y * srcStride;
                byte* dstRow = dst + (y + Pad) * dstStride + Pad;
                for (int x = 0; x < scaledW; x++)
                {
                    byte b = srcRow[x * 4], g = srcRow[x * 4 + 1], r = srcRow[x * 4 + 2];
                    byte gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                    dstRow[x] = gray > Threshold ? (byte)0 : (byte)255;
                }
            }
        }

        return padded;
    }
}
