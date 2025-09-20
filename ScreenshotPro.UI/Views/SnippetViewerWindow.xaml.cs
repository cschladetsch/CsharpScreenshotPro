using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenshotPro.Core.Services;
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class SnippetViewerWindow : Window
    {
        private string _filePath;
        private readonly LoggingService _logger;
        private readonly SnippetFileEditor _fileEditor;
        private OcrService _ocrService;

        public SnippetViewerWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            _logger = LoggingService.Instance;
            _fileEditor = new SnippetFileEditor(new SnippetStorageService());
            _ocrService = new OcrService();

            // Set window title
            Title = Path.GetFileName(filePath);

            // Configure window with dark title bar
            var appWindow = AppWindow;
            if (appWindow != null)
            {
                // Enable dark title bar
                appWindow.TitleBar.BackgroundColor = Windows.UI.Color.FromArgb(255, 31, 31, 35); // #FF1F1F23
                appWindow.TitleBar.ForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255); // White text
                appWindow.TitleBar.InactiveBackgroundColor = Windows.UI.Color.FromArgb(255, 31, 31, 35);
                appWindow.TitleBar.InactiveForegroundColor = Windows.UI.Color.FromArgb(255, 176, 176, 181); // #FFB0B0B5
                appWindow.TitleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(255, 31, 31, 35);
                appWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                appWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 74, 85, 104); // #FF4A5568
                appWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                appWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 43, 108, 176); // #FF2B6CB0
                appWindow.TitleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);

                // Set a reasonable default size
                appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

                // Center the window on screen
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(appWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
                if (displayArea != null)
                {
                    var centerX = (displayArea.WorkArea.Width - 1200) / 2;
                    var centerY = (displayArea.WorkArea.Height - 800) / 2;
                    appWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                }
            }

            LoadImage();
        }

        private async void LoadImage()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    _logger.LogError($"File not found: {_filePath}", null);
                    return;
                }

                // Display file info
                var fileInfo = new FileInfo(_filePath);
                FileNameText.Text = fileInfo.Name;
                FileSizeText.Text = $"{fileInfo.Length / 1024.0:F1} KB";

                // Load the image
                var bitmap = new BitmapImage();
                using (var stream = File.OpenRead(_filePath))
                {
                    using (var randomStream = stream.AsRandomAccessStream())
                    {
                        await bitmap.SetSourceAsync(randomStream);
                    }
                }

                SnippetImage.Source = bitmap;
                _logger.LogInfo($"📸 Loaded image: {_filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load image: {_filePath}", ex);
            }
        }

        private async void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_filePath)));
                Clipboard.SetContent(dataPackage);
                _logger.LogInfo($"📋 Copied image to clipboard: {_filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to copy image to clipboard", ex);
            }
        }

        private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("PNG Image", new[] { ".png" });
                savePicker.FileTypeChoices.Add("JPEG Image", new[] { ".jpg", ".jpeg" });
                savePicker.SuggestedFileName = Path.GetFileNameWithoutExtension(_filePath);

                // Initialize with this window's handle instead of main window
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    File.Copy(_filePath, file.Path, true);
                    _logger.LogInfo($"💾 Saved image as: {file.Path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save image", ex);
            }
        }


        private void FileNameText_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileNameText.Visibility = Visibility.Collapsed;
            FileNameEditBox.Visibility = Visibility.Visible;
            FileNameEditBox.Text = Path.GetFileNameWithoutExtension(_filePath);
            FileNameEditBox.Focus(FocusState.Programmatic);
            FileNameEditBox.SelectAll();
        }

        private async void FileNameEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SaveNewFileName();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                CancelRename();
                e.Handled = true;
            }
        }

        private async void FileNameEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await SaveNewFileName();
        }

        private async Task SaveNewFileName()
        {
            try
            {
                var newFileName = FileNameEditBox.Text.Trim();
                if (!string.IsNullOrEmpty(newFileName))
                {
                    var currentName = Path.GetFileNameWithoutExtension(_filePath);
                    var result = await _fileEditor.RenameSnippetAsync(_filePath, currentName, newFileName);
                    if (result.Success && !string.IsNullOrEmpty(result.NewFilePath))
                    {
                        _filePath = result.NewFilePath;
                        Title = Path.GetFileName(_filePath);
                        FileNameText.Text = Path.GetFileName(_filePath);
                        _logger.LogInfo($"📝 Renamed file to: {_filePath}");
                    }
                    else if (!result.Success)
                    {
                        _logger.LogError($"Failed to rename file: {result.ErrorMessage}", null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to rename file", ex);
            }
            finally
            {
                FileNameEditBox.Visibility = Visibility.Collapsed;
                FileNameText.Visibility = Visibility.Visible;
            }
        }

        private void CancelRename()
        {
            FileNameEditBox.Visibility = Visibility.Collapsed;
            FileNameText.Visibility = Visibility.Visible;
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

        private void ExtractTextButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("📝 EXTRACT TEXT BUTTON CLICKED in SnippetViewer!");

            try
            {
                _logger.LogInfo($"🖼️ SnippetViewer OCR - Loading image: {_filePath}");
                _logger.LogInfo($"🖼️ File exists check: {File.Exists(_filePath)}");
                _logger.LogInfo($"🖼️ File size: {new FileInfo(_filePath).Length} bytes");

                // Verify file exists
                if (!File.Exists(_filePath))
                {
                    _logger.LogError($"❌ Image file not found: {_filePath}");
                    return;
                }

                // Create a NEW OCR service instance to avoid any caching
                using (var freshOcrService = new OcrService())
                {
                    // Load the image from file
                    using (var bitmap = new Bitmap(_filePath))
                    {
                        _logger.LogInfo($"🖼️ SnippetViewer - Image loaded: {bitmap.Width}x{bitmap.Height} pixels, Format: {bitmap.PixelFormat}");
                        _logger.LogInfo($"🔍 SnippetViewer - Processing SELECTED image: {Path.GetFileName(_filePath)}");

                        // Save a debug copy to verify we're loading the right image
                        var debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "OCR_Debug");
                        Directory.CreateDirectory(debugDir);
                        var debugPath = Path.Combine(debugDir, $"DEBUG_VIEWER_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(_filePath)}");
                        bitmap.Save(debugPath);
                        _logger.LogInfo($"🔍 Debug image saved to: {debugPath}");

                        // Also log the actual pixel data hash to verify we're processing different images
                        var pixelHash = GetImageHash(bitmap);
                        _logger.LogInfo($"🔍 Image pixel hash: {pixelHash}");

                        var ocrResult = freshOcrService.ExtractTextWithConfidence(bitmap);
                        _logger.LogInfo($"📝 SnippetViewer OCR RAW RESULT: '{ocrResult.Text}' (Length: {ocrResult.Text.Length}, Confidence: {ocrResult.Confidence:F1}%)");

                        if (!string.IsNullOrWhiteSpace(ocrResult.Text))
                        {
                            // Log the full text for debugging
                            _logger.LogInfo($"📝 SnippetViewer OCR extracted {ocrResult.Text.Length} characters from {Path.GetFileName(_filePath)}: {ocrResult.Text.Substring(0, Math.Min(100, ocrResult.Text.Length))}...");

                            // Copy extracted text to clipboard
                            var dataPackage = new DataPackage();
                            dataPackage.SetText(ocrResult.Text);
                            Clipboard.SetContent(dataPackage);

                            _logger.LogInfo($"📋 SnippetViewer OCR Text copied to clipboard from {Path.GetFileName(_filePath)} (Confidence: {ocrResult.Confidence:F1}%)");

                            // Show the extracted text in a new window
                            var ocrWindow = new OcrResultWindow(ocrResult.Text, ocrResult.Confidence, Path.GetFileName(_filePath));
                            ocrWindow.Activate();
                        }
                        else
                        {
                            _logger.LogInfo("SnippetViewer OCR: No text found in image");
                            // Could add a simple notification here if needed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to extract text", ex);
                // Could add error notification here if needed
            }
        }
    }
}