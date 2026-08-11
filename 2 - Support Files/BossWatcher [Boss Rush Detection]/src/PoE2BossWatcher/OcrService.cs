using System.Drawing;
using TesseractOCR;
using TesseractOCR.Enums;

namespace PoE2BossWatcher;

public sealed class OcrService : IDisposable
{
    private readonly Engine _engine;

    public OcrService(string tessdataParent)
    {
        var full = Path.GetFullPath(tessdataParent);
        var tessdataPath = Path.Combine(full, "tessdata");
        var trainedData = Path.Combine(tessdataPath, "eng.traineddata");
        if (!File.Exists(trainedData))
            throw new FileNotFoundException("Missing tessdata\\eng.traineddata. Run Setup-OCR.ps1 first.", trainedData);

        // TesseractOCR expects the directory containing the traineddata files.
        _engine = new Engine(tessdataPath, Language.English, EngineMode.Default);
    }

    public OcrRead ReadSingleLine(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        using var pix = TesseractOCR.Pix.Image.LoadFromMemory(ms.ToArray());
        using var page = _engine.Process(pix, PageSegMode.SingleLine);
        return new OcrRead((page.Text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim(), page.MeanConfidence);
    }

    public void Dispose() => _engine.Dispose();
}

public sealed record OcrRead(string Text, float Confidence);
