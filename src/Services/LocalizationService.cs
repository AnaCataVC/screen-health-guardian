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
            ["alert_look_away_resting"] = "Resting eyes... {0}s",
            ["alert_look_away_completed"] = "Rest completed! Great job! 👀",
            ["alert_skip_btn"] = "Skip",
            ["alert_posture_title"] = "Check Your Posture",
            ["alert_posture_msg"] = "Straighten your back, relax your shoulders, and keep your feet flat on the floor.",
            ["alert_posture_btn"] = "Posture corrected",
            ["alert_snooze_btn"] = "Snooze (5m)",
            ["alert_preview_title"] = "Alert Preview",
            ["alert_preview_msg"] = "This is a preview of your alert notification. Visual styling and sound match your preferences.",
            ["settings_window_title"] = "Screen Health Guardian — Preferences",
            ["settings_title"] = "Preferences & Settings",
            ["settings_subtitle"] = "Customize interval timers, alerts, and system behaviors",
            ["settings_sec_habits"] = "HABITS & COMPLIANCE",
            ["settings_tab_weekly"] = "This Week",
            ["settings_tab_lifetime"] = "Lifetime",
            ["settings_habits_compliance"] = "{0:F0}% Compliance",
            ["settings_habits_respected"] = "Respected (20s)",
            ["settings_habits_snoozed"] = "Snoozed",
            ["settings_habits_discarded"] = "Skipped",
            ["settings_habits_reset"] = "Reset Statistics",
            ["settings_habits_reset_confirm"] = "Are you sure you want to reset all habit statistics? This cannot be undone.",
            ["settings_sec_intervals"] = "TIMER INTERVALS",
            ["settings_eye_rest"] = "Eye Rest Interval:",
            ["settings_posture"] = "Posture Check Interval:",
            ["settings_idle"] = "Idle Inactivity Reset:",
            ["settings_dismiss"] = "Auto-dismiss Timeout:",
            ["settings_sec_preferences"] = "SYSTEM & PREFERENCES",
            ["settings_sound"] = "Play notification sound",
            ["settings_autostart"] = "Start on Windows login",
            ["settings_language"] = "Language:",
            ["settings_display_mode"] = "Alert Display:",
            ["settings_display_mode_all"] = "All Monitors",
            ["settings_display_mode_active"] = "Active Screen (Cursor)",
            ["settings_cancel"] = "Cancel",
            ["settings_save"] = "Save Preferences",
            ["settings_preview_alert"] = "Preview Alert",
            ["settings_reset_defaults"] = "Reset Defaults",
            ["settings_err_invalid_values"] = "Please enter valid numbers: Eye Rest (1–480 min), Posture (1–480 min), Idle (1–60 min), Timeout (5–300 sec).",
            ["unit_min"] = "min",
            ["unit_sec"] = "sec",
            ["tray_stats_summary"] = "This week: {0} respected · {1:F0}% compliance"
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
            ["alert_look_away_resting"] = "Descansando la vista... {0}s",
            ["alert_look_away_completed"] = "¡Descanso completado! Excelente trabajo 👀",
            ["alert_skip_btn"] = "Omitir",
            ["alert_posture_title"] = "Revisa tu postura",
            ["alert_posture_msg"] = "Endereza la espalda, relaja los hombros y apoya ambos pies en el suelo.",
            ["alert_posture_btn"] = "Postura corregida",
            ["alert_snooze_btn"] = "Posponer (5m)",
            ["alert_preview_title"] = "Vista Previa de Alerta",
            ["alert_preview_msg"] = "Esta es una vista previa de la notificación. El estilo visual y el sonido coinciden con tus preferencias.",
            ["settings_window_title"] = "Screen Health Guardian — Preferencias",
            ["settings_title"] = "Ajustes y Preferencias",
            ["settings_subtitle"] = "Personaliza los intervalos de descanso, alertas y comportamientos",
            ["settings_sec_habits"] = "HÁBITOS Y ESTADÍSTICAS",
            ["settings_tab_weekly"] = "Esta Semana",
            ["settings_tab_lifetime"] = "Histórico Total",
            ["settings_habits_compliance"] = "{0:F0}% Adherencia",
            ["settings_habits_respected"] = "Respetados (20s)",
            ["settings_habits_snoozed"] = "Pospuestos",
            ["settings_habits_discarded"] = "Omitidos",
            ["settings_habits_reset"] = "Reiniciar estadísticas",
            ["settings_habits_reset_confirm"] = "¿Estás seguro de que deseas reiniciar todas las estadísticas de hábitos? Esta acción no se puede deshacer.",
            ["settings_sec_intervals"] = "INTERVALOS DE TIEMPO",
            ["settings_eye_rest"] = "Descanso visual:",
            ["settings_posture"] = "Revisión postural:",
            ["settings_idle"] = "Reinicio por inactividad:",
            ["settings_dismiss"] = "Cierre automático de alerta:",
            ["settings_sec_preferences"] = "SISTEMA Y PREFERENCIAS",
            ["settings_sound"] = "Reproducir sonido en alertas",
            ["settings_autostart"] = "Iniciar con Windows",
            ["settings_language"] = "Idioma:",
            ["settings_display_mode"] = "Mostrar alertas en:",
            ["settings_display_mode_all"] = "Todos los monitores",
            ["settings_display_mode_active"] = "Pantalla activa (cursor)",
            ["settings_cancel"] = "Cancelar",
            ["settings_save"] = "Guardar Cambios",
            ["settings_preview_alert"] = "Probar Alerta",
            ["settings_reset_defaults"] = "Valores por Defecto",
            ["settings_err_invalid_values"] = "Introduce valores válidos: Descanso (1–480 min), Postura (1–480 min), Inactividad (1–60 min), Cierre (5–300 seg).",
            ["unit_min"] = "min",
            ["unit_sec"] = "seg",
            ["tray_stats_summary"] = "Esta semana: {0} respetados · {1:F0}% adherencia"
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
