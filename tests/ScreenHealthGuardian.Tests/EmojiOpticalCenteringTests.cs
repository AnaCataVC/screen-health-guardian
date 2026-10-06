using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ScreenHealthGuardian.Views;
using Xunit;

namespace ScreenHealthGuardian.Tests;

public class EmojiOpticalCenteringTests
{
    [Theory]
    [InlineData("👀")]
    [InlineData("✨")]
    public void CalculateGlyphCenterOffset_CompensatesDownwardEmojiBaselineShift(string emoji)
    {
        var fontFamily = new FontFamily("Segoe UI Emoji");
        double fontSize = 32.0;

        var (offsetX, offsetY) = AlertOverlayWindow.CalculateGlyphCenterOffset(emoji, fontFamily, fontSize, 1.0);

        // In Segoe UI Emoji, both 👀 and ✨ sit below the typographic vertical center (positive OffsetY)
        Assert.True(offsetY > 1.0, $"Expected positive vertical offset for '{emoji}', but got {offsetY}");

        // Verify that applying -offsetX and -offsetY places the glyph ink center at the exact layout center
        var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var formattedText = new FormattedText(
            emoji,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            1.0);

        Rect bounds = formattedText.BuildGeometry(new Point(0, 0)).Bounds;
        double correctedCenterX = (bounds.Left + bounds.Width / 2.0) - offsetX;
        double correctedCenterY = (bounds.Top + bounds.Height / 2.0) - offsetY;

        double expectedCenterX = formattedText.WidthIncludingTrailingWhitespace / 2.0;
        double expectedCenterY = formattedText.Height / 2.0;

        Assert.Equal(expectedCenterX, correctedCenterX, precision: 4);
        Assert.Equal(expectedCenterY, correctedCenterY, precision: 4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CalculateGlyphCenterOffset_ReturnsZeroForEmptyOrWhitespaceInput(string input)
    {
        var fontFamily = new FontFamily("Segoe UI Emoji");

        var (offsetX, offsetY) = AlertOverlayWindow.CalculateGlyphCenterOffset(input, fontFamily, 32.0, 1.0);

        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void CalculateGlyphCenterOffset_ReturnsZeroForNonPositiveFontSize()
    {
        var fontFamily = new FontFamily("Segoe UI Emoji");

        var (offsetX, offsetY) = AlertOverlayWindow.CalculateGlyphCenterOffset("👀", fontFamily, 0, 1.0);

        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }
}
