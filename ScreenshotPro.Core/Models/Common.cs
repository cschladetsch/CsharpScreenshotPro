namespace ScreenshotPro.Core.Models;

public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Point Empty => new(0, 0);
}

public struct Size
{
    public int Width { get; set; }
    public int Height { get; set; }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static Size Empty => new(0, 0);
}

public struct Region
{
    public Point Location { get; set; }
    public Size Size { get; set; }

    public Region(Point location, Size size)
    {
        Location = location;
        Size = size;
    }

    public Region(int x, int y, int width, int height)
    {
        Location = new Point(x, y);
        Size = new Size(width, height);
    }

    public int Left => Location.X;
    public int Top => Location.Y;
    public int Right => Location.X + Size.Width;
    public int Bottom => Location.Y + Size.Height;

    public static Region Empty => new(0, 0, 0, 0);
}

public struct Color : IEquatable<Color>
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Black => new(0, 0, 0);
    public static Color White => new(255, 255, 255);
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color Blue => new(0, 0, 255);
    public static Color Yellow => new(255, 255, 0);

    public uint ToArgb()
    {
        return (uint)((A << 24) | (R << 16) | (G << 8) | B);
    }

    public static Color FromArgb(uint argb)
    {
        return new Color(
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF),
            (byte)((argb >> 24) & 0xFF)
        );
    }

    public bool Equals(Color other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
    }
}

public interface IRenderContext
{
    void DrawArrow(Point start, Point end, Color color, int thickness, ArrowStyle style);
    void DrawRectangle(Point position, Size size, Color color, int thickness);
    void DrawCircle(Point center, int radius, Color color, int thickness);
    void DrawLine(Point start, Point end, Color color, int thickness);
    void DrawText(string text, Point position, string fontFamily, float fontSize, Color color, TextAlignment alignment);
    void FillRectangle(Point position, Size size, Color color);
    void FillCircle(Point center, int radius, Color color);
    void Highlight(Point position, Size size, Color color, float opacity, HighlightStyle style);
    void Blur(Point position, Size size, int intensity);
}