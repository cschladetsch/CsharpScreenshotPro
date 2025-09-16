namespace ScreenshotPro.Core.Models;

public class AppSettings
{
    public CaptureSettings Capture { get; set; } = new();
    public FileSettings Files { get; set; } = new();
    public UISettings UI { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();
    public AnnotationSettings Annotations { get; set; } = new();
}

public class CaptureSettings
{
    public CaptureMode DefaultMode { get; set; } = CaptureMode.Region;
    public bool IncludeCursor { get; set; } = false;
    public int DefaultDelay { get; set; } = 0;
    public bool AutoCopyToClipboard { get; set; } = true;
    public bool PlayCaptureSound { get; set; } = true;
    public bool AutoSave { get; set; } = false;
}

public class FileSettings
{
    public string DefaultSaveLocation { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ScreenshotPro");
    public FileNamingTemplate FileNameTemplate { get; set; } = new();
    public bool CreateDateFolders { get; set; } = true;
    public bool BackupOriginal { get; set; } = true;
    public int MaxBackupVersions { get; set; } = 5;
    public FileFormat DefaultFormat { get; set; } = FileFormat.Png;
    public ExportQuality DefaultQuality { get; set; } = ExportQuality.High;
}

public class UISettings
{
    public bool UseDarkTheme { get; set; } = false;
    public bool FloatingToolPalette { get; set; } = true;
    public bool ShowGrid { get; set; } = false;
    public bool ShowRulers { get; set; } = false;
    public bool SnapToGrid { get; set; } = false;
    public int GridSize { get; set; } = 10;
}

public class HotkeySettings
{
    public string CaptureRegion { get; set; } = "Ctrl+Shift+S";
    public string CaptureFullScreen { get; set; } = "Ctrl+Shift+F";
    public string CaptureActiveWindow { get; set; } = "Ctrl+Shift+W";
    public string QuickSave { get; set; } = "Ctrl+S";
    public string Undo { get; set; } = "Ctrl+Z";
    public string Redo { get; set; } = "Ctrl+Y";
}

public class AnnotationSettings
{
    public Color DefaultColor { get; set; } = Color.Red;
    public int DefaultThickness { get; set; } = 2;
    public float DefaultOpacity { get; set; } = 1.0f;
    public string DefaultFontFamily { get; set; } = "Arial";
    public float DefaultFontSize { get; set; } = 12;
    public bool EnablePressureSimulation { get; set; } = false;
}

public class FileNamingTemplate
{
    public string Pattern { get; set; } = "Screenshot_{timestamp:yyyy-MM-dd_HH-mm-ss}";
    public bool IncludeWindowTitle { get; set; } = false;
    public bool IncludeApplicationName { get; set; } = false;
    public string CustomPrefix { get; set; } = string.Empty;
    public string CustomSuffix { get; set; } = string.Empty;
}