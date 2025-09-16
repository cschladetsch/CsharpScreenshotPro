namespace ScreenshotPro.Core.Models;

public class Layer
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public float Opacity { get; set; } = 1.0f;
    public List<Annotation> Annotations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    public Layer Clone()
    {
        return new Layer
        {
            Index = Index,
            Name = Name,
            IsVisible = IsVisible,
            Opacity = Opacity,
            Annotations = Annotations.Select(a => a.Clone()).ToList(),
            CreatedAt = CreatedAt,
            LastModified = LastModified
        };
    }
}

public class ExportOptions
{
    public ExportQuality Quality { get; set; } = ExportQuality.High;
    public bool IncludeMetadata { get; set; } = true;
    public bool FlattenLayers { get; set; } = true;
    public Size? MaxSize { get; set; }
    public bool OptimizeForWeb { get; set; } = false;
    public Dictionary<string, object> FormatSpecificOptions { get; set; } = new();
}

public class FileMetadata
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime LastAccessed { get; set; }
    public FileFormat Format { get; set; }
    public Size ImageSize { get; set; }
    public Dictionary<string, object> ExifData { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}