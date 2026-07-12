"""Internationalization module for Work Health Timer."""

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
        "alert_look_away_btn": "✓  Done, eyes rested",
        "alert_posture_title": "Check Your Posture",
        "alert_posture_msg": "Sit up straight! Shoulders back, feet flat on the floor,\nand screen at eye level.",
        "alert_posture_btn": "✓  Posture corrected",
        "alert_window_title": "Work Health Timer Alert",
        
        # Tray Icon
        "tray_resume": "▶  Resume",
        "tray_pause": "⏸  Pause",
        "tray_settings": "⚙  Settings",
        "tray_quit": "✕  Quit",
        "tray_status_paused": "Paused",
        "tray_status_active": "Active",
        
        # Settings UI
        "settings_title": "⚙  Settings",
        "settings_eye_rest": "👀  Eye rest interval (min)",
        "settings_posture": "🧘  Posture check interval (min)",
        "settings_idle": "⏱  Idle threshold (sec)",
        "settings_dismiss": "⏳  Alert auto-dismiss (sec)",
        "settings_sound": "🔔  Enable notification sound",
        "settings_autostart": "🚀  Start with Windows",
        "settings_language": "🌐  Language",
        "settings_save": "💾  Save Settings",
        
        # Tray Status Formatting
        "status_paused_menu": "⏸  Paused",
    },
    "es": {
        # Alert Overlays
        "alert_look_away_title": "Hora de descansar la vista",
        "alert_look_away_msg": "Mira algo a 6 metros (20 pies) de distancia durante 20 segundos.\n¡Tus ojos te lo agradecerán!",
        "alert_look_away_btn": "✓  Listo, vista descansada",
        "alert_posture_title": "Revisa tu postura",
        "alert_posture_msg": "¡Siéntate derecho! Hombros atrás, pies apoyados en el suelo,\ny la pantalla a la altura de los ojos.",
        "alert_posture_btn": "✓  Postura corregida",
        "alert_window_title": "Alerta de Work Health Timer",
        
        # Tray Icon
        "tray_resume": "▶  Reanudar",
        "tray_pause": "⏸  Pausar",
        "tray_settings": "⚙  Configuración",
        "tray_quit": "✕  Salir",
        "tray_status_paused": "Pausado",
        "tray_status_active": "Activo",
        
        # Settings UI
        "settings_title": "⚙  Configuración",
        "settings_eye_rest": "👀  Intervalo de descanso visual (min)",
        "settings_posture": "🧘  Intervalo de postura (min)",
        "settings_idle": "⏱  Umbral de inactividad (seg)",
        "settings_dismiss": "⏳  Auto-ocultar alerta (seg)",
        "settings_sound": "🔔  Activar sonido de notificación",
        "settings_autostart": "🚀  Iniciar con Windows",
        "settings_language": "🌐  Idioma",
        "settings_save": "💾  Guardar Configuración",
        
        # Tray Status Formatting
        "status_paused_menu": "⏸  Pausado",
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
