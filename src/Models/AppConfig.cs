using System.Text.Json.Serialization;

namespace ScreenHealthGuardian.Models;

/// <summary>
/// Application settings model stored in %APPDATA%/ScreenHealthGuardian/config.json.
/// </summary>
public class AppConfig
{
    [JsonPropertyName("look_away_interval_min")]
    public int LookAwayIntervalMin { get; set; } = 20;

    [JsonPropertyName("posture_interval_min")]
    public int PostureIntervalMin { get; set; } = 45;

    [JsonPropertyName("idle_threshold_sec")]
    public int IdleThresholdSec { get; set; } = 120;

    [JsonPropertyName("alert_auto_dismiss_sec")]
    public int AlertAutoDismissSec { get; set; } = 30;

    [JsonPropertyName("sound_enabled")]
    public bool SoundEnabled { get; set; } = true;

    [JsonPropertyName("auto_start")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";
}
