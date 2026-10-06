using System.Globalization;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenHealthGuardian.Services;

namespace ScreenHealthGuardian.Views;

public partial class AlertOverlayWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;
    private readonly int _totalSeconds;
    private readonly Action? _onDismissed;
    private bool _dismissed = false;

    public AlertOverlayWindow(
        string emoji,
        string title,
        string message,
        string buttonText,
        string accentHex,
        DisplayInfo? targetDisplay = null,
        int autoDismissSec = 30,
        bool soundEnabled = true,
        Action? onDismissed = null)
    {
        InitializeComponent();

        _totalSeconds = autoDismissSec;
        _onDismissed = onDismissed;

        // Position window within target display bounds
        if (targetDisplay != null)
        {
            var (left, top) = targetDisplay.CalculateCenter(Width, Height);
            Left = left;
            Top = top;
        }
        else
        {
            Left = SystemParameters.WorkArea.Left + Math.Max(0, (SystemParameters.WorkArea.Width - Width) / 2.0);
            Top = SystemParameters.WorkArea.Top + Math.Max(0, (SystemParameters.WorkArea.Height - Height) / 2.0);
        }

        TitleBlock.Text = title;
        MessageBlock.Text = message;
        DismissButton.Content = buttonText;

        var accentBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(accentHex)!;
        TitleBlock.Foreground = accentBrush;
        AutoDismissProgress.Foreground = accentBrush;

        if (string.IsNullOrWhiteSpace(emoji))
        {
            EmojiBadge.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmojiBlock.Text = emoji;
            EmojiBadge.Visibility = Visibility.Visible;
            EmojiBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(140, accentBrush.Color.R, accentBrush.Color.G, accentBrush.Color.B));
            CenterEmojiOpticalBounds(emoji);
        }

        if (soundEnabled)
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Silently handle sound play failure
            }
        }

        AutoDismissProgress.Maximum = _totalSeconds;
        AutoDismissProgress.Value = _totalSeconds;

        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _dismissTimer.Tick += DismissTimer_Tick;

        if (_totalSeconds > 0)
        {
            _dismissTimer.Start();
        }
        else
        {
            AutoDismissProgress.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Calculates the optical center offset between the WPF TextBlock layout box
    /// and the actual rendered glyph ink bounding box for a given font and size.
    /// </summary>
    public static (double OffsetX, double OffsetY) CalculateGlyphCenterOffset(
        string text,
        FontFamily fontFamily,
        double fontSize,
        double pixelsPerDip = 1.0)
    {
        if (string.IsNullOrWhiteSpace(text) || fontSize <= 0)
        {
            return (0, 0);
        }

        var typeface = new Typeface(
            fontFamily,
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

        var formattedText = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White,
            pixelsPerDip > 0 ? pixelsPerDip : 1.0);

        Geometry geometry = formattedText.BuildGeometry(new Point(0, 0));
        Rect bounds = geometry.Bounds;

        if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return (0, 0);
        }

        double layoutCenterX = formattedText.WidthIncludingTrailingWhitespace / 2.0;
        double layoutCenterY = formattedText.Height / 2.0;

        double glyphCenterX = bounds.Left + (bounds.Width / 2.0);
        double glyphCenterY = bounds.Top + (bounds.Height / 2.0);

        return (glyphCenterX - layoutCenterX, glyphCenterY - layoutCenterY);
    }

    private void CenterEmojiOpticalBounds(string emoji)
    {
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var (offsetX, offsetY) = CalculateGlyphCenterOffset(
            emoji,
            EmojiBlock.FontFamily,
            EmojiBlock.FontSize,
            pixelsPerDip);

        EmojiBlock.RenderTransform = new TranslateTransform(-offsetX, -offsetY);
    }

    private double _elapsedMs = 0;

    private void DismissTimer_Tick(object? sender, EventArgs e)
    {
        _elapsedMs += 100;
        double totalMs = _totalSeconds * 1000.0;
        double remaining = totalMs - _elapsedMs;

        if (remaining <= 0)
        {
            Dismiss();
        }
        else
        {
            AutoDismissProgress.Value = (remaining / totalMs) * _totalSeconds;
        }
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Dismiss();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Dismiss();
        }
    }

    public void Dismiss()
    {
        if (_dismissed) return;
        _dismissed = true;

        _dismissTimer.Stop();
        Close();
        _onDismissed?.Invoke();
    }
}
