using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using ScreenHealthGuardian.Services;
using ScreenHealthGuardian.Views;

namespace ScreenHealthGuardian;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "Local\\ScreenHealthGuardian_SingleInstance_Mutex";

    private ConfigService _configService = null!;
    private LocalizationService _locService = null!;
    private HealthTimerService _timerService = null!;
    private TaskbarIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Single Instance Check via Win32 Mutex
        _singleInstanceMutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running; terminate cleanly.
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // 2. Initialize Services
        _configService = new ConfigService();
        _locService = new LocalizationService();
        _locService.SetLanguage(_configService.Current.Language);

        _timerService = new HealthTimerService(_configService);
        _timerService.OnLookAwayTriggered += ShowLookAwayAlert;
        _timerService.OnPostureTriggered += ShowPostureAlert;
        _timerService.OnStateChanged += UpdateTrayTooltip;

        // 3. Initialize System Tray Icon
        InitializeTrayIcon();

        // 4. Start Health Monitor Timer
        _timerService.Start();
        UpdateTrayTooltip();
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Screen Health Guardian",
            Icon = SystemIcons.Shield
        };

        // Try to load custom icon from resources or disk
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.RelativeOrAbsolute);
            var iconStream = Application.GetResourceStream(uri)?.Stream;
            if (iconStream != null)
            {
                _trayIcon.Icon = new Icon(iconStream);
            }
            else
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = Path.Combine(exeDir, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    _trayIcon.Icon = new Icon(iconPath);
                }
            }
        }
        catch
        {
            _trayIcon.Icon = SystemIcons.Application;
        }

        // Left double click opens Settings
        _trayIcon.DoubleClickCommand = new RelayCommand(_ => ShowSettings());

        RebuildTrayMenu();
        _trayIcon.ForceCreate();
    }

    private void RebuildTrayMenu()
    {
        if (_trayIcon == null) return;

        var menu = new ContextMenu();

        // Status Header Item
        var (lookMin, postMin) = _timerService.GetRemainingMinutes();
        string statusText = _timerService.IsPaused 
            ? _locService.Get("status_paused") 
            : $"👀 {lookMin}min  |  ✨ {postMin}min";

        var statusItem = new MenuItem
        {
            Header = statusText,
            IsEnabled = false,
            FontWeight = FontWeights.Bold
        };
        menu.Items.Add(statusItem);
        menu.Items.Add(new Separator());

        // Pause / Resume Item
        string pauseHeader = _timerService.IsPaused 
            ? _locService.Get("tray_resume") 
            : _locService.Get("tray_pause");

        var pauseItem = new MenuItem { Header = pauseHeader };
        pauseItem.Click += (s, e) =>
        {
            _timerService.TogglePause();
            RebuildTrayMenu();
        };
        menu.Items.Add(pauseItem);

        // Settings Item
        var settingsItem = new MenuItem { Header = _locService.Get("tray_settings") };
        settingsItem.Click += (s, e) => ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        // Exit Item
        var exitItem = new MenuItem { Header = _locService.Get("tray_quit") };
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
    }

    private void UpdateTrayTooltip()
    {
        if (_trayIcon == null) return;

        var (lookMin, postMin) = _timerService.GetRemainingMinutes();
        string status = _timerService.IsPaused
            ? _locService.Get("status_paused")
            : $"👀 {lookMin}m | ✨ {postMin}m";

        _trayIcon.ToolTipText = $"Screen Health Guardian\n{status}";
    }

    private void ShowLookAwayAlert()
    {
        var config = _configService.Current;
        var window = new AlertOverlayWindow(
            emoji: "👀",
            title: _locService.Get("alert_look_away_title"),
            message: _locService.Get("alert_look_away_msg"),
            buttonText: _locService.Get("alert_look_away_btn"),
            accentHex: "#4fc3f7",
            autoDismissSec: config.AlertAutoDismissSec,
            soundEnabled: config.SoundEnabled,
            onDismissed: () => _timerService.AlertDismissed(wasLookAway: true)
        );
        window.Show();
    }

    private void ShowPostureAlert()
    {
        var config = _configService.Current;
        var window = new AlertOverlayWindow(
            emoji: "✨",
            title: _locService.Get("alert_posture_title"),
            message: _locService.Get("alert_posture_msg"),
            buttonText: _locService.Get("alert_posture_btn"),
            accentHex: "#b388ff",
            autoDismissSec: config.AlertAutoDismissSec,
            soundEnabled: config.SoundEnabled,
            onDismissed: () => _timerService.AlertDismissed(wasLookAway: false)
        );
        window.Show();
    }

    private void ShowSettings()
    {
        var win = new SettingsWindow(_configService, _locService, onSaved: () =>
        {
            RebuildTrayMenu();
            UpdateTrayTooltip();
        });
        win.ShowDialog();
    }

    private void ExitApplication()
    {
        _timerService.Stop();
        _trayIcon?.Dispose();
        try
        {
            _singleInstanceMutex?.ReleaseMutex();
        }
        catch
        {
            // Silently handle mutex release if thread does not own it
        }
        _singleInstanceMutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged;
}
