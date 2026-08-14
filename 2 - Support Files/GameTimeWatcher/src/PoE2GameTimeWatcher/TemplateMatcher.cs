using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PoE2GameTimeWatcher;

public sealed class TemplateMatcher : IDisposable
{
    private readonly AppConfig _config;
    private readonly Bitmap _pauseStackTemplate;
    private readonly Bitmap _resumeTemplate;
    private readonly Bitmap _pauseBannerTemplate;
    private readonly Bitmap _exitTemplate;
    private readonly Bitmap _mtxTemplate;
    private readonly GrayImage _mtxGray;
    private readonly GrayImage[] _pauseStackScales;
    private readonly GrayImage[] _resumeScales;
    private readonly GrayImage[] _pauseBannerScales;
    private readonly GrayImage[] _exitScales;
    private Rectangle _cachedContentBounds = Rectangle.Empty;
    private int _cachedContentFrameWidth = -1;
    private int _cachedContentFrameHeight = -1;
    private int _cachedContentUses = 0;

    // Canonicalization already removes most resolution differences, so these
    // scales are only for the residual UI scaling seen between PoE2 display modes.
    private static readonly double[] PauseTemplateScales =
    {
        // Typical canonicalized UI is near 1.0, so test those scales first.
        // Search exits early on a very strong match.
        1.00, 0.94, 1.08, 0.86, 1.16, 0.78, 1.24, 1.32
    };

    public TemplateMatcher(AppConfig config, string baseDirectory)
    {
        _config = config;
        _pauseStackTemplate = new Bitmap(Resolve(baseDirectory, config.PauseStackTemplate));
        _resumeTemplate = new Bitmap(Resolve(baseDirectory, config.ResumeGameTemplate));
        _pauseBannerTemplate = new Bitmap(Resolve(baseDirectory, config.PauseBannerTemplate));
        _exitTemplate = new Bitmap(Resolve(baseDirectory, config.ExitPathOfExileTemplate));
        _mtxTemplate = new Bitmap(Resolve(baseDirectory, config.MtxShopTemplate));

        var pauseStackGray = ExtractGray(_pauseStackTemplate);
        var resumeGray = ExtractGray(_resumeTemplate);
        var pauseBannerGray = ExtractGray(_pauseBannerTemplate);
        var exitGray = ExtractGray(_exitTemplate);
        _mtxGray = ExtractGray(_mtxTemplate);

        // v0.3.6 rebuilt every scaled template on every captured frame. That
        // turned the nominal 10 FPS detector into roughly one analysis every
        // two seconds on the test system. Build the variants once instead.
        _pauseStackScales = BuildScales(pauseStackGray);
        _resumeScales = BuildScales(resumeGray);
        _pauseBannerScales = BuildScales(pauseBannerGray);
        _exitScales = BuildScales(exitGray);
    }

    public DetectionResult Analyze(
        Bitmap frame,
        ManualPauseVisualState stableState = ManualPauseVisualState.Running,
        bool inputHint = false,
        bool detailedDiagnostics = false)
    {
        var content = GetContentBounds(frame);
        using var canonical = Canonicalize(frame, content, _config.CanonicalHeight);
        var source = ExtractGray(canonical);

        double stackScore = 0;
        double resumeScore = 0;
        double bannerScore = 0;
        double exitScore = 0;
        double mtxScore = 0;
        ManualPauseVisualState state = ManualPauseVisualState.Running;

        // State-aware short-circuiting is the main v0.4.0 latency optimization.
        // While paused, verify the currently expected paused surface first. While
        // running, the robust top-button stack is sufficient for the common ESC
        // path; the smaller text fallbacks are only evaluated when the stack is
        // ambiguous or detailed diagnostics are due.
        if (stableState == ManualPauseVisualState.MtxShop)
        {
            mtxScore = SearchMtx(source);
            if (mtxScore >= _config.MtxShopThreshold)
            {
                state = ManualPauseVisualState.MtxShop;
            }
            else
            {
                stackScore = SearchPauseStack(source);
                if (stackScore >= _config.PauseStackThreshold)
                    state = ManualPauseVisualState.PauseMenu;
                else
                    state = ClassifyPauseFallback(source, stackScore, inputHint,
                        ref resumeScore, ref bannerScore, ref exitScore);
            }
        }
        else
        {
            stackScore = SearchPauseStack(source);
            if (stackScore >= _config.PauseStackThreshold)
            {
                state = ManualPauseVisualState.PauseMenu;
            }
            else
            {
                state = ClassifyPauseFallback(source, stackScore, inputHint,
                    ref resumeScore, ref bannerScore, ref exitScore);
                if (state == ManualPauseVisualState.Running)
                {
                    mtxScore = SearchMtx(source);
                    if (mtxScore >= _config.MtxShopThreshold)
                        state = ManualPauseVisualState.MtxShop;
                }
            }
        }

        // Detailed scores are diagnostic-only. Production classification above
        // intentionally avoids paying for four redundant template searches every
        // frame once a strong invariant has already classified the state.
        if (detailedDiagnostics)
        {
            if (resumeScore == 0) resumeScore = SearchResume(source);
            if (bannerScore == 0) bannerScore = SearchBanner(source);
            if (exitScore == 0) exitScore = SearchExit(source);
            if (mtxScore == 0) mtxScore = SearchMtx(source);
        }

        var pauseScore = Math.Max(stackScore,
            Math.Max(resumeScore, Math.Max(bannerScore, exitScore)));

        return new DetectionResult(state, pauseScore, stackScore, resumeScore, bannerScore, exitScore, mtxScore, content);
    }

