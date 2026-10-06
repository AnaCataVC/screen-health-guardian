# WPF Emoji Optical Centering in Circular Badges

## The Problem
When rendering emoji glyphs (such as `👀` and `✨`) inside a circular WPF `Border` (`Width="68" Height="68" CornerRadius="34"`) using a standard `TextBlock` (`FontFamily="Segoe UI Emoji"`, `FontSize="32"`, `HorizontalAlignment="Center"`, `VerticalAlignment="Center"`), the visible icon appears noticeably shifted downward inside the circle.

### Root Cause
WPF's layout engine (`TextBlock.Measure` / `FormattedText`) centers the **typographic line box** (`DesiredSize = 43.94 x 42.56 px`, with `Baseline = 34.53 px`) rather than the **ink bounding box** (`Geometry.Bounds`) of the glyph:
- In `Segoe UI Emoji` at `32pt`, the `👀` glyph's actual ink geometry spans `Top = 13.11 px` to `Bottom = 40.14 px` (`Height = 27.03 px`), leaving `13.11 px` of internal ascent whitespace above the glyph and only `2.42 px` below it. This produces a **`+5.34 px` downward visual offset** relative to the circle's geometric center.
- The `✨` glyph spans `Top = 9.73 px` to `Bottom = 37.88 px` (`Height = 28.15 px`), producing a **`+2.53 px` downward visual offset**.
- Because different emojis have distinct vertical ink bounds relative to the font baseline, a static XAML `Margin` cannot center all icons accurately.

## The Solution
To achieve exact optical centering for any emoji glyph in WPF:
1. Build a `FormattedText` instance matching the `TextBlock`'s `FontFamily`, `FontSize`, and monitor DPI (`VisualTreeHelper.GetDpi(this).PixelsPerDip`).
2. Extract the rendered ink bounding box via `formattedText.BuildGeometry(new Point(0, 0)).Bounds`.
3. Compute the delta between the glyph ink center (`bounds.Left + bounds.Width / 2`, `bounds.Top + bounds.Height / 2`) and the typographic layout center (`formattedText.WidthIncludingTrailingWhitespace / 2`, `formattedText.Height / 2`).
4. Apply an inverse `TranslateTransform(-offsetX, -offsetY)` to the `TextBlock`, aligning the glyph's true geometric center with the center of the circular badge with sub-pixel accuracy.
