using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using ScreenshotPro.Core.Services;
using ScreenshotPro.Core.Interfaces;
using ScreenshotPro.Core.Models;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly WindowsCaptureService _captureService;
        private readonly SnippetStorageService _storageService;
        private readonly string _snippetsFolder;
        private readonly LoggingService _logger;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public ObservableCollection<ScreenshotItem> Screenshots { get; } = new();

        public LibraryPage()
        {
            InitializeComponent();
            _captureService = new WindowsCaptureService();
            _storageService = new SnippetStorageService();
            _logger = LoggingService.Instance;

            _logger.LogInfo("LibraryPage constructor started");

            // Set up Documents/Snippets folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _snippetsFolder = Path.Combine(documentsPath, "Snippets");

            // Create folder if it doesn't exist
            Directory.CreateDirectory(_snippetsFolder);

            // Load existing screenshots
            LoadScreenshots();

            _logger.LogInfo($"LibraryPage initialized. Log file: {_logger.GetLogFilePath()}");
        }

        private void NewCaptureButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Pre-capture desktop when user hovers over button to reduce delay later
            _ = Task.Run(async () =>
            {
                try
                {
                    var captureService = new WindowsCaptureService();
                    var screenWidth = GetSystemMetrics(0);
                    var screenHeight = GetSystemMetrics(1);
                    var region = new ScreenshotPro.Core.Models.Region(0, 0, screenWidth, screenHeight);
                    await captureService.CaptureRegionBitmapAsync(region);
                    _logger.LogInfo("🔄 Pre-captured desktop on hover");
                }
                catch
                {
                    // Ignore errors in pre-capture
                }
            });
        }

        private async void NewCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("🎯 NEW CAPTURE BUTTON CLICKED!");

            try
            {
                _logger.LogInfo("🚀 Starting SIMPLE capture...");
                await StartSimpleCaptureAsync();
                _logger.LogInfo("✅ Simple capture completed");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Capture failed", ex);
                RestoreMainWindow();
                await ShowErrorDialog("Capture Failed", $"An error occurred while capturing: {ex.Message}");
            }
        }

        private async Task StartSequentialAnimationAsync()
        {
            _logger.LogInfo("🎬 StartSequentialAnimationAsync called");

            var mainWindow = App.MainWindow;
            if (mainWindow == null) return;

            // Step 1: Capture desktop FIRST (with main window visible)
            var captureService = new WindowsCaptureService();
            var screenWidth = GetSystemMetrics(0);
            var screenHeight = GetSystemMetrics(1);
            var region = new ScreenshotPro.Core.Models.Region(0, 0, screenWidth, screenHeight);

            _logger.LogInfo("🖥️ Capturing desktop with main window visible");
            var desktopBitmap = await captureService.CaptureRegionBitmapAsync(region);
            _logger.LogInfo($"📷 Desktop capture result: {(desktopBitmap != null ? "SUCCESS" : "FAILED")}");

            // Step 2: Start fade out AND create RegionSelectionWindow simultaneously
            _logger.LogInfo("🚀 Starting fade out AND creating RegionSelectionWindow in parallel");

            var fadeOutTask = AnimateFadeOut(mainWindow, 200);

            var createWindowTask = Task.Run(() =>
            {
                _logger.LogInfo("🏗️ Creating RegionSelectionWindow during fade out");
                var regionWindow = new RegionSelectionWindow(desktopBitmap);
            regionWindow.SuppressOverlayForCapture();

                // Set up event handlers
                regionWindow.RegionSelected += async (sender, e) =>
                {
                    _logger.LogInfo("🎯 RegionSelected event received");
                    regionWindow.Close();
                    await ProcessSelectedRegion(e.SelectedRegion);
                };

                regionWindow.SelectionCancelled += (sender, e) =>
                {
                    _logger.LogInfo("❌ SelectionCancelled event received");
                    regionWindow.Close();
                    RestoreMainWindow();
                };

                _logger.LogInfo("✅ RegionSelectionWindow created during fade out");
                return regionWindow;
            });

            // Wait for fade out to complete
            await fadeOutTask;
            _logger.LogInfo("✅ Main window fade out COMPLETED");

            // Get the created window
            var regionWindow = await createWindowTask;
            _logger.LogInfo("✅ RegionSelectionWindow ready");

            // Step 3: Now fade in the overlay
            _logger.LogInfo("📈 Starting overlay fade in (200ms)");
            await AnimateFadeIn(regionWindow, 200);
            _logger.LogInfo("✅ Overlay fade in completed");
        }

        private async Task StartSimpleCaptureAsync()
        {
            _logger.LogInfo("🎯 StartSimpleCaptureAsync called");

            var mainWindow = App.MainWindow;
            if (mainWindow == null) return;

            var presenter = mainWindow.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter == null) return;

            var screenWidth = GetSystemMetrics(0);
            var screenHeight = GetSystemMetrics(1);
            var screenRegion = new ScreenshotPro.Core.Models.Region(0, 0, screenWidth, screenHeight);
            var desktopBitmap = new Bitmap(screenWidth, screenHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            _logger.LogInfo("🏗️ Pre-creating RegionSelectionWindow with shared desktop bitmap");
            var regionWindow = new RegionSelectionWindow(desktopBitmap);
            regionWindow.SuppressOverlayForCapture();

            regionWindow.Closed += (_, _) =>
            {
                desktopBitmap.Dispose();
                _logger.LogInfo("🧹 Disposed shared desktop bitmap after region window closed");
            };

            regionWindow.RegionSelected += async (sender, e) =>
            {
                _logger.LogInfo("✅ RegionSelected event received");
                regionWindow.Close();
                await ProcessSelectedRegion(e.SelectedRegion);
            };

            regionWindow.SelectionCancelled += (sender, e) =>
            {
                _logger.LogInfo("ℹ️ SelectionCancelled event received");
                regionWindow.Close();
                RestoreMainWindow();
            };

            _logger.LogInfo("✅ RegionSelectionWindow ready");

            var windowDeactivated = new TaskCompletionSource<bool>();

            void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs e)
            {
                _logger.LogInfo($"🪟 Window activation state: {e.WindowActivationState}");
                if (e.WindowActivationState == Microsoft.UI.Xaml.WindowActivationState.Deactivated)
                {
                    _logger.LogInfo("🪟 Window DEACTIVATED - hiding animation and showing overlay");
                    mainWindow.Content.Opacity = 0;
                    mainWindow.AppWindow.Hide();

                    _logger.LogInfo("⚡ Showing overlay immediately");
                    regionWindow.Activate();
                    _logger.LogInfo("⚡ Overlay shown instantly");

                    windowDeactivated.TrySetResult(true);
                }
            }

            mainWindow.Activated += OnWindowActivated;
            _logger.LogInfo("🌀 MINIMIZING main window");
            presenter.Minimize();
            _logger.LogInfo("🌀 Minimize called");

            _logger.LogInfo("⏳ Waiting for window deactivation and overlay display");
            await windowDeactivated.Task;
            mainWindow.Activated -= OnWindowActivated;
            _logger.LogInfo("✅ Window deactivated and overlay displayed");

            _logger.LogInfo("🔄 Ensuring main window fully hidden before capture");
            await WaitForWindowToBeHidden(mainWindow);

            regionWindow.SuppressOverlayForCapture();

            try
            {
                var captureService = new WindowsCaptureService();
                _logger.LogInfo("📸 Capturing clean desktop into shared bitmap");
                await captureService.CaptureRegionIntoBitmapAsync(screenRegion, desktopBitmap);
                regionWindow.RefreshDesktopBackground();
                _logger.LogInfo("✅ Desktop background refreshed in overlay");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Failed to capture clean desktop", ex);
            }
            finally
            {
                regionWindow.RestoreOverlayAfterCapture();
            }

        }

        private async Task WaitForWindowToBeHidden(Microsoft.UI.Xaml.Window window)
        {
            var maxWait = 1000; // Maximum 1 second
            var elapsed = 0;
            var checkInterval = 10; // Check every 10ms

            while (elapsed < maxWait)
            {
                // Check if window is actually visible on screen
                if (!window.Visible || window.Content.Opacity == 0)
                {
                    // Add small buffer to ensure window manager has processed the change
                    await Task.Delay(50);
                    _logger.LogInfo($"🔍 Window confirmed hidden after {elapsed + 50}ms");
                    return;
                }

                await Task.Delay(checkInterval);
                elapsed += checkInterval;
                _logger.LogInfo($"⏳ Still waiting for window to hide... {elapsed}ms");
            }

            _logger.LogInfo("⚠️ Timeout waiting for window to hide, proceeding anyway");
        }

        private async Task StartInstantCaptureAsync()
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
            _logger.LogInfo("🏗️ Showing RegionSelectionWindow immediately");
            await ShowRegionSelectionOverlay();
            _logger.LogInfo("✅ RegionSelectionWindow shown");
        }

        private async Task StartAnimatedSnippingAsync()
        {
            _logger.LogInfo("🎬 StartAnimatedSnippingAsync called");
            _logger.LogInfo($"⏰ Animated snipping timestamp: {DateTime.Now:HH:mm:ss.fff}");

            var mainWindow = App.MainWindow;
            if (mainWindow == null) return;

            // Capture desktop FIRST (while main window is still visible - this is what we want!)
            var captureService = new WindowsCaptureService();
            var screenWidth = GetSystemMetrics(0);
            var screenHeight = GetSystemMetrics(1);
            var region = new ScreenshotPro.Core.Models.Region(0, 0, screenWidth, screenHeight);

            _logger.LogInfo("🖥️ Capturing desktop with main window visible");
            var desktopBitmap = await captureService.CaptureRegionBitmapAsync(region);
            _logger.LogInfo($"📷 Desktop capture result: {(desktopBitmap != null ? "SUCCESS" : "FAILED")}");

            // Create RegionSelectionWindow but start invisible
            _logger.LogInfo("🏗️ Creating invisible RegionSelectionWindow");
            var regionWindow = new RegionSelectionWindow(desktopBitmap);

            // Set up event handlers
            regionWindow.RegionSelected += async (sender, e) =>
            {
                _logger.LogInfo("🎯 RegionSelected event received");
                regionWindow.Close();
                await ProcessSelectedRegion(e.SelectedRegion);
            };

            regionWindow.SelectionCancelled += (sender, e) =>
            {
                _logger.LogInfo("❌ SelectionCancelled event received");
                regionWindow.Close();
                RestoreMainWindow();
            };

            // Position and activate region window but keep it invisible initially
            regionWindow.Content.Opacity = 0;
            regionWindow.Activate();
            _logger.LogInfo("🏗️ RegionSelectionWindow activated but invisible");

            // Now start smooth crossfade: fade out main window while fading in overlay
            _logger.LogInfo("🎭 Starting smooth crossfade animation");
            var fadeOutTask = AnimateFadeOut(mainWindow, 150);
            var fadeInTask = AnimateFadeIn(regionWindow, 150);

            await Task.WhenAll(fadeOutTask, fadeInTask);
            _logger.LogInfo("✅ Smooth crossfade animation completed");
        }

        private async Task AnimateFadeOut(Microsoft.UI.Xaml.Window window, int durationMs)
        {
            _logger.LogInfo($"📉 Animating fade out over {durationMs}ms");

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / durationMs;
                var opacity = 1.0 - progress;

                window.Content.Opacity = Math.Max(0, opacity);
                await Task.Delay(16); // ~60fps
            }

            window.Content.Opacity = 0;
            window.AppWindow.Hide();

            var presenter = window.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                presenter.Minimize();
            }

            _logger.LogInfo("✅ Fade out animation completed");
        }

        private async Task AnimateFadeIn(RegionSelectionWindow regionWindow, int durationMs)
        {
            _logger.LogInfo($"📈 Animating fade in over {durationMs}ms");

            // Start with transparent window
            regionWindow.Content.Opacity = 0;
            regionWindow.Activate();

            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < durationMs)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var progress = elapsed / durationMs;

                regionWindow.Content.Opacity = progress;
                await Task.Delay(16); // ~60fps
            }

            regionWindow.Content.Opacity = 1.0;

            // Ensure proper focus after animation
            try
            {
                regionWindow.Activate();
                regionWindow.Content.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                _logger.LogInfo("✅ RegionSelectionWindow focus set after animation");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Failed to set focus after animation", ex);
            }

            _logger.LogInfo("✅ Fade in animation completed");
        }



        private Task ShowRegionSelectionOverlay(Bitmap? desktopBitmap = null)
        {
            try
            {
                _logger.LogInfo("🏗️ Creating RegionSelectionWindow");
                _logger.LogInfo($"⏰ Creating window timestamp: {DateTime.Now:HH:mm:ss.ffffff}");
                var regionWindow = new RegionSelectionWindow(desktopBitmap);
                _logger.LogInfo("✅ RegionSelectionWindow created");

                // Set up event handlers
                regionWindow.RegionSelected += async (sender, e) =>
                {
                    _logger.LogInfo("🎯 RegionSelected event received");
                    _logger.LogInfo($"⏰ RegionSelected timestamp: {DateTime.Now:HH:mm:ss.ffffff}");
                    regionWindow.Close();
                    await ProcessSelectedRegion(e.SelectedRegion);
                };

                regionWindow.SelectionCancelled += (sender, e) =>
                {
                    _logger.LogInfo("❌ SelectionCancelled event received");
                    _logger.LogInfo($"⏰ SelectionCancelled timestamp: {DateTime.Now:HH:mm:ss.ffffff}");
                    regionWindow.Close();
                    // Restore main window when cancelled
                    RestoreMainWindow();
                };

                // Show the region selection overlay
                _logger.LogInfo("🚀 Activating RegionSelectionWindow");
                _logger.LogInfo($"⏰ Pre-activate timestamp: {DateTime.Now:HH:mm:ss.ffffff}");
                regionWindow.Activate();
                _logger.LogInfo("✅ RegionSelectionWindow activated");
                _logger.LogInfo($"⏰ Post-activate timestamp: {DateTime.Now:HH:mm:ss.ffffff}");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error in ShowRegionSelectionOverlay", ex);
                RestoreMainWindow();
                return Task.CompletedTask;
            }
        }

        private async Task ProcessSelectedRegion(ScreenshotPro.Core.Models.Region region)
        {
            try
            {
                _logger.LogInfo($"📸 Processing selected region: {region.Size.Width}x{region.Size.Height} at ({region.Left}, {region.Top})");

                // Capture the selected region
                var bitmap = await _captureService.CaptureRegionBitmapAsync(region);
                if (bitmap == null)
                {
                    _logger.LogError("❌ Failed to capture the selected region");
                    RestoreMainWindow();
                    return;
                }

                // Auto-save the snippet without dialogs
                await SaveSnippet(bitmap);

                // Restore main window only after successful save
                RestoreMainWindow();
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Failed to process selected region", ex);
                RestoreMainWindow();
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            // Ensure main window is visible for error dialog
            RestoreMainWindow();

            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private async Task SaveSnippet(Bitmap bitmap)
        {
            try
            {
                var filePath = await _storageService.SaveSnippetAsync(bitmap);
                bitmap.Dispose();

                _logger.LogInfo($"✅ Snippet saved successfully to: {filePath}");

                // Refresh the screenshots list to show the new capture
                await RefreshScreenshots();
            }
            catch (Exception ex)
            {
                bitmap.Dispose();
                _logger.LogError($"❌ Failed to save snippet", ex);
            }
        }

        private void LoadScreenshots()
        {
            try
            {
                Screenshots.Clear();

                if (!Directory.Exists(_snippetsFolder))
                    return;

                var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
                var files = Directory.GetFiles(_snippetsFolder)
                    .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var item = new ScreenshotItem
                    {
                        FilePath = file,
                        FileName = Path.GetFileNameWithoutExtension(file),
                        CreatedDate = fileInfo.CreationTime,
                        ThumbnailPath = file // For now, use the original file as thumbnail
                    };

                    Screenshots.Add(item);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {Screenshots.Count} screenshots from {_snippetsFolder}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading screenshots: {ex.Message}");
            }
        }

        private async Task RefreshScreenshots()
        {
            await Task.Run(() =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadScreenshots();
                });
            });
        }

        private void RestoreMainWindow()
        {
            try
            {
                _logger.LogInfo("🔍 RESTORING main window visibility");
                _logger.LogInfo($"⏰ RestoreMainWindow called at: {DateTime.Now:HH:mm:ss.fff}");

                // Log the call stack to see where this is being called from
                var stackTrace = new System.Diagnostics.StackTrace();
                _logger.LogInfo($"📞 RestoreMainWindow called from: {stackTrace.GetFrame(1)?.GetMethod()?.Name}");

                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogInfo($"🏠 Window state before restore - Opacity: {mainWindow.Content.Opacity}");

                    // Restore opacity
                    mainWindow.Content.Opacity = 1.0;
                    _logger.LogInfo("🔧 Opacity restored to 1.0");

                    // Restore from minimized state if needed
                    var presenter = mainWindow.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                    if (presenter != null && presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
                    {
                        presenter.Restore();
                        _logger.LogInfo("🔧 Presenter.Restore() called");
                    }

                    // Show and activate the window
                    mainWindow.AppWindow.Show();
                    _logger.LogInfo("🔧 AppWindow.Show() called");
                    mainWindow.Activate();
                    _logger.LogInfo("🔧 Window.Activate() called");

                    _logger.LogInfo("✅ Main window restored and activated");
                    _logger.LogInfo($"⏰ RestoreMainWindow completed at: {DateTime.Now:HH:mm:ss.fff}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error restoring main window", ex);
            }
        }
    }

    public class ScreenshotItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ThumbnailPath { get; set; } = string.Empty;

        public string FormattedDate => CreatedDate.ToString("MMM dd, yyyy h:mm tt");
    }
}






