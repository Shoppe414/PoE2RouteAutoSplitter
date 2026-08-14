using System.Drawing;
using System.Drawing.Imaging;

namespace PoE2BossWatcher;

public sealed class DebugImageWriter
{
    private readonly string _dir;

    public DebugImageWriter(string directory)
    {
        _dir = directory;
        Directory.CreateDirectory(_dir);
    }

    public void Save(string label, Bitmap raw, Bitmap? processed = null)
    {
        var safe = Safe(label);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        raw.Save(Path.Combine(_dir, $"{stamp}_{safe}_raw.png"), ImageFormat.Png);
        processed?.Save(Path.Combine(_dir, $"{stamp}_{safe}_ocr.png"), ImageFormat.Png);
    }

    public void SaveOcrDiagnostic(
        string label,
        Bitmap raw,
        Bitmap? gold,
        Bitmap? broad,
        Bitmap? temporalGold,
        Bitmap? temporalBroad)
    {
        var safe = Safe(label);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        raw.Save(Path.Combine(_dir, $"{stamp}_{safe}_raw.png"), ImageFormat.Png);
        gold?.Save(Path.Combine(_dir, $"{stamp}_{safe}_gold.png"), ImageFormat.Png);
        broad?.Save(Path.Combine(_dir, $"{stamp}_{safe}_broad.png"), ImageFormat.Png);
        temporalGold?.Save(Path.Combine(_dir, $"{stamp}_{safe}_temporal-gold.png"), ImageFormat.Png);
        temporalBroad?.Save(Path.Combine(_dir, $"{stamp}_{safe}_temporal-broad.png"), ImageFormat.Png);
    }

    private static string Safe(string label)
        => string.Concat(label.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
}
