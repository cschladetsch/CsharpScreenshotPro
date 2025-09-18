using System.IO;

namespace ScreenshotPro.Core.Services;

public class LoggingService
{
    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
    public static LoggingService Instance => _instance.Value;

    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly object _lockObject = new();

    private LoggingService()
    {
        // Create Logs folder in the application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _logDirectory = Path.Combine(appDirectory, "Logs");
        Directory.CreateDirectory(_logDirectory);

        // Create timestamped log file
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logFilePath = Path.Combine(_logDirectory, $"ScreenshotPro_{timestamp}.log");

        // Write initial log entry
        WriteToFile($"=== ScreenshotPro Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.ffffff");
        var logEntry = $"[{timestamp}] {message}";

        WriteToFile(logEntry);

        // Also write to console for immediate feedback
        try
        {
            Console.WriteLine(logEntry);
        }
        catch
        {
            // Ignore console write errors
        }
    }

    public void LogError(string message, Exception? ex = null)
    {
        var errorMessage = ex != null ? $"{message}: {ex.Message}" : message;
        Log($"ERROR: {errorMessage}");
    }

    public void LogInfo(string message)
    {
        Log($"INFO: {message}");
    }

    public void LogDebug(string message)
    {
        Log($"DEBUG: {message}");
    }

    private void WriteToFile(string logEntry)
    {
        try
        {
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Ignore file write errors to prevent logging from breaking the app
        }
    }

    public string GetLogFilePath() => _logFilePath;
    public string GetLogDirectory() => _logDirectory;
}