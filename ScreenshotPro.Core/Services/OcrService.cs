using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace ScreenshotPro.Core.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;
        private bool _disposed;

        public OcrService()
        {
            var tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            if (!Directory.Exists(tessDataPath))
            {
                throw new DirectoryNotFoundException($"Tesseract data directory not found at: {tessDataPath}");
            }

            _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
        }

        public string ExtractTextFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                using (var pix = Pix.LoadFromMemory(memoryStream.ToArray()))
                using (var page = _engine.Process(pix))
                {
                    return page.GetText();
                }
            }
        }

        public string ExtractTextFromImage(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));

            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Image file not found.", imagePath);

            using (var pix = Pix.LoadFromFile(imagePath))
            using (var page = _engine.Process(pix))
            {
                return page.GetText();
            }
        }

        public OcrResult ExtractTextWithConfidence(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Position = 0;

                using (var pix = Pix.LoadFromMemory(memoryStream.ToArray()))
                using (var page = _engine.Process(pix))
                {
                    return new OcrResult
                    {
                        Text = page.GetText(),
                        Confidence = page.GetMeanConfidence()
                    };
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _engine?.Dispose();
                }
                _disposed = true;
            }
        }
    }

    public class OcrResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}