using ScreenshotPro.Core.Models;

namespace ScreenshotPro.Tests;

public class CoreModelsTests
{
    [Fact]
    public void Screenshot_Should_Create_With_Default_Values()
    {
        var screenshot = new Screenshot();

        Assert.NotEqual(string.Empty, screenshot.Id);
        Assert.True(screenshot.CapturedAt <= DateTime.UtcNow);
        Assert.Empty(screenshot.Annotations);
        Assert.Empty(screenshot.Layers);
        Assert.Empty(screenshot.Tags);
    }

    [Fact]
    public void Color_Equality_Should_Work()
    {
        var color1 = Color.Red;
        var color2 = new Color(255, 0, 0);
        var color3 = Color.Blue;

        Assert.True(color1 == color2);
        Assert.False(color1 == color3);
        Assert.True(color1 != color3);
    }

    [Fact]
    public void Region_Should_Calculate_Bounds_Correctly()
    {
        var region = new Region(10, 20, 100, 80);

        Assert.Equal(10, region.Left);
        Assert.Equal(20, region.Top);
        Assert.Equal(110, region.Right);
        Assert.Equal(100, region.Bottom);
    }

    [Fact]
    public void ArrowAnnotation_Should_Clone_Correctly()
    {
        var original = new ArrowAnnotation
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(100, 100),
            Color = Color.Red,
            ArrowStyle = ArrowStyle.Thick
        };

        var clone = original.Clone() as ArrowAnnotation;

        Assert.NotNull(clone);
        Assert.Equal(original.StartPoint.X, clone.StartPoint.X);
        Assert.Equal(original.StartPoint.Y, clone.StartPoint.Y);
        Assert.Equal(original.EndPoint.X, clone.EndPoint.X);
        Assert.Equal(original.EndPoint.Y, clone.EndPoint.Y);
        Assert.Equal(original.ArrowStyle, clone.ArrowStyle);
        Assert.NotEqual(original.Id, clone.Id); // Should have different IDs
    }
}