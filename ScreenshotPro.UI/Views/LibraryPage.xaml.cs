using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using ScreenshotPro.Core.Services;
using ScreenshotPro.Core.Interfaces;
using ScreenshotPro.Core.Models;
using System.Drawing;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly WindowsCaptureService _captureService;
        private readonly SnippetStorageService _storageService;

        public LibraryPage()
        {
            InitializeComponent();
            _captureService = new WindowsCaptureService();
            _storageService = new SnippetStorageService();
        }

        private async void NewCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await StartEnhancedSnippingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture failed: {ex.Message}");
                await ShowErrorDialog("Capture Failed", $"An error occurred while capturing: {ex.Message}");
            }
        }

        private async Task StartEnhancedSnippingAsync()
        {
            // Hide the main window
            var mainWindow = App.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AppWindow.Hide();
            }

            // Small delay to ensure window is hidden
            await Task.Delay(500);

            // Show region selection dialog (Windows Snipping Tool style)
            await ShowSnippingOptionsDialog();
        }

        private async Task ShowSnippingOptionsDialog()
        {
            // Show main window temporarily to display dialog
            var mainWindow = App.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AppWindow.Show();
            }

            var optionsDialog = new ContentDialog()
            {
                Title = "Select Capture Region",
                Content = "Choose how you want to capture your screen:",
                PrimaryButtonText = "Small Region (400×300)",
                SecondaryButtonText = "Medium Region (800×600)",
                CloseButtonText = "Large Region (1200×800)",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await optionsDialog.ShowAsync();

            // Hide window for capture
            if (mainWindow != null)
            {
                mainWindow.AppWindow.Hide();
            }

            await Task.Delay(300);

            ScreenshotPro.Core.Models.Region captureRegion;

            // Determine region based on user choice
            switch (result)
            {
                case ContentDialogResult.Primary: // Small
                    captureRegion = new ScreenshotPro.Core.Models.Region(400, 300, 400, 300);
                    break;
                case ContentDialogResult.Secondary: // Medium
                    captureRegion = new ScreenshotPro.Core.Models.Region(300, 200, 800, 600);
                    break;
                default: // Large
                    captureRegion = new ScreenshotPro.Core.Models.Region(200, 100, 1200, 800);
                    break;
            }

            await ProcessSelectedRegion(captureRegion);
        }

        private async Task ProcessSelectedRegion(ScreenshotPro.Core.Models.Region region)
        {
            try
            {
                // Capture the selected region
                var bitmap = await _captureService.CaptureRegionBitmapAsync(region);
                if (bitmap == null)
                {
                    await ShowErrorDialog("Capture Failed", "Failed to capture the selected region.");
                    return;
                }

                // Show main window first so dialog has a parent
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.AppWindow.Show();
                }

                // Show confirmation dialog with region info
                var saveDialog = new ContentDialog()
                {
                    Title = "Snippet Captured Successfully!",
                    Content = $"📸 Captured region: {region.Size.Width} × {region.Size.Height} pixels\n📍 Position: ({region.Left}, {region.Top})\n\n💾 Save this snippet to your Snippets folder?",
                    PrimaryButtonText = "✅ Keep & Save",
                    SecondaryButtonText = "🔄 Try Different Size",
                    CloseButtonText = "❌ Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var saveResult = await saveDialog.ShowAsync();

                switch (saveResult)
                {
                    case ContentDialogResult.Primary: // Keep
                        await SaveSnippet(bitmap);
                        break;

                    case ContentDialogResult.Secondary: // Try Again
                        bitmap.Dispose();
                        await StartEnhancedSnippingAsync(); // Restart process
                        break;

                    default: // Cancel
                        bitmap.Dispose();
                        break;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Processing Failed", $"Failed to process selected region: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            // Ensure main window is visible for error dialog
            var mainWindow = App.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.AppWindow.Show();
            }

            var dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();

            // Keep main window visible
            if (mainWindow != null)
            {
                mainWindow.Activate();
            }
        }

        private async Task SaveSnippet(Bitmap bitmap)
        {
            try
            {
                var filePath = await _storageService.SaveSnippetAsync(bitmap);
                bitmap.Dispose();

                // Show success message
                var dialog = new ContentDialog()
                {
                    Title = "Snippet Saved",
                    Content = $"Your snippet has been saved to:\n{filePath}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                bitmap.Dispose();

                var dialog = new ContentDialog()
                {
                    Title = "Save Failed",
                    Content = $"Failed to save snippet: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }
    }
}
