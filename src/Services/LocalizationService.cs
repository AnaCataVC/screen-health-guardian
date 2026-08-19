namespace ScreenHealthGuardian.Services;

/// <summary>
/// Service managing dual-language localization (EN / ES).
/// </summary>
public class LocalizationService
{
    private string _currentLanguage = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new()
        {
            ["app_name"] = "Screen Health Guardian",
            ["status_paused"] = "⏸ Paused",
            ["tray_pause"] = "Pause Reminders",
            ["tray_resume"] = "Resume Reminders",
            ["tray_settings"] = "Settings...",
            ["tray_quit"] = "Exit",
            ["alert_look_away_title"] = "Rest Your Eyes",
            ["alert_look_away_msg"] = "Look at an object at least 20 feet (6m) away for 20 seconds to reduce eye strain.",
            ["alert_look_away_btn"] = "I'm looking away (Done)",
            ["alert_posture_title"] = "Check Your Posture",
            ["alert_posture_msg"] = "Straighten your back, relax your shoulders, and keep your feet flat on the floor.",
            ["alert_posture_btn"] = "Posture corrected",
            ["settings_title"] = "Preferences & Settings",
            ["settings_subtitle"] = "Customize interval timers, alerts, and system behaviors",
            ["settings_sec_intervals"] = "TIMER INTERVALS",
            ["settings_eye_rest"] = "Eye Rest Interval:",
            ["settings_posture"] = "Posture Check Interval:",
            ["settings_idle"] = "Idle Inactivity Reset:",
            ["settings_dismiss"] = "Auto-dismiss Timeout:",
            ["settings_sec_preferences"] = "SYSTEM & PREFERENCES",
            ["settings_sound"] = "Play notification sound",
            ["settings_autostart"] = "Start on Windows login",
            ["settings_language"] = "Language:",
            ["settings_cancel"] = "Cancel",
            ["settings_save"] = "Save Preferences",
            ["unit_min"] = "min",
            ["unit_sec"] = "sec",
        },
        ["es"] = new()
        {
            ["app_name"] = "Screen Health Guardian",
            ["status_paused"] = "⏸ Pausado",
            ["tray_pause"] = "Pausar recordatorios",
            ["tray_resume"] = "Reanudar recordatorios",
            ["tray_settings"] = "Ajustes...",
            ["tray_quit"] = "Salir",
            ["alert_look_away_title"] = "Descansa la vista",
            ["alert_look_away_msg"] = "Mira a un objeto a unos 6 metros (20 pies) de distancia durante 20 segundos para reducir la fatiga ocular.",
            ["alert_look_away_btn"] = "Hecho, descansando la vista",
            ["alert_posture_title"] = "Revisa tu postura",
            ["alert_posture_msg"] = "Endereza la espalda, relaja los hombros y apoya ambos pies en el suelo.",
            ["alert_posture_btn"] = "Postura corregida",
            ["settings_title"] = "Ajustes y Preferencias",
            ["settings_subtitle"] = "Personaliza los intervalos de descanso, alertas y comportamientos",
            ["settings_sec_intervals"] = "INTERVALOS DE TIEMPO",
            ["settings_eye_rest"] = "Descanso visual:",
            ["settings_posture"] = "Revisión postural:",
            ["settings_idle"] = "Reinicio por inactividad:",
            ["settings_dismiss"] = "Cierre automático de alerta:",
            ["settings_sec_preferences"] = "SISTEMA Y PREFERENCIAS",
            ["settings_sound"] = "Reproducir sonido en alertas",
            ["settings_autostart"] = "Iniciar con Windows",
            ["settings_language"] = "Idioma:",
            ["settings_cancel"] = "Cancelar",
            ["settings_save"] = "Guardar Cambios",
            ["unit_min"] = "min",
            ["unit_sec"] = "seg",
        }
    };

    public string CurrentLanguage => _currentLanguage;

    public void SetLanguage(string lang)
    {
        _currentLanguage = lang.ToLowerInvariant() switch
        {
            "es" or "spanish" => "es",
            _ => "en"
        };
    }

    public string Get(string key)
    {
        if (Translations.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Translations["en"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }
}
