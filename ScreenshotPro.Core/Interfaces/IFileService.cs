using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Interfaces;

public interface IFileService
{
    Task<string> SaveAsync(Screenshot screenshot, string filePath, FileFormat format);
    Task<string> SaveAsync(Screenshot screenshot, string filePath, FileFormat format, ExportOptions options);

    Task<Screenshot> LoadAsync(string filePath);
    Task<List<Screenshot>> LoadBatchAsync(IEnumerable<string> filePaths);

    Task<string> ExportToPdfAsync(IEnumerable<Screenshot> screenshots, string filePath);
    Task<string> ExportToPowerPointAsync(IEnumerable<Screenshot> screenshots, string filePath);
    Task<string> ExportToWordAsync(IEnumerable<Screenshot> screenshots, string filePath);

    Task<bool> DeleteAsync(string filePath);
    Task<string> BackupAsync(string filePath);
    Task RestoreBackupAsync(string backupPath, string targetPath);

    Task<List<string>> SearchAsync(string searchTerm, string directory);
    Task<FileMetadata> GetMetadataAsync(string filePath);

    string GenerateFileName(FileNamingTemplate template);
    string GetDefaultSaveLocation();

    event EventHandler<FileOperationEventArgs> FileOperationCompleted;
}