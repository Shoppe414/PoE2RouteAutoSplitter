using System.Drawing;
using TesseractOCR;
using TesseractOCR.Enums;

namespace PoE2BossWatcher;

public sealed class OcrService : IDisposable
{
    private readonly Engine _engine;
    public GameLanguageInfo Language { get; }

    public OcrService(string tessdataParent, string gameLanguage)
    {
        Language = GameLanguageCatalog.Resolve(gameLanguage);
        var full = Path.GetFullPath(tessdataParent);
        var tessdataPath = Path.Combine(full, "tessdata");
        var trainedData = Path.Combine(tessdataPath, Language.TesseractCode + ".traineddata");
        if (!File.Exists(trainedData))
            throw new FileNotFoundException(
                $"Missing tessdata\\{Language.TesseractCode}.traineddata for PoE2 game language {Language.DisplayName}. Run Setup-OCR.ps1 first.",
                trainedData);

        // TesseractOCR supports language-code strings. Keep the PoE2/UI language code
        // separate from the Tesseract model code so the rest of the runtime stays canonical.
        _engine = new Engine(tessdataPath, Language.TesseractCode, EngineMode.Default);
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
