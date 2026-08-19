"""Internationalization module for Screen Health Guardian."""

import logging

logger = logging.getLogger(__name__)

# Current selected language
_current_language = "en"

# Translation dictionary
_translations = {
    "en": {
        # Alert Overlays
        "alert_look_away_title": "Time to Rest Your Eyes",
        "alert_look_away_msg": "Look at something 6 meters (20 feet) away for 20 seconds.\nYour eyes will thank you!",
        "alert_look_away_btn": "Done, eyes rested",
        "alert_posture_title": "Check Your Posture",
        "alert_posture_msg": "Sit up straight! Shoulders back,\nfeet flat on the floor and screen at eye level.",
        "alert_posture_btn": "Posture corrected",
        "alert_window_title": "Screen Health Guardian Alert",
        
        # Tray Icon
        "tray_resume": "▶  Resume",
        "tray_pause": "Pause",
        "tray_settings": "Settings",
        "tray_quit": "✕  Quit",
        "tray_status_paused": "Paused",
        "tray_status_active": "Active",
        
        # Settings UI
        "settings_title": "Settings",
        "settings_subtitle": "Configure timers and application preferences",
        "settings_sec_intervals": "TIMER INTERVALS",
        "settings_sec_preferences": "PREFERENCES & SYSTEM",
        "settings_eye_rest": "👀  Eye rest interval",
        "settings_posture": "✨  Posture check interval",
        "settings_idle": "⏱  Idle threshold",
        "settings_dismiss": "⏳  Alert auto-dismiss",
        "settings_sound": "🔔  Enable notification sound",
        "settings_autostart": "🚀  Start with Windows",
        "settings_language": "🌐  Language",
        "settings_save": "Save Settings",
        "settings_cancel": "Cancel",
        "unit_min": "min",
        "unit_sec": "sec",
        
        # Tray Status Formatting
        "status_paused_menu": "Paused",
    },
    "es": {
        # Alert Overlays
        "alert_look_away_title": "Hora de descansar la vista",
        "alert_look_away_msg": "Mira algo a 6 metros (20 pies) de distancia durante 20 segundos.\n¡Tus ojos te lo agradecerán!",
        "alert_look_away_btn": "Listo, vista descansada",
        "alert_posture_title": "Revisa tu postura",
        "alert_posture_msg": "¡Mantén la espalda recta! Hombros atrás,\npies apoyados en el suelo y la pantalla a la altura de los ojos.",
        "alert_posture_btn": "Postura corregida",
        "alert_window_title": "Alerta de Screen Health Guardian",
        
        # Tray Icon
        "tray_resume": "▶  Reanudar",
        "tray_pause": "Pausar",
        "tray_settings": "Configuración",
        "tray_quit": "✕  Salir",
        "tray_status_paused": "Pausado",
        "tray_status_active": "Activo",
        
        # Settings UI
        "settings_title": "Configuración",
        "settings_subtitle": "Configura tus descansos y preferencias",
        "settings_sec_intervals": "INTERVALOS Y TIEMPOS",
        "settings_sec_preferences": "PREFERENCIAS Y SISTEMA",
        "settings_eye_rest": "👀  Descanso visual",
        "settings_posture": "✨  Revisión de postura",
        "settings_idle": "⏱  Umbral de inactividad",
        "settings_dismiss": "⏳  Auto-ocultar alerta",
        "settings_sound": "🔔  Sonido de notificación",
        "settings_autostart": "🚀  Iniciar con Windows",
        "settings_language": "🌐  Idioma",
        "settings_save": "Guardar Configuración",
        "settings_cancel": "Cancelar",
        "unit_min": "min",
        "unit_sec": "seg",
        
        # Tray Status Formatting
        "status_paused_menu": "Pausado",
    }
}

def set_language(lang: str) -> None:
    """Set the current language for translations."""
    global _current_language
    if lang in _translations:
        _current_language = lang
    else:
        logger.warning(f"Language '{lang}' not supported. Falling back to English.")
        _current_language = "en"

def get_language() -> str:
    """Get the current language code."""
    return _current_language

def t(key: str) -> str:
    """Translate a given key into the current language."""
    if key in _translations[_current_language]:
        return _translations[_current_language][key]
    
    # Fallback to English if key is missing in the target language
    if key in _translations["en"]:
        logger.warning(f"Translation key '{key}' missing for '{_current_language}'. Using fallback.")
        return _translations["en"][key]
    
    logger.error(f"Translation key '{key}' not found.")
    return key
