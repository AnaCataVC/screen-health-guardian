using System.Windows.Threading;

namespace ScreenHealthGuardian.Services;

/// <summary>
/// Core orchestrator for activity timers, idle checks, and alert triggers.
/// </summary>
public class HealthTimerService
{
    private readonly ConfigService _configService;
    private readonly DispatcherTimer _timer;

    private double _activeSecondsLookAway = 0;
    private double _activeSecondsPosture = 0;
    private bool _isPaused = false;
    private bool _isAlertActive = false;
    private bool _pendingPosture = false;

    public event Action? OnLookAwayTriggered;
    public event Action? OnPostureTriggered;
    public event Action? OnStateChanged;

    public bool IsPaused => _isPaused;

    public HealthTimerService(ConfigService configService)
    {
        _configService = configService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) => CheckActivity();
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            _activeSecondsLookAway = 0;
            _activeSecondsPosture = 0;
        }
        OnStateChanged?.Invoke();
    }

    public void AlertDismissed(bool wasLookAway)
    {
        _isAlertActive = false;
        if (wasLookAway)
        {
            _activeSecondsLookAway = 0;
            if (_pendingPosture)
            {
                _pendingPosture = false;
                TriggerPostureAlert();
            }
        }
        else
        {
            _activeSecondsPosture = 0;
        }
        OnStateChanged?.Invoke();
    }

    public (int LookAwayMin, int PostureMin) GetRemainingMinutes()
    {
        if (_isPaused) return (0, 0);

        var config = _configService.Current;
        double lookLimit = config.LookAwayIntervalMin * 60.0;
        double postureLimit = config.PostureIntervalMin * 60.0;

        int lookRem = Math.Max(0, (int)((lookLimit - _activeSecondsLookAway) / 60));
        int postRem = Math.Max(0, (int)((postureLimit - _activeSecondsPosture) / 60));

        return (lookRem, postRem);
    }

    private void CheckActivity()
    {
        if (_isPaused) return;

        var config = _configService.Current;

        if (IdleDetector.IsUserActive(config.IdleThresholdSec))
        {
            _activeSecondsLookAway += 1.0;
            _activeSecondsPosture += 1.0;

            // Check Look-away (Eye Rest)
            if (_activeSecondsLookAway >= config.LookAwayIntervalMin * 60.0)
            {
                TriggerLookAwayAlert();
            }

            // Check Posture
            if (_activeSecondsPosture >= config.PostureIntervalMin * 60.0)
            {
                TriggerPostureAlert();
            }
        }
        else
        {
            // User was idle beyond threshold — reset counters
            _activeSecondsLookAway = 0;
            _activeSecondsPosture = 0;
        }

        OnStateChanged?.Invoke();
    }

    private void TriggerLookAwayAlert()
    {
        if (_isAlertActive) return;
        _isAlertActive = true;
        OnLookAwayTriggered?.Invoke();
    }

    private void TriggerPostureAlert()
    {
        if (_isAlertActive)
        {
            _pendingPosture = true;
            _activeSecondsPosture = 0;
            return;
        }
        _isAlertActive = true;
        _activeSecondsPosture = 0;
        OnPostureTriggered?.Invoke();
    }
}