    private ManualPauseVisualState ClassifyPauseFallback(
        GrayImage source,
        double stackScore,
        bool inputHint,
        ref double resumeScore,
        ref double bannerScore,
        ref double exitScore)
    {
        // The corrected v0.3.9 stack scores around 0.95-0.99 on the real menu.
        // Only spend time on the smaller/less reliable text fallbacks when input
        // says a menu transition is likely or the stack is at least somewhat menu-like.
        if (!inputHint && stackScore < 0.40)
            return ManualPauseVisualState.Running;

        resumeScore = SearchResume(source);
        if (resumeScore >= _config.ResumeGameThreshold)
        {
            bannerScore = SearchBanner(source);
            if (bannerScore >= _config.PauseBannerThreshold * 0.70)
                return ManualPauseVisualState.PauseMenu;
            exitScore = SearchExit(source);
            if (exitScore >= _config.ExitPathOfExileThreshold * 0.75)
                return ManualPauseVisualState.PauseMenu;
        }
        else
        {
            bannerScore = SearchBanner(source);
            if (bannerScore >= _config.PauseBannerThreshold)
            {
                exitScore = SearchExit(source);
                if (exitScore >= _config.ExitPathOfExileThreshold * 0.70)
                    return ManualPauseVisualState.PauseMenu;
            }
        }

        return ManualPauseVisualState.Running;
    }

    // The pause menu is anchored to the center of PoE2's render surface. Earlier
    // builds searched very broad X/Y ranges for every template and every scale.
    // At 5120x1440 that turned a single frame into ~0.5 s of NCC work even after
    // the content-bound fix. The diagnostic captures show that, after canonicalizing
    // to 576 px high, the menu positions are stable to only a few pixels across
    // the tested display modes. Search a conservative local window around those
    // known anchors instead of scanning most of the upper half of the frame.
    private double SearchPauseStack(GrayImage source) => SearchCenteredMultiScaleAt(source, _pauseStackScales,
        expectedY: 116, verticalTolerance: 26, horizontalTolerance: 18, searchStep: 2, sampleStep: 2, earlyStopScore: 0.92);

    private double SearchResume(GrayImage source) => SearchCenteredMultiScaleAt(source, _resumeScales,
        expectedY: 127, verticalTolerance: 24, horizontalTolerance: 16, searchStep: 2, sampleStep: 2, earlyStopScore: 0.92);

    private double SearchBanner(GrayImage source) => SearchCenteredMultiScaleAt(source, _pauseBannerScales,
        expectedY: 78, verticalTolerance: 24, horizontalTolerance: 18, searchStep: 2, sampleStep: 3, earlyStopScore: 0.96);

    private double SearchExit(GrayImage source) => SearchCenteredMultiScaleAt(source, _exitScales,
        expectedY: 290, verticalTolerance: 30, horizontalTolerance: 18, searchStep: 2, sampleStep: 2, earlyStopScore: 0.92);

    private double SearchMtx(GrayImage source) => SearchNcc(source, _mtxGray,
        0, 28, 38, 78, 2, 2);

    private Rectangle GetContentBounds(Bitmap frame)
    {
        // Pillarbox geometry is effectively static during a run. Recompute only
        // when the client dimensions change or periodically as a safety check.
        if (_cachedContentBounds == Rectangle.Empty ||
            _cachedContentFrameWidth != frame.Width ||
            _cachedContentFrameHeight != frame.Height ||
            _cachedContentUses >= 50)
        {
            _cachedContentBounds = FindContentBounds(frame);
            _cachedContentFrameWidth = frame.Width;
            _cachedContentFrameHeight = frame.Height;
            _cachedContentUses = 0;
        }
        _cachedContentUses++;
        return _cachedContentBounds;
    }

