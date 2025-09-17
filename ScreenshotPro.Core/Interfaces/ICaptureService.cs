using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Interfaces;

public interface ICaptureService
{
    Task<Screenshot> CaptureFullScreenAsync();
    Task<Screenshot> CaptureActiveWindowAsync();
    Task<Screenshot> CaptureRegionAsync(Models.Region region);
    Task<Screenshot> CaptureDelayedAsync(CaptureMode mode, int delaySeconds);
    Task<Screenshot> CaptureScrollingAsync(IntPtr windowHandle);
    Task<List<Screenshot>> CaptureMultiMonitorAsync();
    event EventHandler<CaptureEventArgs> CaptureCompleted;
}