namespace ScreenshotPro.Core.Models;

public class CaptureEventArgs : EventArgs
{
    public Screenshot Screenshot { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }

    public CaptureEventArgs(Screenshot screenshot, bool success, string errorMessage = "", TimeSpan duration = default)
    {
        Screenshot = screenshot;
        Success = success;
        ErrorMessage = errorMessage;
        Duration = duration;
    }
}

public class AnnotationEventArgs : EventArgs
{
    public Screenshot Screenshot { get; set; }
    public Annotation Annotation { get; set; }
    public string Operation { get; set; } = string.Empty;

    public AnnotationEventArgs(Screenshot screenshot, Annotation annotation, string operation)
    {
        Screenshot = screenshot;
        Annotation = annotation;
        Operation = operation;
    }
}

public class FileOperationEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public FileOperation Operation { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public TimeSpan Duration { get; set; }

    public FileOperationEventArgs(string filePath, FileOperation operation, bool success, string errorMessage = "", long fileSize = 0, TimeSpan duration = default)
    {
        FilePath = filePath;
        Operation = operation;
        Success = success;
        ErrorMessage = errorMessage;
        FileSize = fileSize;
        Duration = duration;
    }
}

public class SettingsChangedEventArgs : EventArgs
{
    public string SettingKey { get; set; } = string.Empty;
    public object OldValue { get; set; }
    public object NewValue { get; set; }

    public SettingsChangedEventArgs(string settingKey, object oldValue, object newValue)
    {
        SettingKey = settingKey;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public class RegionSelectedEventArgs : EventArgs
{
    public Region? SelectedRegion { get; set; }
    public bool WasCancelled { get; set; }

    public RegionSelectedEventArgs()
    {
    }

    public RegionSelectedEventArgs(Region selectedRegion)
    {
        SelectedRegion = selectedRegion;
        WasCancelled = false;
    }

    public RegionSelectedEventArgs(bool cancelled)
    {
        WasCancelled = cancelled;
    }
}