using System.Drawing;
using System.Drawing.Imaging;

namespace PoE2BossWatcher;

/// <summary>
/// Spatial fingerprint of the boss-name color mask captured when OCR identifies a boss.
/// v0.1.9 allows the template to be learned from the full single-boss name ROI or from a
/// left/right half-lane when PoE2 presents two boss bars side-by-side.
/// </summary>
public sealed class BossNameTemplate
{
    private readonly Point[] _referencePoints;
    private readonly int _roiWidth;
    private readonly int _roiHeight;
    private readonly NormalizedRect _roi;

    private BossNameTemplate(Point[] referencePoints, int roiWidth, int roiHeight, NormalizedRect roi)
    {
        _referencePoints = referencePoints;
        _roiWidth = roiWidth;
        _roiHeight = roiHeight;
        _roi = roi;
    }

    public int ReferencePixelCount => _referencePoints.Length;

    public static BossNameTemplate Capture(Bitmap bitmap, AppConfig config)
        => Capture(bitmap, config, config.BossNameRoi);

    public static BossNameTemplate Capture(Bitmap bitmap, AppConfig config, BossLane lane)
        => Capture(bitmap, config, ScreenCapture.GetBossNameLaneRoi(config, lane));

    public static BossNameTemplate Capture(Bitmap bitmap, AppConfig config, NormalizedRect roi)
    {
        var rct = ScreenCapture.ToPixelRect(bitmap.Width, bitmap.Height, roi);
        var points = new List<Point>();
        var centerWidth = Math.Max(1, (int)Math.Round(rct.Width * config.TrackedTemplateCenterWidthFraction));
        var centerLeft = Math.Max(0, (rct.Width - centerWidth) / 2);
        var centerRight = Math.Min(rct.Width - 1, centerLeft + centerWidth - 1);
        var data = bitmap.LockBits(rct, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            unsafe
            {
                for (var y = 0; y < data.Height; y++)
                {
                    var row = (byte*)data.Scan0 + y * data.Stride;
                    for (var x = centerLeft; x <= centerRight; x++)
                    {
                        var p = row + x * 3;
                        var b = (int)p[0];
                        var g = (int)p[1];
                        var r = (int)p[2];
                        if (ScreenCapture.IsBossNamePixel(r, g, b, config))
                            points.Add(new Point(x, y));
                    }
                }
            }
        }
        finally { bitmap.UnlockBits(data); }

        return new BossNameTemplate(points.ToArray(), rct.Width, rct.Height, roi);
    }

    public double MeasureCoverage(Bitmap bitmap, AppConfig config)
    {
        if (_referencePoints.Length == 0) return 0;
        var rct = ScreenCapture.ToPixelRect(bitmap.Width, bitmap.Height, _roi);
        if (rct.Width != _roiWidth || rct.Height != _roiHeight) return 0;

        var radius = config.TrackedTemplatePixelSearchRadius;
        var data = bitmap.LockBits(rct, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        var matched = 0;
        try
        {
            unsafe
            {
                foreach (var point in _referencePoints)
                {
                    var found = false;
                    var minY = Math.Max(0, point.Y - radius);
                    var maxY = Math.Min(data.Height - 1, point.Y + radius);
                    var minX = Math.Max(0, point.X - radius);
                    var maxX = Math.Min(data.Width - 1, point.X + radius);

                    for (var y = minY; y <= maxY && !found; y++)
                    {
                        var row = (byte*)data.Scan0 + y * data.Stride;
                        for (var x = minX; x <= maxX; x++)
                        {
                            var p = row + x * 3;
                            var b = (int)p[0];
                            var g = (int)p[1];
                            var r = (int)p[2];
                            if (ScreenCapture.IsBossNamePixel(r, g, b, config))
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found) matched++;
                }
            }
        }
        finally { bitmap.UnlockBits(data); }

        return (double)matched / _referencePoints.Length;
    }
}
