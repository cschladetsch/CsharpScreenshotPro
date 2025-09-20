using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace ScreenshotPro.UI.Services
{
    public static class WindowTracker
    {
        private static readonly List<Window> OpenWindows = new();
        private static readonly object Lock = new();

        public static void RegisterWindow(Window window)
        {
            lock (Lock)
            {
                if (!OpenWindows.Contains(window))
                {
                    OpenWindows.Add(window);
                    window.Closed += OnWindowClosed;
                }
            }
        }

        private static void OnWindowClosed(object sender, WindowEventArgs e)
        {
            lock (Lock)
            {
                if (sender is Window window)
                {
                    window.Closed -= OnWindowClosed;
                    OpenWindows.Remove(window);
                }
            }
        }

        public static void CloseAllWindows()
        {
            lock (Lock)
            {
                var windowsToClose = new List<Window>(OpenWindows);
                foreach (var window in windowsToClose)
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // Window might already be closed or closing
                    }
                }
                OpenWindows.Clear();
            }
        }
    }
}