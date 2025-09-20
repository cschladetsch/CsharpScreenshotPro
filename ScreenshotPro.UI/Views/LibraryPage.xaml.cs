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

namespace ScreenshotPro.UI.Views
{
    public sealed partial class LibraryPage : Page
    {
        private readonly ScreenshotCaptureOrchestrator _captureOrchestrator;
        private readonly LibraryPageViewModel _viewModel;
        private readonly LoggingService _logger;

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
                // Move window off-screen before starting capture
                var mainWindow = App.MainWindow;
                if (mainWindow != null)
                {
                    var appWindow = mainWindow.AppWindow;
                    appWindow.Move(new Windows.Graphics.PointInt32(-5000, -5000));
                }

                await _captureOrchestrator.StartSnippingModeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Capture failed", ex);
                RestoreMainWindow();
                await ShowErrorDialog("Capture Failed", $"An error occurred while capturing: {ex.Message}");
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
            if (sender is TextBlock textBlock && textBlock.Tag is ScreenshotItem item)
            {
                _viewModel.StartEditingItem(item);
            }
        }

        private async void FileNameEdit_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is ScreenshotItem item)
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    await _viewModel.SaveItemNameAsync(item, textBox.Text);
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    _viewModel.CancelEditingItem(item);
                    e.Handled = true;
                }
            }
        }

        private async void FileNameEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is ScreenshotItem item)
            {
                await _viewModel.SaveItemNameAsync(item, textBox.Text);
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
                    // Move window back to center of screen first (using virtual desktop dimensions)
                    var screenWidth = GetSystemMetrics(78); // SM_CXVIRTUALSCREEN - Width of virtual desktop
                    var screenHeight = GetSystemMetrics(79); // SM_CYVIRTUALSCREEN - Height of virtual desktop

                    // Get actual window size
                    var windowWidth = mainWindow.AppWindow.Size.Width;
                    var windowHeight = mainWindow.AppWindow.Size.Height;

                    var centerX = (screenWidth - windowWidth) / 2;
                    var centerY = (screenHeight - windowHeight) / 2;
                    mainWindow.AppWindow.Move(new Windows.Graphics.PointInt32(centerX, centerY));
                    _logger.LogInfo($"🔧 Window moved back to center: ({centerX}, {centerY})");

                    // Restore window visibility - AppWindow.Show() handles this
                    _logger.LogInfo("🔧 Preparing to show window");

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

        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
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