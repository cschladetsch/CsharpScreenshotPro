using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using ScreenshotPro.Core.Services;
using System;
using System.IO;
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

        public SnippetViewerWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            _logger = LoggingService.Instance;
            _fileEditor = new SnippetFileEditor(new SnippetStorageService());

            // Set window title
            Title = Path.GetFileName(filePath);

            // Configure window
            var appWindow = AppWindow;
            if (appWindow != null)
            {
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
    }
}