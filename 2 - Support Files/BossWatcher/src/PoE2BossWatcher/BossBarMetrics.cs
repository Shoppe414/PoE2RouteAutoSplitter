using System.Drawing;
using System.Drawing.Imaging;

namespace PoE2BossWatcher;

public sealed record BossBarMetrics(
    double RedFraction,
    double LightFraction,
    double FrameGoldFraction,
    double NameGoldFraction,
    double HealthRedRunFraction,
    double LeftHealthRedRunFraction,
    double RightHealthRedRunFraction,
    double LeftLaneNameGoldFraction,
    double RightLaneNameGoldFraction,
    double CenterNameGoldFraction,
    bool DualNameSignature,
    bool DualSignature)
{
    /// <summary>
    /// Single-pass ROI analysis. v0.1.9 adds lane-aware measurements for PoE2's horizontal
    /// dual-boss UI. Live capture is centered and height-scaled, so the boss container remains
    /// physically comparable across aspect ratios. We measure name-colored anchor bands around
    /// ~25% and ~75% of the normal boss-name ROI, plus red-health runs in each half of BossHealthRoi.
    /// </summary>
    public static BossBarMetrics Analyze(Bitmap bitmap, AppConfig config, bool includeDiagnostics = true)
    {
        var full = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var frameRoi = ScreenCapture.ToPixelRect(bitmap.Width, bitmap.Height, config.BossFrameRoi);
        var nameRoi = ScreenCapture.ToPixelRect(bitmap.Width, bitmap.Height, config.BossNameRoi);
        var healthRoi = ScreenCapture.ToPixelRect(bitmap.Width, bitmap.Height, config.BossHealthRoi);

        // The anchors intentionally avoid the center where a normal single-boss name is drawn.
        // In the supplied dual capture the two names are centered near 25% and 75% of BossNameRoi.
        // Use the lower 70% of the name band so the long gold/red health-bar highlight near
        // the top cannot masquerade as name text in both lanes.
        var leftNameAnchor = Slice(nameRoi, 0.10, 0.40, 0.30, 1.00);
        var rightNameAnchor = Slice(nameRoi, 0.60, 0.90, 0.30, 1.00);
        var centerNameGap = Slice(nameRoi, 0.45, 0.55, 0.30, 1.00);
        var leftHealth = SliceHorizontal(healthRoi, 0.00, 0.50);
        var rightHealth = SliceHorizontal(healthRoi, 0.50, 1.00);

        long red = 0;
        long light = 0;
        long frameGold = 0;
        long nameGold = 0;
        long leftNameGold = 0;
        long rightNameGold = 0;
        long centerNameGold = 0;
        var longestHealthRun = 0;
        var longestLeftHealthRun = 0;
        var longestRightHealthRun = 0;

        var data = bitmap.LockBits(full, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    var currentHealthRun = 0;
                    var currentLeftRun = 0;
                    var currentRightRun = 0;
                    var inHealthRow = y >= healthRoi.Top && y < healthRoi.Bottom;
                    var inNameRow = y >= nameRoi.Top && y < nameRoi.Bottom;
                    var inFrameRow = includeDiagnostics && y >= frameRoi.Top && y < frameRoi.Bottom;

                    for (var x = 0; x < data.Width; x++)
                    {
                        var p = row + x * 3;
                        var b = (int)p[0];
                        var g = (int)p[1];
                        var r = (int)p[2];

                        if (includeDiagnostics)
                        {
                            if (IsHealthRedPixel(r, g, b)) red++;
                            var lum = (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
                            if (lum >= 185) light++;

                            if (inFrameRow && x >= frameRoi.Left && x < frameRoi.Right &&
                                r >= 90 && g >= 50 && b <= 100 && r - g >= 20 && g - b >= 5)
                                frameGold++;
                        }

                        if (inNameRow && x >= nameRoi.Left && x < nameRoi.Right &&
                            ScreenCapture.IsBossNamePixel(r, g, b, config))
                        {
                            nameGold++;
                            if (y >= leftNameAnchor.Top && y < leftNameAnchor.Bottom &&
                                x >= leftNameAnchor.Left && x < leftNameAnchor.Right) leftNameGold++;
                            if (y >= rightNameAnchor.Top && y < rightNameAnchor.Bottom &&
                                x >= rightNameAnchor.Left && x < rightNameAnchor.Right) rightNameGold++;
                            if (y >= centerNameGap.Top && y < centerNameGap.Bottom &&
                                x >= centerNameGap.Left && x < centerNameGap.Right) centerNameGold++;
                        }

                        if (!inHealthRow || x < healthRoi.Left || x >= healthRoi.Right)
                            continue;

                        var isRed = IsHealthRedPixel(r, g, b);
                        if (isRed)
                        {
                            currentHealthRun++;
                            if (currentHealthRun > longestHealthRun) longestHealthRun = currentHealthRun;
                        }
                        else
                        {
                            currentHealthRun = 0;
                        }

                        if (x >= leftHealth.Left && x < leftHealth.Right)
                        {
                            if (isRed)
                            {
                                currentLeftRun++;
                                if (currentLeftRun > longestLeftHealthRun) longestLeftHealthRun = currentLeftRun;
                            }
                            else currentLeftRun = 0;
                        }
                        else if (x >= rightHealth.Left && x < rightHealth.Right)
                        {
                            if (isRed)
                            {
                                currentRightRun++;
                                if (currentRightRun > longestRightHealthRun) longestRightHealthRun = currentRightRun;
                            }
                            else currentRightRun = 0;
                        }
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        var fullTotal = (long)bitmap.Width * bitmap.Height;
        var frameTotal = (long)frameRoi.Width * frameRoi.Height;
        var nameTotal = (long)nameRoi.Width * nameRoi.Height;
        var leftAnchorTotal = (long)leftNameAnchor.Width * leftNameAnchor.Height;
        var rightAnchorTotal = (long)rightNameAnchor.Width * rightNameAnchor.Height;
        var centerGapTotal = (long)centerNameGap.Width * centerNameGap.Height;

        var leftNameFraction = leftAnchorTotal > 0 ? (double)leftNameGold / leftAnchorTotal : 0;
        var rightNameFraction = rightAnchorTotal > 0 ? (double)rightNameGold / rightAnchorTotal : 0;
        var centerNameFraction = centerGapTotal > 0 ? (double)centerNameGold / centerGapTotal : 0;
        var leftRunFraction = leftHealth.Width > 0 ? (double)longestLeftHealthRun / leftHealth.Width : 0;
        var rightRunFraction = rightHealth.Width > 0 ? (double)longestRightHealthRun / rightHealth.Width : 0;

        // Name anchors are the topology discriminator; at least one health lane is required as
        // structural corroboration. A single full-width boss may have red in both halves, but its
        // centered name does not normally occupy BOTH left/right name anchors.
        var dualNameSignature =
            leftNameFraction >= config.DualLayoutMinLaneNameGoldFraction &&
            rightNameFraction >= config.DualLayoutMinLaneNameGoldFraction &&
            centerNameFraction <= config.DualLayoutMaxCenterNameGoldFraction;
        var dualSignature = dualNameSignature &&
            (leftRunFraction >= config.DualLayoutMinLaneHealthRunFraction ||
             rightRunFraction >= config.DualLayoutMinLaneHealthRunFraction);

        return new BossBarMetrics(
            includeDiagnostics && fullTotal > 0 ? (double)red / fullTotal : 0,
            includeDiagnostics && fullTotal > 0 ? (double)light / fullTotal : 0,
            includeDiagnostics && frameTotal > 0 ? (double)frameGold / frameTotal : 0,
            nameTotal > 0 ? (double)nameGold / nameTotal : 0,
            healthRoi.Width > 0 ? (double)longestHealthRun / healthRoi.Width : 0,
            leftRunFraction,
            rightRunFraction,
            leftNameFraction,
            rightNameFraction,
            centerNameFraction,
            dualNameSignature,
            dualSignature);
    }

    private static Rectangle SliceHorizontal(Rectangle parent, double startFraction, double endFraction)
        => Slice(parent, startFraction, endFraction, 0.0, 1.0);

    private static Rectangle Slice(Rectangle parent, double xStart, double xEnd, double yStart, double yEnd)
    {
        var left = parent.Left + (int)Math.Round(parent.Width * xStart);
        var right = parent.Left + (int)Math.Round(parent.Width * xEnd);
        var top = parent.Top + (int)Math.Round(parent.Height * yStart);
        var bottom = parent.Top + (int)Math.Round(parent.Height * yEnd);
        left = Math.Clamp(left, parent.Left, parent.Right - 1);
        right = Math.Clamp(right, left + 1, parent.Right);
        top = Math.Clamp(top, parent.Top, parent.Bottom - 1);
        bottom = Math.Clamp(bottom, top + 1, parent.Bottom);
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static bool IsHealthRedPixel(int r, int g, int b)
        => r >= 80 && r >= g * 1.30 && r >= b * 1.15 && r - g >= 18;
}
