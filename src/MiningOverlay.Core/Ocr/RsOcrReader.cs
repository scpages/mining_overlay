using MiningOverlay.Core.Resources;
using SkiaSharp;

namespace MiningOverlay.Core.Ocr;

public sealed record RsReadResult(string Digits, int? Rs, IReadOnlyList<RsMatch> Matches);

/// <summary>
/// Single entry point tying preprocessing, OCR, and RS resolution together — the same
/// pipeline is used by MiningOverlay.Cli (static screenshots) and MiningOverlay.Overlay
/// (live captures), so calibrating against a screenshot means exactly what it says.
/// </summary>
public static class RsOcrReader
{
    // A single fixed binarization threshold isn't reliable: verified cases where it lost
    // a digit (thin "1" thresholded away) or corrupted one (an "8"/"0" losing its inner
    // gap) — while still producing a *different*, self-confident wrong match. Since a
    // wrong reading can look just as "confident" as a right one, per-candidate confidence
    // alone can't be trusted. Instead, read at several thresholds and prefer whichever
    // value multiple independent readings agree on (falling back to confidence only to
    // break ties) — cross-threshold agreement is much harder to fake than one lucky guess.
    private static readonly byte[] Thresholds = { ImagePreprocessor.DefaultThreshold, 180, 120 };

    public static RsReadResult Read(SKBitmap capturedRegion, string mode = "ship", int maxRs = 100_000)
    {
        var digitCandidates = new List<string>();
        var votes = new Dictionary<int, (int Agreements, int BestConfidence, IReadOnlyList<RsMatch> Matches)>();

        foreach (var threshold in Thresholds)
        {
            using var preprocessed = ImagePreprocessor.Preprocess(capturedRegion, threshold);
            var digits = TesseractRunner.ReadDigits(preprocessed);
            digitCandidates.Add(digits);

            foreach (var (val, confidence, matches) in CandidateValues(digits, mode, maxRs))
            {
                votes[val] = votes.TryGetValue(val, out var existing)
                    ? (existing.Agreements + 1, Math.Max(existing.BestConfidence, confidence), confidence > existing.BestConfidence ? matches : existing.Matches)
                    : (1, confidence, matches);
            }
        }

        var digitsForDisplay = string.Join("/", digitCandidates.Where(d => d.Length > 0).Distinct());

        var winner = votes
            .OrderByDescending(kv => kv.Value.Agreements)
            .ThenByDescending(kv => kv.Value.BestConfidence)
            .Select(kv => (Value: kv.Key, kv.Value.BestConfidence, kv.Value.Matches))
            .FirstOrDefault();

        if (votes.Count == 0)
            return new RsReadResult(digitsForDisplay, null, Array.Empty<RsMatch>());

        return new RsReadResult(digitsForDisplay, winner.Value, winner.Matches);
    }

    /// <summary>
    /// Every plausible RS value obtainable by stripping 0-2 leading digits (icon noise)
    /// from an OCR read, restricted to the same confidence bar the old single-threshold
    /// logic used — low-confidence noise shouldn't be able to out-vote a single strong,
    /// correct reading from another threshold.
    /// </summary>
    private static IEnumerable<(int Value, int Confidence, IReadOnlyList<RsMatch> Matches)> CandidateValues(
        string digits, string mode, int maxRs)
    {
        if (digits.Length < 3)
            yield break;

        int limit = Math.Min(3, digits.Length - 2);
        for (int start = 0; start < limit; start++)
        {
            if (!int.TryParse(digits.AsSpan(start), out var val))
                continue;
            if (val < 1000 || val > maxRs)
                continue;

            var results = ResourceTable.AnalyzeRs(val, mode);
            int confidence = results.Count > 0 ? results[0].Confidence : -1;
            if (confidence >= 60)
                yield return (val, confidence, results);
        }
    }
}
