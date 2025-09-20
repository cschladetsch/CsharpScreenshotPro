using Microsoft.UI.Xaml;
using ScreenshotPro.UI.Services;

namespace ScreenshotPro.UI.Views
{
    /// <summary>
    /// Base window class that automatically registers with WindowTracker
    /// </summary>
    public class TrackedWindow : Window
    {
        public TrackedWindow()
        {
            // Automatically register with window tracker when created
            WindowTracker.RegisterWindow(this);
        }
    }
}