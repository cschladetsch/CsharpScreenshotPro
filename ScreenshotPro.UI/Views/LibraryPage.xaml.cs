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
using ScreenshotProRegion = ScreenshotPro.Core.Models.Region;

namespace ScreenshotPro.UI.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly ScreenshotCaptureOrchestrator _captureOrchestrator;
        private readonly LibraryPageViewModel _viewModel;
        private readonly LoggingService _logger;
        private Windows.Graphics.PointInt32 _originalWindowPosition;

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

                // Prepare the desktop bitmap
                var desktopBitmap = await _captureOrchestrator.PrepareDesktopBitmapAsync();

                // Create and show the region selection window
                await ShowRegionSelectionWindow(desktopBitmap);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Capture failed", ex);
                RestoreMainWindow();
                await ShowErrorDialog("Capture Failed", $"An error occurred while capturing: {ex.Message}");
            }
        }

        private Task ShowRegionSelectionWindow(Bitmap desktopBitmap)
        {
            try
            {
                _logger.LogInfo("🏗️ Creating RegionSelectionWindow with darkened desktop");
                var regionWindow = new RegionSelectionWindow(desktopBitmap);

                // Set up event handlers
                regionWindow.RegionSelected += async (sender, e) =>
                {
                    _logger.LogInfo("🎯 RegionSelected event received");
                    regionWindow.Close();
                    await ProcessSelectedRegion((ScreenshotProRegion)e.SelectedRegion);
                };

                regionWindow.SelectionCancelled += (sender, e) =>
                {
                    _logger.LogInfo("❌ SelectionCancelled event received");
                    regionWindow.Close();
                    RestoreMainWindow();
                };

                // Show the region selection window
                _logger.LogInfo("🚀 Activating RegionSelectionWindow");
                regionWindow.Activate();
                _logger.LogInfo("✅ RegionSelectionWindow activated");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error in ShowRegionSelectionWindow", ex);
                RestoreMainWindow();
                return Task.CompletedTask;
            }
        }

        private async Task ProcessSelectedRegion(ScreenshotProRegion region)
        {
            try
            {
                var bitmap = await _captureOrchestrator.ProcessSelectedRegionAsync(region);
                if (bitmap != null)
                {
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
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("🗑️ DELETE BUTTON CLICKED!");
            await _viewModel.DeleteSelectedItemsAsync();
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