using System.Diagnostics;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace MiningOverlay.Core.Ocr;

/// <summary>
/// Shells out to the system `tesseract` binary, mirroring pytesseract's approach in the
/// Python version — no OCR NuGet package needed, and it behaves identically on Linux
/// (used here for calibration) and Windows (used by the overlay at runtime).
/// </summary>
public static class TesseractRunner
{
    // Fast path uses PSM 7 (single line) — good enough most of the time. When that comes
    // back empty/too short, fall back to trying a few other page segmentation modes,
    // since HUD fonts vary by ship.
    private static readonly int[] PsmFallbacks = { 6, 8, 11, 13 };

    private static readonly string[] CandidateExecutables =
    {
        "tesseract",
        @"C:\Program Files\Tesseract-OCR\tesseract.exe",
        @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe",
    };

    public static string ReadDigits(SKBitmap preprocessed)
    {
        var digits = RunOnce(preprocessed, psm: 7);
        if (digits.Length >= 3)
            return digits;

        foreach (var psm in PsmFallbacks)
        {
            digits = RunOnce(preprocessed, psm);
            if (digits.Length >= 3)
                break;
        }
        return digits;
    }

    private static string RunOnce(SKBitmap image, int psm)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"mining-overlay-ocr-{Guid.NewGuid():N}.png");
        try
        {
            using (var fs = File.OpenWrite(tmpFile))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                data.SaveTo(fs);

            var text = RunTesseract(tmpFile, psm);
            return Regex.Replace(text, "[^0-9]", "");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    private static string RunTesseract(string imagePath, int psm)
    {
        Exception? lastError = null;
        foreach (var exe in CandidateExecutables)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    ArgumentList =
                    {
                        imagePath, "stdout",
                        "--psm", psm.ToString(),
                        "--oem", "3",
                        "-c", "tessedit_char_whitelist=0123456789.,",
                    },
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = Process.Start(psi)
                    ?? throw new InvalidOperationException($"Failed to start {exe}");
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output.Trim();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            "Could not find the `tesseract` executable on PATH or in the default install locations. " +
            "Install Tesseract-OCR: https://github.com/UB-Mannheim/tesseract/wiki", lastError);
    }
}
