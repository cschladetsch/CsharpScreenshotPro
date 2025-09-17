using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ScreenshotPro.Core.Interfaces;
using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Services;

public class WindowsCaptureService : ICaptureService
{
    public event EventHandler<CaptureEventArgs>? CaptureCompleted;

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    private const uint SRCCOPY = 0x00CC0020;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public async Task<Screenshot> CaptureFullScreenAsync()
    {
        return await Task.Run(() =>
        {
            var screenWidth = GetSystemMetrics(SM_CXSCREEN);
            var screenHeight = GetSystemMetrics(SM_CYSCREEN);

            var bitmap = CaptureScreen(0, 0, screenWidth, screenHeight);
            var screenshot = new Screenshot
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}",
                CapturedAt = DateTime.Now,
                Width = screenWidth,
                Height = screenHeight,
                CaptureMode = CaptureMode.FullScreen,
                FilePath = string.Empty,
                Tags = new List<string>(),
                Annotations = new List<Annotation>()
            };

            OnCaptureCompleted(new CaptureEventArgs(screenshot, true));
            return screenshot;
        });
    }

    public async Task<Screenshot> CaptureActiveWindowAsync()
    {
        return await Task.Run(() =>
        {
            // For simplicity, capture full screen for now
            // In a full implementation, you'd get the active window bounds
            return CaptureFullScreenAsync().Result;
        });
    }

    public async Task<Screenshot> CaptureRegionAsync(Models.Region region)
    {
        return await Task.Run(() =>
        {
            var bitmap = CaptureScreen(region.Left, region.Top, region.Size.Width, region.Size.Height);
            var screenshot = new Screenshot
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}",
                CapturedAt = DateTime.Now,
                Width = region.Size.Width,
                Height = region.Size.Height,
                CaptureMode = CaptureMode.Region,
                FilePath = string.Empty,
                Tags = new List<string>(),
                Annotations = new List<Annotation>()
            };

            OnCaptureCompleted(new CaptureEventArgs(screenshot, true));
            return screenshot;
        });
    }

    public async Task<Screenshot> CaptureDelayedAsync(CaptureMode mode, int delaySeconds)
    {
        await Task.Delay(delaySeconds * 1000);

        return mode switch
        {
            CaptureMode.FullScreen => await CaptureFullScreenAsync(),
            CaptureMode.ActiveWindow => await CaptureActiveWindowAsync(),
            _ => await CaptureFullScreenAsync()
        };
    }

    public async Task<Screenshot> CaptureScrollingAsync(IntPtr windowHandle)
    {
        // Placeholder implementation
        return await CaptureActiveWindowAsync();
    }

    public async Task<List<Screenshot>> CaptureMultiMonitorAsync()
    {
        // Placeholder implementation - just capture primary screen
        var screenshot = await CaptureFullScreenAsync();
        return new List<Screenshot> { screenshot };
    }

    public event EventHandler<RegionSelectedEventArgs>? RegionSelectionRequested;

    public async Task StartRegionCaptureAsync()
    {
        // Trigger region selection - the UI will handle showing the overlay
        var args = new RegionSelectedEventArgs();
        RegionSelectionRequested?.Invoke(this, args);
        await Task.CompletedTask;
    }

    public async Task<Bitmap?> CaptureRegionBitmapAsync(Models.Region region)
    {
        return await Task.Run(() =>
        {
            try
            {
                return CaptureScreen(region.Left, region.Top, region.Size.Width, region.Size.Height);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to capture region: {ex.Message}");
                return null;
            }
        });
    }

    private Bitmap CaptureScreen(int x, int y, int width, int height)
    {
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    protected virtual void OnCaptureCompleted(CaptureEventArgs e)
    {
        CaptureCompleted?.Invoke(this, e);
    }
}