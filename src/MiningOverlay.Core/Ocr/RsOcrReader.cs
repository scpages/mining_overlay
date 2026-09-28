using MiningOverlay.Core.Resources;
using SkiaSharp;

namespace MiningOverlay.Core.Ocr;

public sealed record RsReadResult(string Digits, int? Rs, IReadOnlyList<RsMatch> Matches);

/// <summary>
/// Single entry point tying preprocessing, OCR, and RS resolution together — the same
/// pipeline is used by MiningOverlay.Cli (static screenshots) and MiningOverlay.Overlay
/// (live captures), so calibrating against a screenshot means exactly what it says.
///
/// Port of main.py's _best_rs.
/// </summary>
public static class RsOcrReader
{
    public static RsReadResult Read(SKBitmap capturedRegion, string mode = "ship", int maxRs = 100_000)
    {
        using var preprocessed = ImagePreprocessor.Preprocess(capturedRegion);
        var digits = TesseractRunner.ReadDigits(preprocessed);
        var rs = BestRs(digits, mode, maxRs);
        var matches = rs is int v ? ResourceTable.AnalyzeRs(v, mode) : Array.Empty<RsMatch>();
        return new RsReadResult(digits, rs, matches);
    }

    /// <summary>
    /// Return the most plausible RS value from OCR digits. Tries stripping 0-2 leading
    /// digits (icon noise) and picks the candidate with the highest AnalyzeRs confidence.
    /// </summary>
    private static int? BestRs(string digits, string mode, int maxRs)
    {
        if (digits.Length < 3)
            return null;

        int? bestVal = null;
        int bestConf = -1;
        int limit = Math.Min(3, digits.Length - 2);

        for (int start = 0; start < limit; start++)
        {
            if (!int.TryParse(digits.AsSpan(start), out var val))
                continue;
            if (val < 1000 || val > maxRs)
                continue;

            var results = ResourceTable.AnalyzeRs(val, mode);
            int conf = results.Count > 0 ? results[0].Confidence : -1;
            if (conf > bestConf)
            {
                bestConf = conf;
                bestVal = val;
            }
        }

        return bestConf >= 60 ? bestVal : null;
    }
}
