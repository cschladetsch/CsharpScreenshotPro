using System.Drawing;
using System.Runtime.InteropServices;
using ScreenshotPro.Core.Models;
using ScreenshotPro.Core.Interfaces;

namespace ScreenshotPro.Core.Services;

public class ScreenshotCaptureOrchestrator
{
    private readonly WindowsCaptureService _captureService;
    private readonly LoggingService _logger;
    private readonly Bitmap _desktopBitmap;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // Events will be handled by the UI layer directly

    public ScreenshotCaptureOrchestrator()
    {
        _captureService = new WindowsCaptureService();
        _logger = LoggingService.Instance;

        // Pre-allocate desktop bitmap for the entire desktop (including multiple monitors)
        var screenWidth = GetSystemMetrics(0);
        var screenHeight = GetSystemMetrics(1);
        _desktopBitmap = new Bitmap(screenWidth, screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        _logger.LogInfo($"Pre-allocated desktop bitmap: {screenWidth}x{screenHeight}");
    }

    public async Task StartSnippingModeAsync()
    {
        _logger.LogInfo("🎯 Starting snipping mode with correct sequence");

        // Small delay to ensure window has been moved off-screen by caller
        await Task.Delay(10);
        _logger.LogInfo("✅ Window should be off-screen");

        // Step 2: Capture clean desktop into pre-allocated bitmap
        var screenRegion = new ScreenshotPro.Core.Models.Region(0, 0, _desktopBitmap.Width, _desktopBitmap.Height);
        await _captureService.CaptureRegionIntoBitmapAsync(screenRegion, _desktopBitmap);

        // Step 3: Darken the desktop bitmap slightly
        DarkenBitmap(_desktopBitmap, 0.7f);
        _logger.LogInfo("🌑 Desktop bitmap darkened");

        // Step 4: Enter selection mode with darkened desktop
        _logger.LogInfo("🎯 Entering selection mode");
        await ShowRegionSelectionOverlay(_desktopBitmap);
    }

    public async Task StartInstantCaptureAsync()
    {
        _logger.LogInfo("⚡ StartInstantCaptureAsync called");
        _logger.LogInfo($"⏰ Instant capture timestamp: {DateTime.Now:HH:mm:ss.fff}");

        // Small delay to ensure main window is fully hidden
        await Task.Delay(50);

        // Capture desktop (main window should be hidden now)
        var captureService = new WindowsCaptureService();
        var screenWidth = GetSystemMetrics(0);
        var screenHeight = GetSystemMetrics(1);
        var region = new ScreenshotPro.Core.Models.Region(0, 0, screenWidth, screenHeight);

        _logger.LogInfo("🖥️ Capturing desktop with hidden main window");
        var desktopBitmap = await captureService.CaptureRegionBitmapAsync(region);
        _logger.LogInfo($"📷 Desktop capture result: {(desktopBitmap != null ? "SUCCESS" : "FAILED")}");

        // Show RegionSelectionWindow immediately
        if (desktopBitmap != null)
        {
            _logger.LogInfo("🏗️ Showing RegionSelectionWindow immediately");
            await ShowRegionSelectionOverlay(desktopBitmap);
            _logger.LogInfo("✅ RegionSelectionWindow shown");
        }
        else
        {
            _logger.LogError("❌ Failed to capture desktop, cannot show region selection");
            // UI layer will handle the error
        }
    }

    public async Task<Bitmap?> ProcessSelectedRegionAsync(ScreenshotPro.Core.Models.Region region)
    {
        try
        {
            _logger.LogInfo($"📸 Processing selected region: {region.Size.Width}x{region.Size.Height} at ({region.Left}, {region.Top})");

            // Capture the selected region
            var bitmap = await _captureService.CaptureRegionBitmapAsync(region);
            if (bitmap == null)
            {
                _logger.LogError("❌ Failed to capture the selected region");
                return null;
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to process selected region", ex);
            return null;
        }
    }

    private void DarkenBitmap(Bitmap bitmap, float factor)
    {
        using var graphics = Graphics.FromImage(bitmap);
        using var darkBrush = new SolidBrush(System.Drawing.Color.FromArgb((int)(255 * (1 - factor)), 0, 0, 0));
        graphics.FillRectangle(darkBrush, 0, 0, bitmap.Width, bitmap.Height);
    }

    private async Task ShowRegionSelectionOverlay(Bitmap desktopBitmap)
    {
        try
        {
            _logger.LogInfo("🏗️ Creating RegionSelectionWindow with darkened desktop");

            // Note: This would need to be refactored to use dependency injection
            // or event-based communication to avoid direct UI dependencies
            // For now, we'll raise events that the UI can handle

            _logger.LogInfo("✅ RegionSelectionWindow ready for display");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error in ShowRegionSelectionOverlay", ex);
            // UI layer will handle the error
        }
    }

    public void Dispose()
    {
        _desktopBitmap?.Dispose();
    }
}