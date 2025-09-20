using ScreenshotPro.Core.Services;

namespace ScreenshotPro.Core.Services;

public class SnippetFileEditor
{
    private readonly SnippetStorageService _storageService;
    private readonly LoggingService _logger;

    public SnippetFileEditor(SnippetStorageService storageService)
    {
        _storageService = storageService;
        _logger = LoggingService.Instance;
    }

    public async Task<SnippetRenameResult> RenameSnippetAsync(string filePath, string currentName, string newName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return new SnippetRenameResult
                {
                    Success = false,
                    ErrorMessage = "Name cannot be empty"
                };
            }

            // Remove file extension from newName if present
            var extension = System.IO.Path.GetExtension(filePath);
            if (newName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                newName = newName.Substring(0, newName.Length - extension.Length);
            }

            if (newName == currentName)
            {
                return new SnippetRenameResult
                {
                    Success = true,
                    NewFilePath = filePath,
                    NewFileName = currentName
                };
            }

            var success = await _storageService.RenameSnippetAsync(filePath, newName);
            if (success)
            {
                var newFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath)!, newName + extension);
                _logger.LogInfo($"✅ Renamed snippet to: {newName}");

                return new SnippetRenameResult
                {
                    Success = true,
                    NewFilePath = newFilePath,
                    NewFileName = newName
                };
            }
            else
            {
                _logger.LogInfo($"⚠️ Failed to rename snippet to: {newName} (file may already exist)");
                return new SnippetRenameResult
                {
                    Success = false,
                    ErrorMessage = "Failed to rename file (may already exist)"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Error renaming snippet: {ex.Message}", ex);
            return new SnippetRenameResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public class SnippetRenameResult
{
    public bool Success { get; set; }
    public string? NewFilePath { get; set; }
    public string? NewFileName { get; set; }
    public string? ErrorMessage { get; set; }
}