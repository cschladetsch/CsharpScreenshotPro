using System.Drawing;
using System.Runtime.InteropServices;
using ScreenshotPro.Core.Models;
using ScreenshotPro.Core.Interfaces;

namespace ScreenshotPro.Core.Services;

public class ScreenshotCaptureOrchestrator
{
    private readonly WindowsCaptureService _captureService;
    private readonly LoggingService _logger;
    private readonly List<DisplayInfo> _displays;
    private readonly Dictionary<int, Bitmap> _displayBitmaps;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hmon, ref MONITORINFO lpmi);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    private const int MDT_EFFECTIVE_DPI = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public class DisplayInfo
    {
        public int Index { get; set; }
        public Rectangle Bounds { get; set; }
        public bool IsPrimary { get; set; }
        public uint DpiX { get; set; }
        public uint DpiY { get; set; }
        public double ScaleFactor => DpiX / 96.0; // 96 DPI is 100% scaling
    }

    public ScreenshotCaptureOrchestrator()
    {
        _captureService = new WindowsCaptureService();
        _logger = LoggingService.Instance;
        _displays = new List<DisplayInfo>();
        _displayBitmaps = new Dictionary<int, Bitmap>();

        DetectDisplays();
        PreAllocateDisplayBitmaps();
    }

    private void DetectDisplays()
    {
        _displays.Clear();
        int displayIndex = 0;

        bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                var bounds = new Rectangle(
                    monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Top,
                    monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top
                );

                var isPrimary = (monitorInfo.dwFlags & 1) == 1; // MONITORINFOF_PRIMARY = 1

                // Get DPI for this monitor
                uint dpiX = 96, dpiY = 96; // Default to 96 DPI
                try
                {
                    GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"⚠️ Could not get DPI for monitor {displayIndex}, using default 96 DPI: {ex.Message}");
                }

                var display = new DisplayInfo
                {
                    Index = displayIndex++,
                    Bounds = bounds,
                    IsPrimary = isPrimary,
                    DpiX = dpiX,
                    DpiY = dpiY
                };

                _displays.Add(display);
                _logger.LogInfo($"🖥️ Detected display {display.Index}: {bounds.Width}x{bounds.Height} at ({bounds.X},{bounds.Y}) " +
                               $"DPI: {dpiX}x{dpiY} (Scale: {display.ScaleFactor:F2}x){(isPrimary ? " [PRIMARY]" : "")}");
            }

            return true; // Continue enumeration
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);
        _logger.LogInfo($"🖥️ Total displays detected: {_displays.Count}");
    }

    private void PreAllocateDisplayBitmaps()
    {
        foreach (var display in _displays)
        {
            _logger.LogInfo($"🖥️ Creating bitmap for display {display.Index}: Target size = {display.Bounds.Width}x{display.Bounds.Height}");
            var bitmap = new Bitmap(display.Bounds.Width, display.Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _logger.LogInfo($"🖥️ Created bitmap actual size: {bitmap.Width}x{bitmap.Height}");

            _displayBitmaps[display.Index] = bitmap;
            _logger.LogInfo($"🖥️ Stored bitmap for display {display.Index} in dictionary");

            // Verify what's in the dictionary
            var storedBitmap = _displayBitmaps[display.Index];
            _logger.LogInfo($"🖥️ Verification - bitmap in dictionary for display {display.Index}: {storedBitmap.Width}x{storedBitmap.Height}");
        }
    }

    public async Task<List<(DisplayInfo Display, Bitmap Bitmap)>> PrepareDesktopBitmapsAsync()
    {
        _logger.LogInfo("🎯 Preparing multi-monitor desktop bitmaps for selection");

        // Small delay to ensure window has been moved off-screen by caller
        await Task.Delay(10);
        _logger.LogInfo("✅ Window should be off-screen");

        // Step 1: Capture each display synchronously into its pre-allocated bitmap
        var captureTasks = new List<Task>();

        foreach (var display in _displays)
        {
            var displayCopy = display; // Capture loop variable
            var task = Task.Run(async () =>
            {
                _logger.LogInfo($"🖥️ Task for display {displayCopy.Index}: Getting bitmap from dictionary");
                var bitmap = _displayBitmaps[displayCopy.Index];
                _logger.LogInfo($"🖥️ Retrieved bitmap for display {displayCopy.Index}: {bitmap.Width}x{bitmap.Height} (Expected: {displayCopy.Bounds.Width}x{displayCopy.Bounds.Height})");

                var region = new ScreenshotPro.Core.Models.Region(
                    displayCopy.Bounds.X,
                    displayCopy.Bounds.Y,
                    displayCopy.Bounds.Width,
                    displayCopy.Bounds.Height);

                _logger.LogInfo($"🖥️ Capturing display {displayCopy.Index}: Region = {region.Size.Width}x{region.Size.Height} at ({region.Left},{region.Top})");
                _logger.LogInfo($"🖥️ Pre-allocated bitmap size for display {displayCopy.Index}: {bitmap.Width}x{bitmap.Height}");
                await _captureService.CaptureRegionIntoBitmapAsync(region, bitmap);
                _logger.LogInfo($"🖥️ After capture, bitmap size for display {displayCopy.Index}: {bitmap.Width}x{bitmap.Height}");

                // Step 2: Darken this display's bitmap
                DarkenBitmap(bitmap, 0.7f);
                _logger.LogInfo($"✅ Display {displayCopy.Index} captured and darkened");
            });
            captureTasks.Add(task);
        }

        // Wait for all displays to be captured synchronously
        await Task.WhenAll(captureTasks);
        _logger.LogInfo("🖥️ All display bitmaps prepared synchronously");

        // Return list of display info paired with their bitmaps
        var result = new List<(DisplayInfo Display, Bitmap Bitmap)>();
        foreach (var display in _displays)
        {
            var bitmap = _displayBitmaps[display.Index];
            _logger.LogInfo($"🖥️ Returning bitmap for display {display.Index}: {bitmap.Width}x{bitmap.Height} (Display bounds: {display.Bounds.Width}x{display.Bounds.Height})");
            result.Add((display, bitmap));
        }

        return result;
    }

    public async Task<Bitmap?> StartInstantCaptureAsync()
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

        // Return the bitmap for UI layer to handle
        if (desktopBitmap != null)
        {
            _logger.LogInfo("✅ Desktop bitmap captured successfully");
            return desktopBitmap;
        }
        else
        {
            _logger.LogError("❌ Failed to capture desktop, cannot show region selection");
            return null;
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


    public void Dispose()
    {
        foreach (var bitmap in _displayBitmaps.Values)
        {
            bitmap?.Dispose();
        }
        _displayBitmaps.Clear();
    }
}