    private static GrayImage[] BuildScales(GrayImage template)
    {
        var result = new GrayImage[PauseTemplateScales.Length];
        for (int i = 0; i < PauseTemplateScales.Length; i++)
        {
            var scale = PauseTemplateScales[i];
            result[i] = Math.Abs(scale - 1.0) < 0.0001
                ? template
                : ScaleGray(template, scale);
        }
        return result;
    }

    private static double SearchCenteredMultiScaleAt(
        GrayImage source,
        GrayImage[] scaledTemplates,
        int expectedY,
        int verticalTolerance,
        int horizontalTolerance,
        int searchStep,
        int sampleStep,
        double earlyStopScore = 1.01)
    {
        double best = 0;
        foreach (var scaled in scaledTemplates)
        {
            if (scaled.Width > source.Width || scaled.Height > source.Height)
                continue;

            int expectedX = Math.Max(0, (source.Width - scaled.Width) / 2);
            var score = SearchNcc(source, scaled,
                expectedX - horizontalTolerance,
                expectedX + horizontalTolerance,
                expectedY - verticalTolerance,
                expectedY + verticalTolerance,
                searchStep,
                sampleStep);

            if (score > best) best = score;
            if (best >= earlyStopScore) break;
        }
        return best;
    }

    private static GrayImage ScaleGray(GrayImage source, double scale)
    {
        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var pixels = new byte[width * height];

        for (int y = 0; y < height; y++)
        {
            double srcY = (y + 0.5) / scale - 0.5;
            int y0 = Math.Clamp((int)Math.Floor(srcY), 0, source.Height - 1);
            int y1 = Math.Clamp(y0 + 1, 0, source.Height - 1);
            double fy = Math.Clamp(srcY - Math.Floor(srcY), 0.0, 1.0);

            for (int x = 0; x < width; x++)
            {
                double srcX = (x + 0.5) / scale - 0.5;
                int x0 = Math.Clamp((int)Math.Floor(srcX), 0, source.Width - 1);
                int x1 = Math.Clamp(x0 + 1, 0, source.Width - 1);
                double fx = Math.Clamp(srcX - Math.Floor(srcX), 0.0, 1.0);

                double a = source[x0, y0] * (1.0 - fx) + source[x1, y0] * fx;
                double b = source[x0, y1] * (1.0 - fx) + source[x1, y1] * fx;
                pixels[y * width + x] = (byte)Math.Clamp(
                    (int)Math.Round(a * (1.0 - fy) + b * fy), 0, 255);
            }
        }

        return new GrayImage(width, height, pixels);
    }

