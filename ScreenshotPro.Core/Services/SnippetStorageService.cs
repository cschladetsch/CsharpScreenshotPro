using System.Drawing;
using System.Drawing.Imaging;
using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Services;

public class SnippetStorageService
{
    private readonly string _snippetsFolder;

    public SnippetStorageService()
    {
        _snippetsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Snippets");
        EnsureSnippetsFolder();
    }

    private void EnsureSnippetsFolder()
    {
        if (!Directory.Exists(_snippetsFolder))
        {
            Directory.CreateDirectory(_snippetsFolder);
        }
    }

    public async Task<string> SaveSnippetAsync(Bitmap bitmap, string? customName = null)
    {
        try
        {
            var fileName = customName ?? $"Snippet_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".png";
            }

            var filePath = Path.Combine(_snippetsFolder, fileName);

            // Ensure unique filename
            int counter = 1;
            var originalPath = filePath;
            while (File.Exists(filePath))
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
                var extension = Path.GetExtension(originalPath);
                filePath = Path.Combine(_snippetsFolder, $"{nameWithoutExtension}_{counter}{extension}");
                counter++;
            }

            await Task.Run(() =>
            {
                bitmap.Save(filePath, ImageFormat.Png);
            });

            return filePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save snippet: {ex.Message}", ex);
        }
    }

    public string GetSnippetsFolder()
    {
        return _snippetsFolder;
    }

    public async Task<List<FileInfo>> GetSnippetsAsync()
    {
        return await Task.Run(() =>
        {
            var directory = new DirectoryInfo(_snippetsFolder);
            return directory.GetFiles("*.png")
                           .OrderByDescending(f => f.CreationTime)
                           .ToList();
        });
    }

    public async Task DeleteSnippetAsync(string filePath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        });
    }

    public async Task<bool> RenameSnippetAsync(string oldFilePath, string newName)
    {
        try
        {
            if (!File.Exists(oldFilePath))
                return false;

            var directory = Path.GetDirectoryName(oldFilePath);
            var extension = Path.GetExtension(oldFilePath);

            // Ensure the new name has the correct extension
            if (!newName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                newName += extension;
            }

            var newFilePath = Path.Combine(directory!, newName);

            // Check if target file already exists
            if (File.Exists(newFilePath))
                return false;

            await Task.Run(() =>
            {
                File.Move(oldFilePath, newFilePath);
            });

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to rename snippet: {ex.Message}", ex);
        }
    }

    public async Task OpenSnippetsFolderAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _snippetsFolder);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to open snippets folder: {ex.Message}", ex);
            }
        });
    }
}