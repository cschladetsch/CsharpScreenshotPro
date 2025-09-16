namespace ScreenshotPro.Core.Models;

public class Screenshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public PixelFormat PixelFormat { get; set; } = PixelFormat.Bgra32;

    public CaptureMode CaptureMode { get; set; }
    public string SourceApplication { get; set; } = string.Empty;
    public string SourceWindowTitle { get; set; } = string.Empty;

    public List<Annotation> Annotations { get; set; } = new();
    public List<Layer> Layers { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public string FilePath { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public Screenshot Clone()
    {
        return new Screenshot
        {
            Id = Id,
            CapturedAt = CapturedAt,
            Title = Title,
            Description = Description,
            ImageData = (byte[])ImageData.Clone(),
            Width = Width,
            Height = Height,
            PixelFormat = PixelFormat,
            CaptureMode = CaptureMode,
            SourceApplication = SourceApplication,
            SourceWindowTitle = SourceWindowTitle,
            Annotations = Annotations.Select(a => a.Clone()).ToList(),
            Layers = Layers.Select(l => l.Clone()).ToList(),
            Metadata = new Dictionary<string, object>(Metadata),
            Tags = new List<string>(Tags),
            FilePath = FilePath,
            LastModified = LastModified
        };
    }
}