using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using ScreenshotPro.Core.Services;
using ScreenshotPro.UI.Views;

namespace ScreenshotPro.UI.ViewModels;

public class LibraryPageViewModel : INotifyPropertyChanged
{
    private readonly SnippetStorageService _storageService;
    private readonly SnippetFileEditor _fileEditor;
    private readonly LoggingService _logger;
    private readonly string _snippetsFolder;

    public ObservableCollection<ScreenshotItem> Screenshots { get; } = new();

    public bool HasSelectedItems => Screenshots.Any(s => s.IsSelected);

    public event PropertyChangedEventHandler? PropertyChanged;

    public LibraryPageViewModel()
    {
        _storageService = new SnippetStorageService();
        _fileEditor = new SnippetFileEditor(_storageService);
        _logger = LoggingService.Instance;

        // Set up Documents/Snippets folder
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _snippetsFolder = Path.Combine(documentsPath, "Snippets");
        Directory.CreateDirectory(_snippetsFolder);
    }

    public async Task LoadScreenshotsAsync()
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
                    ThumbnailPath = file
                };

                item.PropertyChanged += OnScreenshotItemPropertyChanged;
                Screenshots.Add(item);
            }

            _logger.LogInfo($"Loaded {Screenshots.Count} screenshots from {_snippetsFolder}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading screenshots: {ex.Message}", ex);
        }
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

    public void StartEditingItem(ScreenshotItem item)
    {
        item.IsEditing = true;
        _logger.LogInfo($"📝 Started editing filename for: {item.FileName}");
    }

    public async Task SaveItemNameAsync(ScreenshotItem item, string newName)
    {
        var result = await _fileEditor.RenameSnippetAsync(item.FilePath, item.FileName, newName);

        if (result.Success && result.NewFilePath != null && result.NewFileName != null)
        {
            item.FilePath = result.NewFilePath;
            item.FileName = result.NewFileName;
            item.ThumbnailPath = result.NewFilePath;
        }

        item.IsEditing = false;
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
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}