using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using ScreenshotPro.Core.Services;
using ScreenshotPro.UI.Views;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ScreenshotPro.UI.ViewModels;

public class LibraryPageViewModel : INotifyPropertyChanged
{
    private readonly SnippetStorageService _storageService;
    private readonly SnippetFileEditor _fileEditor;
    private readonly LoggingService _logger;
    private readonly string _snippetsFolder;
    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<ScreenshotItem> Screenshots { get; } = new();

    public bool HasSelectedItems => Screenshots.Any(s => s.IsSelected);

    public bool HasSingleSelectedItem => Screenshots.Count(s => s.IsSelected) == 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LibraryPageViewModel()
    {
        _storageService = new SnippetStorageService();
        _fileEditor = new SnippetFileEditor(_storageService);
        _logger = LoggingService.Instance;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Set up Documents/Snippets folder - check for OneDrive first
        var documentsPath = GetDocumentsPath();
        _snippetsFolder = Path.Combine(documentsPath, "Snippets");
        Directory.CreateDirectory(_snippetsFolder);

        _logger.LogInfo($"📁 Using Snippets folder: {_snippetsFolder}");
    }

    public async Task LoadScreenshotsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                _dispatcherQueue.TryEnqueue(() => Screenshots.Clear());

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
                        ThumbnailPath = file
                    };

                    item.PropertyChanged += OnScreenshotItemPropertyChanged;

                    _dispatcherQueue.TryEnqueue(() => Screenshots.Add(item));
                }

                _logger.LogInfo($"📷 Loaded {Screenshots.Count} screenshots from {_snippetsFolder}");

                // Log each loaded screenshot for debugging
                foreach (var screenshot in Screenshots)
                {
                    _logger.LogInfo($"📷 Screenshot: {screenshot.FileName} (IsEditing: {screenshot.IsEditing})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading screenshots: {ex.Message}", ex);
            }
        });
    }

    public async Task<bool> DeleteSelectedItemsAsync()
    {
        try
        {
            var selectedItems = Screenshots.Where(s => s.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                _logger.LogInfo("🗑️ Delete requested but no items selected");
                return false;
            }

            _logger.LogInfo($"🗑️ Deleting {selectedItems.Count} items");

            foreach (var item in selectedItems)
            {
                try
                {
                    // Move file to recycle bin asynchronously
                    await Task.Run(() =>
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                            item.FilePath,
                            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    });

                    _logger.LogInfo($"🗑️ Moved to recycle bin: {item.FileName}");
                    Screenshots.Remove(item);
                }
                catch (Exception fileEx)
                {
                    _logger.LogError($"❌ Failed to delete {item.FileName}", fileEx);
                }
            }

            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(HasSingleSelectedItem));
            _logger.LogInfo($"✅ Deleted {selectedItems.Count} items successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to delete selected items", ex);
            return false;
        }
    }

    public async Task<string?> SaveSnippetAsync(System.Drawing.Bitmap bitmap)
    {
        try
        {
            var filePath = await _storageService.SaveSnippetAsync(bitmap);
            _logger.LogInfo($"✅ Snippet saved successfully to: {filePath}");

            // Copy to clipboard
            await CopyToClipboardAsync(filePath);

            // Refresh the screenshots list to show the new capture
            await LoadScreenshotsAsync();
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Failed to save snippet", ex);
            return null;
        }
    }

    private async Task CopyToClipboardAsync(string filePath)
    {
        try
        {
            var dataPackage = new DataPackage();
            var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(storageFile));
            Clipboard.SetContent(dataPackage);
            _logger.LogInfo($"📋 Copied new snippet to clipboard: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to copy snippet to clipboard", ex);
        }
    }

    public void StartEditingItem(ScreenshotItem item)
    {
        _logger.LogInfo($"📝 StartEditingItem called for: {item.FileName}");
        _logger.LogInfo($"📝 Before: IsEditing = {item.IsEditing}");
        item.IsEditing = true;
        _logger.LogInfo($"📝 After: IsEditing = {item.IsEditing}");
        _logger.LogInfo($"📝 Started editing filename for: {item.FileName}");
    }

    public async Task SaveItemNameAsync(ScreenshotItem item, string newName)
    {
        _logger.LogInfo($"💾 SaveItemNameAsync: Attempting to rename '{item.FileName}' to '{newName}'");
        _logger.LogInfo($"💾 Current file path: {item.FilePath}");
        _logger.LogInfo($"💾 File exists before rename: {File.Exists(item.FilePath)}");

        var result = await _fileEditor.RenameSnippetAsync(item.FilePath, item.FileName, newName);

        _logger.LogInfo($"💾 Rename result: Success={result.Success}, NewFileName={result.NewFileName}, ErrorMessage={result.ErrorMessage}");

        if (result.Success && result.NewFilePath != null && result.NewFileName != null)
        {
            _logger.LogInfo($"💾 New file path: {result.NewFilePath}");
            _logger.LogInfo($"💾 New file exists: {File.Exists(result.NewFilePath)}");
            _logger.LogInfo($"💾 Old file still exists: {File.Exists(item.FilePath)}");

            item.FilePath = result.NewFilePath;
            item.FileName = result.NewFileName;
            item.ThumbnailPath = result.NewFilePath;
        }
        else
        {
            _logger.LogInfo($"💾 Rename failed, keeping original name: {item.FileName}");
        }

        item.IsEditing = false;
        _logger.LogInfo($"💾 Final item state: FileName={item.FileName}, IsEditing={item.IsEditing}");
    }

    public void CancelEditingItem(ScreenshotItem item)
    {
        item.IsEditing = false;
    }

    public void ToggleItemSelection(ScreenshotItem item)
    {
        item.IsSelected = !item.IsSelected;
        _logger.LogInfo($"📋 Screenshot {item.FileName} selection changed to: {item.IsSelected}");
        OnPropertyChanged(nameof(HasSelectedItems));
    }

    public async Task OpenSnippetsFolderAsync()
    {
        try
        {
            await _storageService.OpenSnippetsFolderAsync();
            _logger.LogInfo($"✅ Opened snippets folder: {_snippetsFolder}");
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Failed to open snippets folder", ex);
        }
    }

    private void OnScreenshotItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScreenshotItem.IsSelected))
        {
            OnPropertyChanged(nameof(HasSelectedItems));
            OnPropertyChanged(nameof(HasSingleSelectedItem));
        }
    }

    private string GetDocumentsPath()
    {
        // Try OneDrive Documents first
        var oneDriveDocuments = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Documents");
        if (Directory.Exists(oneDriveDocuments))
        {
            return oneDriveDocuments;
        }

        // Fallback to regular Documents folder
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}