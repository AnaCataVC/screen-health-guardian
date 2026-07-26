# Work Health Timer 🩺

![Python](https://img.shields.io/badge/Python-3.11-3776AB?style=flat-square&logo=python&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=flat-square&logo=windows&logoColor=white)
![PyInstaller](https://img.shields.io/badge/PyInstaller-Packaged-4D7797?style=flat-square&logo=python&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)

[English](#english) | [Español](#español)

---

## English

### Project Description
A lightweight Windows desktop application that sends periodic health alerts while you work. It detects real user activity before triggering reminders, so you only get notified when you've actually been working.

### Features

| Alert | Default Interval | Purpose |
|---|---|---|
| 👀 **Eye Rest** | 20 minutes | 20-20-20 rule — look 20 feet away for 20 seconds |
| 🧘 **Posture Check** | 60 minutes | Sit up straight reminder |

- **Activity Detection**: Only counts active time (keyboard/mouse). If you're idle for 2+ minutes, timers pause automatically.
- **System Tray**: Heart icon in the notification area with a menu to pause, configure, or quit.
- **Configurable**: All intervals, idle threshold, sound, and auto-start are adjustable via the Settings window.
- **Auto-Start with Windows**: Optional — configurable from Settings.
- **Sound Alerts**: Optional notification sound (disabled by default).
- **Standalone `.exe`**: No Python installation required to run.

### Installation

#### Option 1: Download the Installer (Recommended)
1. Download `WorkHealthTimer-Setup.exe` from the [Releases](../../releases) page.
2. Run the installer and follow the instructions.
3. A heart icon will appear in your system tray automatically. Right-click the tray icon → **Settings** to customize intervals.

#### Option 2: Run from Source
```bash
# Clone the repository
git clone <repo-url>
cd work-health-timer

# Install dependencies
pip install -r requirements.txt

# Run the application
python src/main.py

# Or run without console (Windows)
pythonw src/main.py
```

#### Building the Installer yourself
You will need **PyInstaller** and **Inno Setup 6** installed.
```bash
# Install PyInstaller
pip install pyinstaller

# Run the build script
build.bat
```
The script will first compile the Python code and then generate the professional installer at `dist/WorkHealthTimer-Setup.exe`.

### Configuration

Settings are stored in `%APPDATA%/WorkHealthTimer/config.json` and can be edited via the Settings window (right-click tray icon → ⚙ Settings).

| Setting | Default | Description |
|---|---|---|
| `look_away_interval_min` | `20` | Minutes between eye rest alerts |
| `posture_interval_min` | `60` | Minutes between posture alerts |
| `idle_threshold_sec` | `120` | Seconds of inactivity to pause timers |
| `alert_auto_dismiss_sec` | `30` | Seconds before alerts auto-close |
| `sound_enabled` | `false` | Play Windows notification sound |
| `auto_start` | `false` | Start with Windows |

### Technologies Used

| Component | Technology |
|---|---|
| Language | Python 3.11 |
| Idle Detection | `ctypes` + Windows API (`GetLastInputInfo`) |
| System Tray | `pystray` + `Pillow` |
| Alert Overlays | `tkinter` (built-in) |
| Configuration | JSON (built-in) |
| Packaging | PyInstaller |

### Key Learnings
Building this project provided valuable insights into developing desktop applications with Python, specifically targeting the Windows ecosystem:
- **Native Windows Integration:** Learning how to interact directly with the Windows API using `ctypes` (e.g., `GetLastInputInfo`) to accurately detect user idle time.
- **Resource Optimization:** Designing a background application that consumes minimal system resources by avoiding busy-waiting and focusing only on essential core features to prevent feature creep.
- **Packaging and Distribution:** Overcoming the challenges of bundling a Python application into a standalone `.exe` using PyInstaller, specifically handling hidden console (`--windowed`) environments.
- **Concurrency & IPC Quirks:** Managing single-instance locks and discovering that traditional Windows Mutexes can produce false positives in packaged environments, leading to the implementation of a robust TCP socket-based locking mechanism.
- **Tkinter Threading:** Handling the complexities of updating Tkinter UI elements from background threads (like a `pystray` system tray icon) using thread-safe queues and managing window states (`withdrawn` vs `transient`).

### Project Structure
```text
work-health-timer/
├── src/
│   ├── main.py            # Entry point with logging setup
│   ├── app.py             # Main orchestrator class
│   ├── idle_detector.py   # Windows API idle detection (ctypes)
│   ├── alert_overlay.py   # Tkinter overlay alert windows
│   ├── tray_icon.py       # System tray icon (pystray)
│   ├── config_manager.py  # JSON config load/save
│   └── constants.py       # Default values and color theme
├── requirements.txt       # Python dependencies
├── build.bat              # PyInstaller build script
├── run.bat                # Run without console (dev mode)
└── README.md
```

---

## Español

### Descripción del Proyecto
Una aplicación de escritorio ligera para Windows que envía alertas periódicas de salud mientras trabajas. Detecta actividad real del usuario antes de lanzar los recordatorios, así que solo recibes notificaciones cuando realmente has estado trabajando.

### Características

| Alerta | Intervalo por Defecto | Propósito |
|---|---|---|
| 👀 **Descanso Visual** | 20 minutos | Regla 20-20-20 — mirar a 20 pies (6m) por 20 segundos |
| 🧘 **Postura** | 60 minutos | Recordatorio para mantener la espalda recta |

- **Detección de Actividad**: Solo cuenta el tiempo de actividad (teclado/ratón). Si no hay actividad por más de 2 minutos, los temporizadores se pausan automáticamente.
- **Bandeja del Sistema**: Ícono de corazón en el área de notificaciones con un menú para pausar, configurar o salir.
- **Configurable**: Todos los intervalos, umbral de inactividad, sonido y auto-inicio son ajustables desde la ventana de Ajustes.
- **Auto-Inicio con Windows**: Opcional — configurable desde Ajustes.
- **Alertas Sonoras**: Sonido de notificación opcional (desactivado por defecto).
- **`.exe` Independiente**: No requiere instalación de Python para ejecutarse.

### Instalación

#### Opción 1: Descargar el Instalador (Recomendado)
1. Descarga `WorkHealthTimer-Setup.exe` desde la página de [Releases](../../releases).
2. Ejecuta el instalador y sigue las instrucciones.
3. Un ícono de corazón aparecerá en la bandeja del sistema. Haz clic derecho en él → **Settings** para personalizar los intervalos.

#### Opción 2: Ejecutar desde el Código Fuente
```bash
# Clonar el repositorio
git clone <repo-url>
cd work-health-timer

# Instalar dependencias
pip install -r requirements.txt

# Ejecutar la aplicación
python src/main.py

# O ejecutar sin consola (Windows)
pythonw src/main.py
```

#### Construir el Instalador tú mismo
Necesitarás tener instalados **PyInstaller** e **Inno Setup 6**.
```bash
# Instalar PyInstaller
pip install pyinstaller

# Ejecutar el script de construcción
build.bat
```
El script primero compilará el código Python y luego generará el instalador profesional en `dist/WorkHealthTimer-Setup.exe`.

### Configuración

Los ajustes se guardan en `%APPDATA%/WorkHealthTimer/config.json` y se pueden editar a través de la ventana de Ajustes (clic derecho en el ícono de bandeja → ⚙ Settings).

| Ajuste | Por Defecto | Descripción |
|---|---|---|
| `look_away_interval_min` | `20` | Minutos entre alertas de descanso visual |
| `posture_interval_min` | `60` | Minutos entre alertas de postura |
| `idle_threshold_sec` | `120` | Segundos de inactividad para pausar contadores |
| `alert_auto_dismiss_sec` | `30` | Segundos antes de que las alertas se cierren solas |
| `sound_enabled` | `false` | Reproducir sonido de notificación de Windows |
| `auto_start` | `false` | Iniciar junto con Windows |

### Tecnologías Utilizadas

| Componente | Tecnología |
|---|---|
| Lenguaje | Python 3.11 |
| Detección de Inactividad | `ctypes` + Windows API (`GetLastInputInfo`) |
| Bandeja del Sistema | `pystray` + `Pillow` |
| Ventanas de Alerta | `tkinter` (incorporado) |
| Configuración | JSON (incorporado) |
| Empaquetado | PyInstaller |

### Aprendizajes Clave
Desarrollar este proyecto ofreció grandes enseñanzas sobre la creación de aplicaciones de escritorio con Python, específicamente para el ecosistema de Windows:
- **Integración Nativa en Windows:** Aprender a interactuar directamente con la API de Windows mediante `ctypes` (ej. `GetLastInputInfo`) para detectar con precisión el tiempo de inactividad del usuario.
- **Optimización de Recursos:** Diseñar una aplicación en segundo plano que consuma los mínimos recursos del sistema, manteniendo la arquitectura simple y enfocándose solo en el objetivo principal sin añadir configuraciones innecesarias.
- **Empaquetado y Distribución:** Superar los retos de compilar una aplicación Python en un `.exe` independiente usando PyInstaller, especialmente al lidiar con entornos sin consola (`--windowed`).
- **Peculiaridades de Concurrencia:** Manejar candados de instancia única y descubrir que los Mutex de Windows pueden dar falsos positivos en entornos empaquetados, lo que llevó a implementar un mecanismo infalible basado en sockets TCP.
- **Subprocesos en Tkinter:** Manejar la complejidad de actualizar la interfaz de Tkinter desde hilos secundarios (como el ícono de bandeja `pystray`) usando colas seguras para hilos y controlando los estados de las ventanas.

### Estructura del Proyecto
```text
work-health-timer/
├── src/
│   ├── main.py            # Entry point with logging setup
│   ├── app.py             # Main orchestrator class
│   ├── idle_detector.py   # Windows API idle detection (ctypes)
│   ├── alert_overlay.py   # Tkinter overlay alert windows
│   ├── tray_icon.py       # System tray icon (pystray)
│   ├── config_manager.py  # JSON config load/save
│   └── constants.py       # Default values and color theme
├── requirements.txt       # Python dependencies
├── build.bat              # PyInstaller build script
├── run.bat                # Run without console (dev mode)
└── README.md
```

---

## License / Licencia
MIT