    private static string Resolve(string baseDirectory, string relative)
    {
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"GameTimeWatcher template file was not found: {fullPath}", fullPath);
        return fullPath;
    }

    public static Rectangle FindContentBounds(Bitmap bitmap)
    {
        const int samplesY = 24;
        const int nonBlackThreshold = 5;
        const double requiredFraction = 0.08;

        bool IsContentColumn(int x)
        {
            int nonBlack = 0;
            for (int s = 0; s < samplesY; s++)
            {
                int y = Math.Clamp((int)Math.Round((s + 0.5) * bitmap.Height / samplesY), 0, bitmap.Height - 1);
                var c = bitmap.GetPixel(x, y);
                if (c.R > nonBlackThreshold || c.G > nonBlackThreshold || c.B > nonBlackThreshold)
                    nonBlack++;
            }
            return nonBlack >= Math.Ceiling(samplesY * requiredFraction);
        }

        // CopyFromScreen captures anything layered over PoE2. On a pillarboxed
        // ultrawide display, an always-on-top LiveSplit window in a black side
        // bar therefore looks like a second island of "content". v0.3.8 used
        // the first non-black column and the last non-black column, joining that
        // overlay to the game image and moving the assumed screen center ~125 px
        // to the right on the 2048x576 diagnostic capture.
        //
        // Identify separate horizontal content runs instead. Prefer the run that
        // contains the physical client center (normally the PoE2 render surface);
        // otherwise use the widest run. This preserves real 16:9/ultrawide game
        // content while ignoring LiveSplit/OBS/other overlays sitting in a black
        // pillarbox. Small gaps are bridged so a dark scene does not fragment the
        // game surface.
        int step = Math.Max(1, bitmap.Width / 1024);
        int maxGapColumns = Math.Max(2, 12 / step);
        var runs = new List<(int Start, int End)>();
        int runStart = -1;
        int lastContent = -1;
        int gapColumns = 0;

        for (int x = 0; x < bitmap.Width; x += step)
        {
            if (IsContentColumn(x))
            {
                if (runStart < 0) runStart = x;
                lastContent = x;
                gapColumns = 0;
            }
            else if (runStart >= 0)
            {
                gapColumns++;
                if (gapColumns > maxGapColumns)
                {
                    runs.Add((runStart, lastContent));
                    runStart = -1;
                    lastContent = -1;
                    gapColumns = 0;
                }
            }
        }
        if (runStart >= 0 && lastContent >= runStart)
            runs.Add((runStart, lastContent));

        if (runs.Count == 0)
            return new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        int centerX = bitmap.Width / 2;
        var selected = runs
            .Where(r => r.Start <= centerX && r.End >= centerX)
            .OrderByDescending(r => r.End - r.Start)
            .FirstOrDefault();

        if (selected.End <= selected.Start)
            selected = runs.OrderByDescending(r => r.End - r.Start).First();

        int left = Math.Max(0, selected.Start - step);
        int right = Math.Min(bitmap.Width - 1, selected.End + step);
        int width = right - left + 1;

        if (width < bitmap.Width / 3)
            return new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        return new Rectangle(left, 0, width, bitmap.Height);
    }

    private static Bitmap Canonicalize(Bitmap source, Rectangle content, int targetHeight)
    {
        if (content.Height == targetHeight)
            return source.Clone(content, PixelFormat.Format24bppRgb);

        var targetWidth = Math.Max(1, (int)Math.Round(content.Width * (targetHeight / (double)content.Height)));
        var output = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(output);
        g.InterpolationMode = InterpolationMode.Bilinear;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight), content, GraphicsUnit.Pixel);
        return output;
    }

    private sealed class GrayImage
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }
        public GrayImage(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
        public byte this[int x, int y] => Pixels[y * Width + x];
    }

    private static GrayImage ExtractGray(Bitmap bitmap)
    {
        Bitmap? converted = null;
        Bitmap source = bitmap;
        if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
        {
            converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(converted);
            g.DrawImageUnscaled(bitmap, 0, 0);
            source = converted;
        }

        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            int stride = Math.Abs(data.Stride);
            var raw = new byte[stride * source.Height];
            Marshal.Copy(data.Scan0, raw, 0, raw.Length);
            var gray = new byte[source.Width * source.Height];
            for (int y = 0; y < source.Height; y++)
            {
                int sourceY = data.Stride >= 0 ? y : source.Height - 1 - y;
                int row = sourceY * stride;
                int dst = y * source.Width;
                for (int x = 0; x < source.Width; x++)
                {
                    int p = row + x * 3;
                    int b = raw[p]; int g = raw[p + 1]; int r = raw[p + 2];
                    gray[dst + x] = (byte)((77 * r + 150 * g + 29 * b) >> 8);
                }
            }
            return new GrayImage(source.Width, source.Height, gray);
        }
        finally
        {
            source.UnlockBits(data);
            converted?.Dispose();
        }
    }

    private static double SearchNcc(GrayImage source, GrayImage template,
        int minX, int maxX, int minY, int maxY, int searchStep, int sampleStep)
    {
        minX = Math.Clamp(minX, 0, Math.Max(0, source.Width - template.Width));
        maxX = Math.Clamp(maxX, minX, Math.Max(minX, source.Width - template.Width));
        minY = Math.Clamp(minY, 0, Math.Max(0, source.Height - template.Height));
        maxY = Math.Clamp(maxY, minY, Math.Max(minY, source.Height - template.Height));

        double best = 0;
        for (int y = minY; y <= maxY; y += Math.Max(1, searchStep))
        for (int x = minX; x <= maxX; x += Math.Max(1, searchStep))
        {
            double score = Ncc(source, template, x, y, sampleStep);
            if (score > best) best = score;
        }
        return best;
    }

    private static double Ncc(GrayImage source, GrayImage template, int offsetX, int offsetY, int sampleStep)
    {
        // Algebraically equivalent one-pass Pearson/NCC calculation. v0.3.x
        // walked every sampled pixel twice for every candidate location.
        double sumA = 0, sumB = 0, sumAA = 0, sumBB = 0, sumAB = 0;
        int count = 0;
        for (int y = 0; y < template.Height; y += sampleStep)
        for (int x = 0; x < template.Width; x += sampleStep)
        {
            double a = template[x, y];
            double b = source[offsetX + x, offsetY + y];
            sumA += a;
            sumB += b;
            sumAA += a * a;
            sumBB += b * b;
            sumAB += a * b;
            count++;
        }
        if (count == 0) return 0;

        double numerator = sumAB - (sumA * sumB / count);
        double denomA = sumAA - (sumA * sumA / count);
        double denomB = sumBB - (sumB * sumB / count);
        double denom = Math.Sqrt(Math.Max(0, denomA) * Math.Max(0, denomB));
        if (denom <= 1e-9) return 0;
        return Math.Max(0, numerator / denom);
    }

    public void Dispose()
    {
        _pauseStackTemplate.Dispose();
        _resumeTemplate.Dispose();
        _pauseBannerTemplate.Dispose();
        _exitTemplate.Dispose();
        _mtxTemplate.Dispose();
    }
}
