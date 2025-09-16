using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Core.Interfaces;

public interface ISettingsService
{
    Task<T> GetSettingAsync<T>(string key, T defaultValue = default!);
    Task SetSettingAsync<T>(string key, T value);
    Task<bool> HasSettingAsync(string key);
    Task RemoveSettingAsync(string key);

    Task<AppSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);

    Task ResetToDefaultsAsync();
    Task<string> ExportSettingsAsync(string filePath);
    Task ImportSettingsAsync(string filePath);

    Task MigrateSettingsAsync(Version fromVersion, Version toVersion);
    Task<bool> ValidateSettingsAsync(AppSettings settings);

    event EventHandler<SettingsChangedEventArgs> SettingsChanged;
}