using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using ScreenHealthGuardian.Models;
using ScreenHealthGuardian.Services;
using ScreenHealthGuardian.Views;

namespace ScreenHealthGuardian;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;
    private const string MutexName = "Local\\ScreenHealthGuardian_SingleInstance_Mutex";

    private ConfigService _configService = null!;
    private LocalizationService _locService = null!;
    private StatsService _statsService = null!;
    private HealthTimerService _timerService = null!;
    private TaskbarIcon? _trayIcon;
    private MenuItem? _trayStatusMenuItem;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 0. Global Exception Handling & Diagnostic Logging (desktop_app_stability.md)
        ConfigureGlobalExceptionHandling();

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
        _statsService = new StatsService();
        _statsService.OnStatsUpdated += UpdateTrayStatusAndTooltip;

        _timerService = new HealthTimerService(_configService);
        _timerService.OnLookAwayTriggered += ShowLookAwayAlert;
        _timerService.OnPostureTriggered += ShowPostureAlert;
        _timerService.OnStateChanged += UpdateTrayStatusAndTooltip;

        // 3. Initialize System Tray Icon
        InitializeTrayIcon();

        // 4. Start Health Monitor Timer
        _timerService.Start();
        UpdateTrayStatusAndTooltip();
    }

    private void ConfigureGlobalExceptionHandling()
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenHealthGuardian",
            "Logs"
        );

        void WriteLog(string prefix, Exception? ex)
        {
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string crashPath = Path.Combine(logDir, "crash.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{prefix}] {ex?.GetType().FullName}: {ex?.Message}\n{ex?.StackTrace}\n";
                if (ex?.InnerException != null)
                {
                    logEntry += $"Inner: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}\n";
                }
                logEntry += new string('-', 80) + "\n";
                File.AppendAllText(crashPath, logEntry);
            }
            catch
            {
                // Silently avoid cascading crash if logger fails
            }
        }

        DispatcherUnhandledException += (s, e) =>
        {
            WriteLog("DispatcherUnhandledException", e.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            WriteLog("AppDomainUnhandledException", e.ExceptionObject as Exception);
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            WriteLog("TaskSchedulerUnobservedTaskException", e.Exception);
            e.SetObserved();
        };
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
        menu.Opened += (s, e) => UpdateTrayStatusHeader();

        // Status Header Item
        _trayStatusMenuItem = new MenuItem
        {
            IsEnabled = false,
            FontWeight = FontWeights.Bold
        };
        UpdateTrayStatusHeader();
        menu.Items.Add(_trayStatusMenuItem);
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

    private void UpdateTrayStatusHeader()
    {
        if (_trayStatusMenuItem == null) return;

        var (lookMin, postMin) = _timerService.GetRemainingMinutes();
        string statusText = _timerService.IsPaused 
            ? _locService.Get("status_paused") 
            : $"👀 {lookMin}min  |  ✨ {postMin}min";

        _trayStatusMenuItem.Header = statusText;
    }

    private void UpdateTrayStatusAndTooltip()
    {
        UpdateTrayStatusHeader();

        if (_trayIcon == null) return;

        var (lookMin, postMin) = _timerService.GetRemainingMinutes();
        string status = _timerService.IsPaused
            ? _locService.Get("status_paused")
            : $"👀 {lookMin}m | ✨ {postMin}m";

        var weekly = _statsService.GetWeeklySummary();
        string weeklyStats = string.Format(
            _locService.Get("tray_stats_summary"),
            weekly.EyeRest.Respected,
            weekly.EyeRest.ComplianceRate
        );

        _trayIcon.ToolTipText = $"Screen Health Guardian\n{status}\n{weeklyStats}";
    }

    private readonly List<AlertOverlayWindow> _activeOverlays = new();
    private int _activeOverlaySessionId = 0;

    private void ShowMultiMonitorAlert(
        string emoji,
        string title,
        string message,
        string buttonText,
        string accentHex,
        bool wasLookAway,
        bool isPreview = false,
        string? overrideDisplayMode = null,
        bool? overrideSound = null,
        int? overrideDismissSec = null)
    {
        var config = _configService.Current;
        string displayMode = overrideDisplayMode ?? config.DisplayMode ?? "all";
        bool soundEnabled = overrideSound ?? config.SoundEnabled;
        int autoDismissSec = overrideDismissSec ?? config.AlertAutoDismissSec;

        IReadOnlyList<DisplayInfo> displays = (displayMode.ToLowerInvariant() == "active")
            ? new[] { DisplayManager.GetCursorDisplay() }
            : DisplayManager.GetDisplays();

        // Close any currently active overlays before spawning new session
        DismissAllActiveOverlays();

        int currentSessionId = Interlocked.Increment(ref _activeOverlaySessionId);
        int isDismissedFlag = 0;

        void OnSessionCompleted()
        {
            if (Interlocked.Exchange(ref isDismissedFlag, 1) == 0)
            {
                if (!isPreview)
                {
                    if (wasLookAway)
                    {
                        _statsService.RecordEyeRest(BreakOutcome.Respected);
                    }
                    else
                    {
                        _statsService.RecordPosture(BreakOutcome.Respected);
                    }
                    _timerService.AlertDismissed(wasLookAway);
                }
                DismissAllActiveOverlays();
            }
        }

        void OnSessionSkipped()
        {
            if (Interlocked.Exchange(ref isDismissedFlag, 1) == 0)
            {
                if (!isPreview)
                {
                    if (wasLookAway)
                    {
                        _statsService.RecordEyeRest(BreakOutcome.Discarded);
                    }
                    else
                    {
                        _statsService.RecordPosture(BreakOutcome.Discarded);
                    }
                    _timerService.AlertDismissed(wasLookAway);
                }
                DismissAllActiveOverlays();
            }
        }

        void OnSessionSnoozed()
        {
            if (Interlocked.Exchange(ref isDismissedFlag, 1) == 0)
            {
                if (!isPreview)
                {
                    if (wasLookAway)
                    {
                        _statsService.RecordEyeRest(BreakOutcome.Snoozed);
                    }
                    else
                    {
                        _statsService.RecordPosture(BreakOutcome.Snoozed);
                    }
                    _timerService.AlertSnoozed(wasLookAway);
                }
                DismissAllActiveOverlays();
            }
        }

        bool soundPlayed = false;
        string? snoozeText = isPreview ? null : _locService.Get("alert_snooze_btn");
        bool isGuidedLookAway = wasLookAway && !isPreview;

        foreach (var display in displays)
        {
            bool shouldPlaySound = soundEnabled && !soundPlayed && (display.IsPrimary || displays.Count == 1);
            if (shouldPlaySound)
            {
                soundPlayed = true;
            }

            var window = new AlertOverlayWindow(
                emoji: emoji,
                title: title,
                message: message,
                buttonText: isGuidedLookAway ? _locService.Get("alert_skip_btn") : buttonText,
                accentHex: accentHex,
                targetDisplay: display,
                autoDismissSec: autoDismissSec,
                soundEnabled: shouldPlaySound,
                isGuidedLookAway: isGuidedLookAway,
                restingTextFormat: _locService.Get("alert_look_away_resting"),
                completedText: _locService.Get("alert_look_away_completed"),
                onCompleted: OnSessionCompleted,
                onSkipped: OnSessionSkipped,
                snoozeButtonText: snoozeText,
                onSnoozed: isPreview ? null : OnSessionSnoozed
            );

            _activeOverlays.Add(window);
            window.Show();
        }

        if (soundEnabled && !soundPlayed && _activeOverlays.Count > 0)
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Silently handle sound play failure
            }
        }
    }

    private void DismissAllActiveOverlays()
    {
        if (_activeOverlays.Count == 0) return;

        var overlaysToClose = _activeOverlays.ToList();
        _activeOverlays.Clear();

        foreach (var overlay in overlaysToClose)
        {
            try
            {
                overlay.Dismiss();
            }
            catch
            {
                // Ignore errors closing individual overlay
            }
        }
    }

    private void ShowLookAwayAlert()
    {
        ShowMultiMonitorAlert(
            emoji: "👀",
            title: _locService.Get("alert_look_away_title"),
            message: _locService.Get("alert_look_away_msg"),
            buttonText: _locService.Get("alert_look_away_btn"),
            accentHex: "#4fc3f7",
            wasLookAway: true
        );
    }

    private void ShowPostureAlert()
    {
        ShowMultiMonitorAlert(
            emoji: "✨",
            title: _locService.Get("alert_posture_title"),
            message: _locService.Get("alert_posture_msg"),
            buttonText: _locService.Get("alert_posture_btn"),
            accentHex: "#b388ff",
            wasLookAway: false
        );
    }

    private void ShowPreviewAlert(AppConfig previewConfig)
    {
        ShowMultiMonitorAlert(
            emoji: "👀",
            title: _locService.Get("alert_preview_title"),
            message: _locService.Get("alert_preview_msg"),
            buttonText: _locService.Get("alert_look_away_btn"),
            accentHex: "#4fc3f7",
            wasLookAway: true,
            isPreview: true,
            overrideDisplayMode: previewConfig.DisplayMode,
            overrideSound: previewConfig.SoundEnabled,
            overrideDismissSec: previewConfig.AlertAutoDismissSec
        );
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(
            _configService,
            _locService,
            _statsService,
            onSaved: () =>
            {
                RebuildTrayMenu();
                UpdateTrayStatusAndTooltip();
            },
            onPreviewAlert: previewConfig => ShowPreviewAlert(previewConfig)
        );

        _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void ExitApplication()
    {
        DismissAllActiveOverlays();
        _settingsWindow?.Close();
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
    public event EventHandler? CanExecuteChanged
    {
        add => System.Windows.Input.CommandManager.RequerySuggested += value;
        remove => System.Windows.Input.CommandManager.RequerySuggested -= value;
    }
}
