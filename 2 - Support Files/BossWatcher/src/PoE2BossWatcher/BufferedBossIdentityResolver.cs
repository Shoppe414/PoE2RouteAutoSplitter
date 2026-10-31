using System.Drawing;

namespace PoE2BossWatcher;

/// <summary>
/// Performs delayed identity OCR against cloned boss-ROI frames on a worker thread. A dedicated
/// Tesseract engine is created for the job so the live capture thread never shares an OCR engine
/// concurrently with post-disappearance processing.
/// </summary>
public static class BufferedBossIdentityResolver
{
    public static Task<BufferedBossIdentityResult?> ResolveAsync(
        List<BufferedBossFrame> frames,
        AppConfig config,
        BossNameMatcher matcher,
        Action<string>? diagnostic = null,
        DebugImageWriter? images = null)
    {
        var completion = new TaskCompletionSource<BufferedBossIdentityResult?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = new Thread(() =>
        {
            try
            {
                completion.SetResult(Resolve(frames, config, matcher, diagnostic, images));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "PoE2BossWatcher-BufferedOCR",
            Priority = ThreadPriority.BelowNormal
        };
        worker.Start();
        return completion.Task;
    }

    private static BufferedBossIdentityResult? Resolve(
        List<BufferedBossFrame> frames,
        AppConfig config,
        BossNameMatcher matcher,
        Action<string>? diagnostic,
        DebugImageWriter? images)
    {
        try
        {
            if (frames.Count == 0) return null;

            using var workerOcr = new OcrService(config.TessdataParent, config.GameLanguage);
            BossMatch? bestMatch = null;
            OcrRead bestRead = new("", 0);
            DateTimeOffset bestWhen = DateTimeOffset.MinValue;
            string bestSource = "none";
            var attempts = 0;

            // Save only the top few buffered candidates and do the PNG work on this below-normal
            // worker thread so diagnostics never block WGC capture. These images show whether the
            // name was actually rendered, whether preprocessing destroyed it, or whether matching
            // rejected otherwise readable text.
            if (images is not null && config.SaveStateChangeImages)
            {
                var diagnosticIndex = 0;
                foreach (var diagnosticFrame in frames.OrderByDescending(frame => frame.QualityScore).Take(3))
                {
                    using var diagnosticGold = ScreenCapture.PreprocessBossNameForOcr(
                        diagnosticFrame.Bitmap, config, BossLane.Single, OcrPreprocessMode.Gold);
                    using var diagnosticBroad = ScreenCapture.PreprocessBossNameForOcr(
                        diagnosticFrame.Bitmap, config, BossLane.Single, OcrPreprocessMode.Broad);
                    images.SaveOcrDiagnostic($"ANON_POST_{diagnosticIndex}_Q{diagnosticFrame.QualityScore:F3}",
                        diagnosticFrame.Bitmap, diagnosticGold, diagnosticBroad, null, null);
                    diagnosticIndex++;
                }
            }

            foreach (var frame in frames.OrderByDescending(frame => frame.QualityScore))
            {
                using (var gold = ScreenCapture.PreprocessBossNameForOcr(
                    frame.Bitmap, config, BossLane.Single, OcrPreprocessMode.Gold))
                {
                    attempts++;
                    var read = workerOcr.ReadSingleLine(gold);
                    var nearest = matcher.MatchBest(read.Text);
                    var match = nearest is not null && nearest.Similarity >= config.MinOcrSimilarity ? nearest : null;
                    diagnostic?.Invoke(FormatAttempt(frame, "gold", read, nearest, config.MinOcrSimilarity));
                    KeepBest(match, read, frame.When, "buffer-gold", ref bestMatch, ref bestRead, ref bestWhen, ref bestSource);
                }

                if (bestMatch?.Similarity >= 0.995) break;

                using (var broad = ScreenCapture.PreprocessBossNameForOcr(
                    frame.Bitmap, config, BossLane.Single, OcrPreprocessMode.Broad))
                {
                    attempts++;
                    var read = workerOcr.ReadSingleLine(broad);
                    var nearest = matcher.MatchBest(read.Text);
                    var match = nearest is not null && nearest.Similarity >= config.MinOcrSimilarity ? nearest : null;
                    diagnostic?.Invoke(FormatAttempt(frame, "broad", read, nearest, config.MinOcrSimilarity));
                    KeepBest(match, read, frame.When, "buffer-broad", ref bestMatch, ref bestRead, ref bestWhen, ref bestSource);
                }

                if (bestMatch?.Similarity >= 0.995) break;
            }

            // If no individual frame resolves identity, combine the buffered masks. This is useful
            // for sub-second encounters where antialiasing leaves different glyph fragments in
            // adjacent captured frames.
            if (bestMatch is null && frames.Count >= 2)
            {
                using var history = new OcrLaneHistory(Math.Max(config.OcrTemporalFrameCount, frames.Count));
                foreach (var frame in frames.OrderBy(frame => frame.When))
                    history.AddFrame(frame.Bitmap, config, BossLane.Single);

                foreach (var mode in new[] { OcrPreprocessMode.Gold, OcrPreprocessMode.Broad })
                {
                    using var temporal = history.CreateTemporalScaled(config, mode, 2);
                    if (temporal is null) continue;
                    attempts++;
                    var read = workerOcr.ReadSingleLine(temporal);
                    var nearest = matcher.MatchBest(read.Text);
                    var match = nearest is not null && nearest.Similarity >= config.MinOcrSimilarity ? nearest : null;
                    diagnostic?.Invoke($"ANONYMOUS_POST_OCR_ATTEMPT | frameWhen={frames[^1].When:O} | quality=temporal | mode={mode.ToString().ToLowerInvariant()} | conf={read.Confidence:F3} | nearest={nearest?.Boss.Id ?? "none"} | similarity={(nearest?.Similarity ?? 0):F3} | threshold={config.MinOcrSimilarity:F3} | accepted={match is not null} | text={Clean(read.Text)}");
                    KeepBest(match, read, frames[^1].When,
                        mode == OcrPreprocessMode.Gold ? "buffer-gold-temporal" : "buffer-broad-temporal",
                        ref bestMatch, ref bestRead, ref bestWhen, ref bestSource);
                }
            }

            return bestMatch is null
                ? null
                : new BufferedBossIdentityResult(bestRead, bestMatch, bestSource, bestWhen, attempts);
        }
        finally
        {
            foreach (var frame in frames)
                frame.Dispose();
        }
    }

    private static string FormatAttempt(BufferedBossFrame frame, string mode, OcrRead read, BossMatch? nearest, double threshold)
        => $"ANONYMOUS_POST_OCR_ATTEMPT | frameWhen={frame.When:O} | quality={frame.QualityScore:F4} | mode={mode} | conf={read.Confidence:F3} | nearest={nearest?.Boss.Id ?? "none"} | similarity={(nearest?.Similarity ?? 0):F3} | threshold={threshold:F3} | accepted={nearest is not null && nearest.Similarity >= threshold} | text={Clean(read.Text)}";

    private static string Clean(string value)
        => value.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();

    private static void KeepBest(
        BossMatch? incoming,
        OcrRead read,
        DateTimeOffset when,
        string source,
        ref BossMatch? bestMatch,
        ref OcrRead bestRead,
        ref DateTimeOffset bestWhen,
        ref string bestSource)
    {
        if (incoming is null) return;
        if (bestMatch is not null && incoming.Similarity <= bestMatch.Similarity) return;
        bestMatch = incoming;
        bestRead = read;
        bestWhen = when;
        bestSource = source;
    }
}

public sealed record BufferedBossIdentityResult(
    OcrRead Read,
    BossMatch Match,
    string Source,
    DateTimeOffset FrameWhen,
    int Attempts);
