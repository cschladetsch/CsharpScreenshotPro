using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using ScreenshotPro.Core.Services;
using ScreenshotPro.Core.Models;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using ScreenshotPro.UI.ViewModels;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using ScreenshotProRegion = ScreenshotPro.Core.Models.Region;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly ScreenshotCaptureOrchestrator _captureOrchestrator;
        private readonly LibraryPageViewModel _viewModel;
        private readonly LoggingService _logger;
        private Windows.Graphics.PointInt32 _originalWindowPosition;
        private OcrService? _ocrService;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public LibraryPage()
        {
            InitializeComponent();
            _captureOrchestrator = new ScreenshotCaptureOrchestrator();
            _viewModel = new LibraryPageViewModel();
            _logger = LoggingService.Instance;

            _logger.LogInfo("LibraryPage constructor started");

            // Event handlers will be set up when creating region selection window

            // Bind ViewModel
            DataContext = _viewModel;

            // Load existing screenshots
            _ = LoadScreenshotsAsync();

            _logger.LogInfo($"LibraryPage initialized. Log file: {_logger.GetLogFilePath()}");
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _captureOrchestrator?.Dispose();
        }

        private async Task LoadScreenshotsAsync()
        {
            await _viewModel.LoadScreenshotsAsync();
        }


        private void NewCaptureButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // No longer needed since we pre-allocate the desktop bitmap
        }

        private async void NewCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("🎯 NEW CAPTURE BUTTON CLICKED!");

            try
            {
                // Store original position and move window off-screen before starting capture
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    var appWindow = mainWindow.AppWindow;
                    _originalWindowPosition = appWindow.Position;
                    appWindow.Move(new Windows.Graphics.PointInt32(-5000, -5000));
                }

                // Prepare the desktop bitmaps for all displays
                var displayBitmaps = await _captureOrchestrator.PrepareDesktopBitmapsAsync();

                // Create and show the region selection window for all displays
                await ShowRegionSelectionWindow(displayBitmaps);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Capture failed", ex);
                RestoreMainWindow();
                await ShowErrorDialog("Capture Failed", $"An error occurred while capturing: {ex.Message}");
            }
        }

        private async Task ShowRegionSelectionWindow(List<(ScreenshotCaptureOrchestrator.DisplayInfo Display, Bitmap Bitmap)> displayBitmaps)
        {
            try
            {
                _logger.LogInfo($"🏗️ Creating {displayBitmaps.Count} RegionSelectionWindows for each display");
                var regionWindows = new List<RegionSelectionWindow>();

                foreach (var (display, bitmap) in displayBitmaps)
                {
                    _logger.LogInfo($"🖥️ Creating RegionSelectionWindow for display {display.Index}: {display.Bounds}");
                    _logger.LogInfo($"🖥️ Bitmap dimensions for display {display.Index}: {bitmap.Width}x{bitmap.Height}");

                    var regionWindow = new RegionSelectionWindow();

                    // Position and size the window exactly for this display FIRST
                    var appWindow = regionWindow.AppWindow;
                    if (appWindow != null)
                    {
                        // Log DPI information
                        _logger.LogInfo($"🖥️ Display {display.Index} DPI: {display.DpiX}x{display.DpiY}, Scale: {display.ScaleFactor:F2}x");

                        // Use Overlapped presenter with manual positioning
                        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);

                        var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                        if (presenter != null)
                        {
                            presenter.SetBorderAndTitleBar(false, false);
                            presenter.IsAlwaysOnTop = true;
                            presenter.IsResizable = false;
                        }

                        // Position and size the window to cover the entire display
                        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                            display.Bounds.X,
                            display.Bounds.Y,
                            display.Bounds.Width,
                            display.Bounds.Height));

                        _logger.LogInfo($"🖥️ Window positioned at ({display.Bounds.X}, {display.Bounds.Y}) with size {display.Bounds.Width}x{display.Bounds.Height}");

                        // Force a refresh after positioning
                        await Task.Delay(10);

                        _logger.LogInfo($"🖥️ Actual window position after MoveAndResize: ({appWindow.Position.X}, {appWindow.Position.Y}), size: {appWindow.Size.Width}x{appWindow.Size.Height}");
                    }

                    // THEN bind the correctly-sized bitmap
                    regionWindow.BindDesktopBitmap(bitmap);
                    _logger.LogInfo($"🖥️ Bound {bitmap.Width}x{bitmap.Height} bitmap to window for display {display.Index}");

                    // Set up event handlers
                    regionWindow.RegionSelected += async (sender, e) =>
                    {
                        _logger.LogInfo($"🎯 RegionSelected event received from display {display.Index}");

                        // Close all windows
                        foreach (var window in regionWindows)
                        {
                            window.Close();
                        }

                        // Convert region coordinates to global coordinates (no DPI scaling needed since Stretch="None")
                        var globalRegion = new ScreenshotProRegion(
                            e.SelectedRegion.Left + display.Bounds.X,
                            e.SelectedRegion.Top + display.Bounds.Y,
                            e.SelectedRegion.Size.Width,
                            e.SelectedRegion.Size.Height);

                        _logger.LogInfo($"🎯 Selected region: {e.SelectedRegion.Left},{e.SelectedRegion.Top} {e.SelectedRegion.Size.Width}x{e.SelectedRegion.Size.Height}");
                        _logger.LogInfo($"🎯 Display {display.Index} bounds: {display.Bounds.X},{display.Bounds.Y} {display.Bounds.Width}x{display.Bounds.Height}");
                        _logger.LogInfo($"🎯 Global region: {globalRegion.Left},{globalRegion.Top} {globalRegion.Size.Width}x{globalRegion.Size.Height}");

                        await ProcessSelectedRegion(globalRegion);
                    };

                    regionWindow.SelectionCancelled += (sender, e) =>
                    {
                        _logger.LogInfo($"❌ SelectionCancelled event received from display {display.Index}");

                        // Close all windows
                        foreach (var window in regionWindows)
                        {
                            window.Close();
                        }

                        RestoreMainWindow();
                    };

                    regionWindows.Add(regionWindow);
                }

                // Activate all windows
                foreach (var window in regionWindows)
                {
                    window.Activate();
                    _logger.LogInfo("✅ RegionSelectionWindow activated for display");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error in ShowRegionSelectionWindow", ex);
                RestoreMainWindow();
            }
        }

        private async Task ProcessSelectedRegion(ScreenshotProRegion region)
        {
            try
            {
                var bitmap = await _captureOrchestrator.ProcessSelectedRegionAsync(region);
                if (bitmap != null)
                {
                    // Extract text using OCR
                    try
                    {
                        if (_ocrService == null)
                        {
                            _ocrService = new OcrService();
                        }

                        _logger.LogInfo($"🔄 Running automatic OCR on captured screenshot...");
                        var ocrResult = _ocrService.ExtractTextWithConfidence(bitmap);
                        if (!string.IsNullOrWhiteSpace(ocrResult.Text))
                        {
                            // Copy extracted text to clipboard
                            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                            dataPackage.SetText(ocrResult.Text);
                            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                            _logger.LogInfo($"📋 Automatic OCR: {ocrResult.Text.Length} characters copied to clipboard (Confidence: {ocrResult.Confidence:F1}%)");
                            _logger.LogInfo($"📝 Auto-OCR preview: {ocrResult.Text.Substring(0, Math.Min(100, ocrResult.Text.Length))}...");
                        }
                        else
                        {
                            _logger.LogInfo("📋 Automatic OCR: No text found in screenshot");
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        _logger.LogError("OCR failed", ocrEx);
                    }

                    await _viewModel.SaveSnippetAsync(bitmap);
                    bitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Failed to process selected region", ex);
            }
            finally
            {
                RestoreMainWindow();
            }
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("📂 OPEN FOLDER BUTTON CLICKED!");
            await _viewModel.OpenSnippetsFolderAsync();
        }

        private void SelectionCircle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is ScreenshotItem item)
            {
                _viewModel.ToggleItemSelection(item);
                e.Handled = true; // Prevent bubble up to Border tap
            }
        }

        private void SnippetImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is ScreenshotItem item)
            {
                // Only open viewer if not handling selection circle tap
                if (!e.Handled)
                {
                    _logger.LogInfo($"📸 Opening snippet viewer for: {item.FileName}");
                    var viewerWindow = new SnippetViewerWindow(item.FilePath);
                    viewerWindow.Activate();
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("🗑️ DELETE BUTTON CLICKED!");
            await _viewModel.DeleteSelectedItemsAsync();
        }

        private void OpenInPaintButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("🎨 OPEN IN PAINT BUTTON CLICKED!");

            var selectedItem = _viewModel.Screenshots.FirstOrDefault(s => s.IsSelected);
            if (selectedItem != null)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mspaint.exe",
                        Arguments = $"\"{selectedItem.FilePath}\"",
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                    _logger.LogInfo($"🎨 Opened {selectedItem.FileName} in Paint");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to open Paint", ex);
                }
            }
        }

        private async void ExtractTextButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("📝 EXTRACT TEXT BUTTON CLICKED!");

            var selectedItem = _viewModel.Screenshots.FirstOrDefault(s => s.IsSelected);
            if (selectedItem != null)
            {
                try
                {
                    if (_ocrService == null)
                    {
                        _ocrService = new OcrService();
                    }

                    _logger.LogInfo($"🖼️ Loading image: {selectedItem.FilePath}");

                    // Verify file exists
                    if (!System.IO.File.Exists(selectedItem.FilePath))
                    {
                        _logger.LogError($"❌ Image file not found: {selectedItem.FilePath}");
                        await ShowOcrNotificationAsync($"Image file not found: {selectedItem.FilePath}");
                        return;
                    }

                    // Load the image from file
                    using (var bitmap = new System.Drawing.Bitmap(selectedItem.FilePath))
                    {
                        _logger.LogInfo($"🖼️ LibraryPage - Image loaded: {bitmap.Width}x{bitmap.Height} pixels, Format: {bitmap.PixelFormat}");
                        _logger.LogInfo($"🔍 LibraryPage - Processing image from: {selectedItem.FilePath}");

                        // Save a debug copy to verify we're loading the right image
                        var debugDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "OCR_Debug");
                        Directory.CreateDirectory(debugDir);
                        var debugPath = System.IO.Path.Combine(debugDir, $"DEBUG_LIBRARY_{DateTime.Now:yyyyMMdd_HHmmss}_{selectedItem.FileName}");
                        bitmap.Save(debugPath);
                        _logger.LogInfo($"🔍 LibraryPage Debug image saved to: {debugPath}");

                        // Also log the actual pixel data hash to verify we're processing different images
                        var pixelHash = GetImageHash(bitmap);
                        _logger.LogInfo($"🔍 LibraryPage Image pixel hash: {pixelHash}");

                        var ocrResult = _ocrService.ExtractTextWithConfidence(bitmap);
                        _logger.LogInfo($"📝 LibraryPage OCR RAW RESULT: '{ocrResult.Text}' (Length: {ocrResult.Text.Length}, Confidence: {ocrResult.Confidence:F1}%)");

                        if (!string.IsNullOrWhiteSpace(ocrResult.Text))
                        {
                            // Log the full text for debugging
                            _logger.LogInfo($"📝 LibraryPage OCR extracted {ocrResult.Text.Length} characters from {selectedItem.FileName}: {ocrResult.Text.Substring(0, Math.Min(100, ocrResult.Text.Length))}...");

                            // Copy extracted text to clipboard
                            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                            dataPackage.SetText(ocrResult.Text);
                            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                            _logger.LogInfo($"📋 LibraryPage OCR Text copied to clipboard from {selectedItem.FileName} (Confidence: {ocrResult.Confidence:F1}%)");

                            // Show the extracted text in a new window
                            ShowOcrResultWindow(ocrResult.Text, ocrResult.Confidence, selectedItem.FileName);
                        }
                        else
                        {
                            _logger.LogInfo("No text found in image");
                            await ShowOcrNotificationAsync("No text found in the selected image.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to extract text", ex);
                    await ShowOcrNotificationAsync($"Failed to extract text: {ex.Message}");
                }
            }
        }

        private async Task ShowOcrNotificationAsync(string message)
        {
            // Create a simple content dialog to show the OCR result
            var dialog = new ContentDialog
            {
                Title = "OCR Result",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private string GetImageHash(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(ms);
                    return Convert.ToHexString(hash)[..16]; // First 16 chars for brevity
                }
            }
        }

        private void ShowOcrResultWindow(string extractedText, float confidence, string fileName = "")
        {
            var ocrWindow = new OcrResultWindow(extractedText, confidence, fileName);
            ocrWindow.Activate();
        }

        private void FileName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _logger.LogInfo("🖱️ FileName_Tapped event fired!");

            if (sender is TextBlock textBlock)
            {
                _logger.LogInfo($"🖱️ Sender is TextBlock with text: '{textBlock.Text}'");

                if (textBlock.Tag is ScreenshotItem item)
                {
                    _logger.LogInfo($"🖱️ Tag is ScreenshotItem: {item.FileName}, IsEditing: {item.IsEditing}");
                    _logger.LogInfo("🖱️ Calling _viewModel.StartEditingItem...");
                    _viewModel.StartEditingItem(item);
                    _logger.LogInfo($"🖱️ After StartEditingItem, IsEditing: {item.IsEditing}");
                }
                else
                {
                    _logger.LogError($"🖱️ Tag is not ScreenshotItem, it's: {textBlock.Tag?.GetType()}", null);
                }
            }
            else
            {
                _logger.LogError($"🖱️ Sender is not TextBlock, it's: {sender?.GetType()}", null);
            }
        }

        private async void FileNameEdit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is ScreenshotItem item)
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    await _viewModel.SaveItemNameAsync(item, textBox.Text);
                    // Remove focus to prevent LostFocus from firing
                    this.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    _viewModel.CancelEditingItem(item);
                    // Remove focus to prevent LostFocus from firing
                    this.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                    e.Handled = true;
                }
            }
        }

        private async void FileNameEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is ScreenshotItem item)
            {
                // Only save if we're still in editing mode (Enter/Escape would have set IsEditing to false)
                if (item.IsEditing)
                {
                    await _viewModel.SaveItemNameAsync(item, textBox.Text);
                }
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

        private void RestoreMainWindow()
        {
            try
            {
                _logger.LogInfo("🔍 RESTORING main window visibility");
                _logger.LogInfo($"⏰ RestoreMainWindow called at: {DateTime.Now:HH:mm:ss.fff}");

                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    // Restore window to original position
                    mainWindow.AppWindow.Move(_originalWindowPosition);
                    _logger.LogInfo($"🔧 Window restored to original position: ({_originalWindowPosition.X}, {_originalWindowPosition.Y})");

                    // Show and activate the window
                    mainWindow.AppWindow.Show();
                    _logger.LogInfo("🔧 AppWindow.Show() called");
                    mainWindow.Activate();
                    _logger.LogInfo("🔧 Window.Activate() called");

                    _logger.LogInfo("✅ Main window restored and activated");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error restoring main window", ex);
            }
        }
    }

    public class ScreenshotItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isEditing;
        private string _fileName = string.Empty;

        public string FilePath { get; set; } = string.Empty;
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
                }
            }
        }
        public DateTime CreatedDate { get; set; }
        public string ThumbnailPath { get; set; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditing)));
                }
            }
        }

        public string FormattedDate => CreatedDate.ToString("MMM dd, yyyy h:mm tt");

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class BoolToSelectionBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 43, 108, 176)); // Blue fill when selected
            }
            return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)); // Transparent when not selected
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isVisible = value is bool b && b;

            // Check if we should invert the logic
            if (parameter?.ToString() == "Invert")
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}