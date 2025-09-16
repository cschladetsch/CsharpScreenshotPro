namespace ScreenshotPro.Core.Models;

public abstract class Annotation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AnnotationType Type { get; set; }
    public Point Position { get; set; }
    public Size Size { get; set; }
    public Color Color { get; set; } = Color.Red;
    public int Thickness { get; set; } = 2;
    public float Opacity { get; set; } = 1.0f;
    public int LayerIndex { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();

    public abstract Annotation Clone();
    public abstract void Render(IRenderContext context);
}

public class ArrowAnnotation : Annotation
{
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Standard;

    public ArrowAnnotation()
    {
        Type = AnnotationType.Arrow;
    }

    public override Annotation Clone()
    {
        return new ArrowAnnotation
        {
            Position = Position,
            Size = Size,
            Color = Color,
            Thickness = Thickness,
            Opacity = Opacity,
            LayerIndex = LayerIndex,
            CreatedAt = CreatedAt,
            LastModified = LastModified,
            StartPoint = StartPoint,
            EndPoint = EndPoint,
            ArrowStyle = ArrowStyle,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    public override void Render(IRenderContext context)
    {
        context.DrawArrow(StartPoint, EndPoint, Color, Thickness, ArrowStyle);
    }
}

public class RectangleAnnotation : Annotation
{
    public bool IsFilled { get; set; } = false;
    public Color FillColor { get; set; } = Color.Transparent;

    public RectangleAnnotation()
    {
        Type = AnnotationType.Rectangle;
    }

    public override Annotation Clone()
    {
        return new RectangleAnnotation
        {
            Position = Position,
            Size = Size,
            Color = Color,
            Thickness = Thickness,
            Opacity = Opacity,
            LayerIndex = LayerIndex,
            CreatedAt = CreatedAt,
            LastModified = LastModified,
            IsFilled = IsFilled,
            FillColor = FillColor,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    public override void Render(IRenderContext context)
    {
        if (IsFilled)
        {
            context.FillRectangle(Position, Size, FillColor);
        }
        context.DrawRectangle(Position, Size, Color, Thickness);
    }
}

public class TextAnnotation : Annotation
{
    public string Text { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 12;
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;
    public bool IsItalic { get; set; } = false;
    public bool IsUnderlined { get; set; } = false;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public Color BackgroundColor { get; set; } = Color.Transparent;

    public TextAnnotation()
    {
        Type = AnnotationType.Text;
    }

    public override Annotation Clone()
    {
        return new TextAnnotation
        {
            Position = Position,
            Size = Size,
            Color = Color,
            Opacity = Opacity,
            LayerIndex = LayerIndex,
            CreatedAt = CreatedAt,
            LastModified = LastModified,
            Text = Text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            IsItalic = IsItalic,
            IsUnderlined = IsUnderlined,
            Alignment = Alignment,
            BackgroundColor = BackgroundColor,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    public override void Render(IRenderContext context)
    {
        if (BackgroundColor != Color.Transparent)
        {
            context.FillRectangle(Position, Size, BackgroundColor);
        }
        context.DrawText(Text, Position, FontFamily, FontSize, Color, Alignment);
    }
}

public class HighlightAnnotation : Annotation
{
    public HighlightStyle Style { get; set; } = HighlightStyle.Rectangle;

    public HighlightAnnotation()
    {
        Type = AnnotationType.Highlight;
        Color = Color.Yellow;
        Opacity = 0.3f;
    }

    public override Annotation Clone()
    {
        return new HighlightAnnotation
        {
            Position = Position,
            Size = Size,
            Color = Color,
            Opacity = Opacity,
            LayerIndex = LayerIndex,
            CreatedAt = CreatedAt,
            LastModified = LastModified,
            Style = Style,
            Properties = new Dictionary<string, object>(Properties)
        };
    }

    public override void Render(IRenderContext context)
    {
        context.Highlight(Position, Size, Color, Opacity, Style);
    }
}