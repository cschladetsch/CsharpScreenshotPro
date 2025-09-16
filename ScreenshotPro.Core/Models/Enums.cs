namespace ScreenshotPro.Core.Models;

public enum CaptureMode
{
    FullScreen,
    ActiveWindow,
    Region,
    Scrolling,
    MultiMonitor
}

public enum AnnotationType
{
    Arrow,
    Rectangle,
    Circle,
    Line,
    Text,
    Highlight,
    Blur,
    Callout,
    NumberedBullet,
    FreehandDrawing
}

public enum ArrowStyle
{
    Standard,
    Thick,
    Dashed,
    Double,
    Curved
}

public enum HighlightStyle
{
    Rectangle,
    Rounded,
    Oval
}

public enum FontWeight
{
    Thin = 100,
    ExtraLight = 200,
    Light = 300,
    Normal = 400,
    Medium = 500,
    SemiBold = 600,
    Bold = 700,
    ExtraBold = 800,
    Black = 900
}

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

public enum FileFormat
{
    Png,
    Jpg,
    Gif,
    Bmp,
    Tiff,
    Pdf
}

public enum PixelFormat
{
    Rgb24,
    Bgr24,
    Rgba32,
    Bgra32,
    Gray8,
    Gray16
}

public enum FileOperation
{
    Save,
    Load,
    Delete,
    Export,
    Backup,
    Restore
}

public enum ExportQuality
{
    Low = 50,
    Medium = 75,
    High = 90,
    Maximum = 100
}