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
                // Hide the main window
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.AppWindow.Hide();
                }

                // Small delay to ensure window is hidden
                await Task.Delay(500);

                // Show a simple dialog asking for user confirmation to proceed
                var confirmDialog = new ContentDialog()
                {
                    Title = "Start Snipping",
                    Content = "This will capture a region of your screen. Click OK to proceed with capturing a 800x600 region from the center of your screen, or Cancel to abort.",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary
                };

                // Show main window temporarily to display dialog
                if (mainWindow != null)
                {
                    mainWindow.AppWindow.Show();
                }

                confirmDialog.XamlRoot = this.XamlRoot;
                var confirmResult = await confirmDialog.ShowAsync();

                if (confirmResult == ContentDialogResult.Primary)
                {
                    // Hide window again for capture
                    if (mainWindow != null)
                    {
                        mainWindow.AppWindow.Hide();
                    }

                    await Task.Delay(300);

                    // Capture a fixed region (center of screen)
                    var region = new ScreenshotPro.Core.Models.Region(300, 200, 800, 600);
                    var bitmap = await _captureService.CaptureRegionBitmapAsync(region);

                    if (bitmap != null)
                    {
                        // Show main window again
                        if (mainWindow != null)
                        {
                            mainWindow.AppWindow.Show();
                        }

                        // Show confirmation dialog
                        var saveDialog = new ContentDialog()
                        {
                            Title = "Snippet Captured",
                            Content = "Your snippet has been captured! Would you like to save it to the Snippets folder?",
                            PrimaryButtonText = "Keep",
                            SecondaryButtonText = "Try Again",
                            CloseButtonText = "Cancel",
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
                                NewCaptureButton_Click(sender, e); // Restart the process
                                return;

                            default: // Cancel
                                bitmap.Dispose();
                                break;
                        }
                    }
                }

                // Ensure main window is shown again
                if (mainWindow != null)
                {
                    mainWindow.AppWindow.Show();
                    mainWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Capture failed: {ex.Message}");

                // Show error dialog
                var dialog = new ContentDialog()
                {
                    Title = "Capture Failed",
                    Content = $"An error occurred while capturing: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();

                // Ensure main window is shown again
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.AppWindow.Show();
                    mainWindow.Activate();
                }
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
