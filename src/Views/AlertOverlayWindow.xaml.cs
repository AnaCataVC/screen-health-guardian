using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScreenHealthGuardian.Views;

public partial class AlertOverlayWindow : Window
{
    private readonly DispatcherTimer _dismissTimer;
    private readonly int _totalSeconds;
    private int _remainingSeconds;
    private readonly Action? _onDismissed;
    private bool _dismissed = false;

    public AlertOverlayWindow(
        string emoji,
        string title,
        string message,
        string buttonText,
        string accentHex,
        int autoDismissSec = 30,
        bool soundEnabled = true,
        Action? onDismissed = null)
    {
        InitializeComponent();

        _totalSeconds = autoDismissSec;
        _remainingSeconds = autoDismissSec;
        _onDismissed = onDismissed;

        EmojiBlock.Text = emoji;
        TitleBlock.Text = title;
        MessageBlock.Text = message;
        DismissButton.Content = buttonText;

        var accentBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(accentHex)!;
        TitleBlock.Foreground = accentBrush;
        AutoDismissProgress.Foreground = accentBrush;

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

    public void Dismiss()
    {
        if (_dismissed) return;
        _dismissed = true;

        _dismissTimer.Stop();
        Close();
        _onDismissed?.Invoke();
    }
}
