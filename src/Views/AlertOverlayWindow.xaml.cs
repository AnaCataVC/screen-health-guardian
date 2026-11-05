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
    private readonly Action? _onCompleted;
    private readonly Action? _onSkipped;
    private readonly Action? _onSnoozed;
    private readonly bool _isGuidedMode;
    private readonly string? _restingTextFormat;
    private readonly string? _completedText;
    private readonly bool _soundEnabled;

    private bool _isCompleted = false;
    private bool _dismissed = false;
    private double _elapsedMs = 0;
    private DispatcherTimer? _graceTimer;

    public AlertOverlayWindow(
        string emoji,
        string title,
        string message,
        string buttonText,
        string accentHex,
        DisplayInfo? targetDisplay = null,
        int autoDismissSec = 30,
        bool soundEnabled = true,
        bool isGuidedLookAway = false,
        string? restingTextFormat = null,
        string? completedText = null,
        Action? onCompleted = null,
        Action? onSkipped = null,
        string? snoozeButtonText = null,
        Action? onSnoozed = null)
    {
        InitializeComponent();

        _isGuidedMode = isGuidedLookAway;
        _totalSeconds = isGuidedLookAway ? 20 : autoDismissSec;
        _restingTextFormat = restingTextFormat;
        _completedText = completedText;
        _soundEnabled = soundEnabled;
        _onCompleted = onCompleted;
        _onSkipped = onSkipped;
        _onSnoozed = onSnoozed;

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

        if (!string.IsNullOrWhiteSpace(snoozeButtonText))
        {
            SnoozeButton.Content = snoozeButtonText;
            SnoozeButton.Visibility = Visibility.Visible;
        }
        else
        {
            SnoozeButton.Visibility = Visibility.Collapsed;
        }

        var accentBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(accentHex)!;
        TitleBlock.Foreground = accentBrush;
        AutoDismissProgress.Foreground = accentBrush;

        if (_isGuidedMode)
        {
            // In guided mode, DismissButton acts as "Skip" CTA with subtle dark button styling
            if (TryFindResource("ModernDarkButton") is Style darkStyle)
            {
                DismissButton.Style = darkStyle;
            }
        }
        else if (string.Equals(accentHex, "#b388ff", StringComparison.OrdinalIgnoreCase))
        {
            if (TryFindResource("ModernSecondaryAccentButton") is Style secondaryStyle)
            {
                DismissButton.Style = secondaryStyle;
            }
            else
            {
                DismissButton.Background = accentBrush;
            }
        }

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
        UpdateProgressLabel(_totalSeconds);

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
            AutoDismissLabel.Visibility = Visibility.Collapsed;
        }
    }

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

    private void UpdateProgressLabel(int remainingSec)
    {
        if (_isGuidedMode && !string.IsNullOrWhiteSpace(_restingTextFormat))
        {
            AutoDismissLabel.Text = string.Format(_restingTextFormat, remainingSec);
        }
        else
        {
            AutoDismissLabel.Text = $"{remainingSec}s";
        }
    }

    private void DismissTimer_Tick(object? sender, EventArgs e)
    {
        _elapsedMs += 100;
        double totalMs = _totalSeconds * 1000.0;
        double remaining = totalMs - _elapsedMs;

        if (remaining <= 0)
        {
            _dismissTimer.Stop();

            if (_isGuidedMode && !_isCompleted)
            {
                TransitionToCompletedState();
            }
            else
            {
                Complete();
            }
        }
        else
        {
            AutoDismissProgress.Value = (remaining / totalMs) * _totalSeconds;
            int remainingSec = (int)Math.Ceiling(remaining / 1000.0);
            UpdateProgressLabel(remainingSec);
        }
    }

    private void TransitionToCompletedState()
    {
        _isCompleted = true;

        if (_soundEnabled)
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Silently ignore audio playback errors
            }
        }

        EmojiBlock.Text = "🎉";
        CenterEmojiOpticalBounds("🎉");

        TitleBlock.Text = _completedText ?? "Rest completed! Great job! 👀";
        MessageBlock.Text = string.Empty;
        MessageBlock.Visibility = Visibility.Collapsed;

        SnoozeButton.Visibility = Visibility.Collapsed;
        DismissButton.Content = "OK";
        if (TryFindResource("ModernPrimaryButton") is Style primaryStyle)
        {
            DismissButton.Style = primaryStyle;
        }

        AutoDismissProgress.Value = _totalSeconds;
        AutoDismissLabel.Text = "✓";

        // 2-second grace period before auto-closing
        _graceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _graceTimer.Tick += (s, args) =>
        {
            _graceTimer.Stop();
            Complete();
        };
        _graceTimer.Start();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isGuidedMode && !_isCompleted)
        {
            Skip();
        }
        else
        {
            Complete();
        }
    }

    private void SnoozeButton_Click(object sender, RoutedEventArgs e)
    {
        Snooze();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_isGuidedMode && !_isCompleted)
            {
                Skip();
            }
            else
            {
                Complete();
            }
        }
        else if (_isCompleted)
        {
            Complete();
        }
    }

    public void Complete()
    {
        if (_dismissed) return;
        _dismissed = true;

        _dismissTimer.Stop();
        _graceTimer?.Stop();
        Close();
        _onCompleted?.Invoke();
    }

    public void Skip()
    {
        if (_dismissed) return;
        _dismissed = true;

        _dismissTimer.Stop();
        _graceTimer?.Stop();
        Close();
        _onSkipped?.Invoke();
    }

    public void Snooze()
    {
        if (_dismissed) return;
        _dismissed = true;

        _dismissTimer.Stop();
        _graceTimer?.Stop();
        Close();
        _onSnoozed?.Invoke();
    }

    public void Dismiss()
    {
        // General safe dismiss
        if (_isGuidedMode && !_isCompleted)
        {
            Skip();
        }
        else
        {
            Complete();
        }
    }
}
