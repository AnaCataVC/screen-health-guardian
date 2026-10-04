<p align="center">
  <img src="icon.png" alt="screen-health-guardian Logo" width="120" />
</p>

# Screen Health Guardian

[English](README.md) | [Español](README.es.md)

![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20Fluent-blue?style=flat)
![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?style=flat&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=flat)

---



### Descripción del Proyecto
Una aplicación nativa de alto rendimiento y ultraligera para Windows diseñada para promover la salud visual y postural. Detecta la actividad real del usuario mediante APIs nativas de Win32, pausando los temporizadores automáticamente cuando el usuario está ausente.

### Características Principales

| Alerta | Intervalo Predeterminado | Propósito |
|---|---|---|
| 👀 **Descanso Visual** | 20 minutos | Regla 20-20-20 (mirar a 6 metros por 20 segundos) |
| 🧘 **Revisión Postural** | 45 minutos | Recordatorio para enderezar espalda y relajar hombros |

- **Detección Nativa de Inactividad**: Consulta directa a `GetLastInputInfo` (Win32) sin consumir CPU.
- **Overlays con Aceleración por GPU**: Ventanas WPF translúcidas con barra regresiva de auto-cierre.
- **Bandeja del Sistema**: Integración fluida con `H.NotifyIcon`, menús contextuales y tooltips dinámicos.
- **Configuración Persistente**: Ajustes guardados en formato JSON en `%APPDATA%\ScreenHealthGuardian\config.json`.
- **Sin Falsos Positivos de Antivirus**: Binario nativo PE compilado con .NET.
- **Bilingüe**: Cambio inmediato de idioma entre Español e Inglés.

### 💡 Aprendizajes Clave y Evolución Arquitectónica (Python ➡️ C# / .NET)

La aplicación nació inicialmente como un proyecto en Python (Tkinter + ctypes + PyInstaller) y fue refactorizada y migrada estratégicamente a **C# / .NET 9 (WPF)**. Esta transición aportó importantes aprendizajes de ingeniería de software:

1. **Erradicación de Falsos Positivos de Antivirus (Distribución Confiable)**:
   - *Problema en Python*: El empaquetador de PyInstaller descomprime archivos en `%TEMP%`, disparando alertas heurísticas recurrentes en Windows Defender y entornos corporativos.
   - *Solución en .NET*: La compilación a ejecutable nativo Portable Executable (PE) mediante `dotnet publish` genera un binario firmado limpiamente por el compilador, eliminando bloqueos y permitiendo arranques instantáneos.

2. **Optimización de Recursos para Daemons Residentes 24/7**:
   - *Huella de Memoria*: Reducción del consumo continuo de RAM de **~45–55 MB** (runtime de CPython + Tcl/Tk) a solo **~12–16 MB** en .NET.
   - *Uso de CPU*: Eliminación de la sobrecarga del GIL de Python, manteniendo el consumo en reposo por debajo del **0.1% de CPU**.

3. **Calidad Visual y Aceleración por GPU**:
   - Los canvas de Tkinter carecen de suavizado de bordes (anti-aliasing) y sufren problemas de escalado en configuraciones multimonitor con distintos DPI.
   - La adopción de XAML en WPF proporcionó aceleración por hardware completa, ventanas translúcidas fluidas (`AllowsTransparency="True"`) y animaciones con composición DWM acordes a las líneas de diseño de Windows 11.

4. **Integración con el Sistema Operativo y Concurrencia Robusta**:
   - Sustitución del bloqueo por socket TCP de Python por un `System.Threading.Mutex` nativo de Win32, evitando alertas de Firewall y puertos ocupados.
   - Integración nativa de la bandeja del sistema con `H.NotifyIcon.Wpf`, eliminando colas de eventos cruzadas entre hilos.

### Compilación y Ejecución

#### Requisitos
- Windows 10/11
- [.NET 8.0 o 9.0 SDK](https://dotnet.microsoft.com/download)

```powershell
# Ejecutar en modo desarrollo
dotnet run --project src/ScreenHealthGuardian.csproj

# Compilar binario standalone e instalador
./build.bat
```

---

## Licencia

Este proyecto está bajo la Licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.

