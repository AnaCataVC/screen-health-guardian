# Avance del Proyecto — Work Health Timer

Este documento detalla el estado actual de la aplicación y cómo puedes usarla o compilarla.

## Estado Actual ✅

La aplicación está **funcional**, tanto en modo desarrollo como empaquetada en modo `--windowed` (silencioso, sin consola). El instalador de Inno Setup genera un `Setup.exe` funcional.

### Resolución del problema de inicialización

1. **Falsos positivos de Mutex (resuelto):** Se usaba `win32event.CreateMutex` para prevenir múltiples instancias, pero bajo PyInstaller `GetLastError()` devolvía siempre `183`, haciendo creer a la app que ya estaba corriendo y cerrándose al instante. Se reemplazó por un candado basado en un socket TCP local (`127.0.0.1:49133`): si el puerto está en uso, ya hay una instancia. Infalible bajo PyInstaller.

2. **Crash silencioso en `--windowed` (resuelto):** El colapso instantáneo se debía a que el `.exe`/instalador en uso estaba compilado de una versión **anterior** al arreglo del socket-lock. El código fuente ya estaba corregido, pero nunca se recompiló un ejecutable funcional encima. Al reconstruir desde el fuente actual, la app inicia correctamente en modo `--windowed`, crea el candado de red, levanta el ícono de bandeja y escribe `app.log`. Verificado lanzando el `.exe` de forma desacoplada (como el doble clic o la carpeta Startup): arranca y permanece estable.

3. **Red de seguridad para crashes (`faulthandler`):** `src/main.py` habilita `faulthandler` y envuelve `main()` en un `try/except` de nivel superior que vuelca cualquier error a `%APPDATA%\WorkHealthTimer\crash.log`. En modo `--windowed` no hay consola, así que sin esto un error temprano desaparecería en silencio.

4. **Auto-inicio en apps congeladas (corregido):** El registro de auto-inicio usaba la lógica `python.exe → pythonw.exe`, incorrecta para un `.exe` empaquetado (donde `sys.executable` ya es el propio exe). Ahora, si `sys.frozen`, se registra directamente `"WorkHealthTimer.exe"`.

5. **Ventanas invisibles: Settings no abría y las alertas no se veían (corregido):** La ventana de Ajustes (`_create_settings_window` en `app.py`) y el overlay de alerta (`_create_overlay` en `alert_overlay.py`) llamaban a `transient(self.root)`. Como el `root` principal está oculto (`withdraw()`), una ventana `transient` de un master oculto **hereda el estado `withdrawn` y nunca se hace visible** en Windows. El código funcionaba (la ventana se creaba, la alerta se disparaba y se registraba en el log), pero la ventana quedaba invisible (`winfo_ismapped() == 0`). Se eliminó `transient()` de ambos lugares. En el overlay `transient` era además redundante: `overrideredirect(True)` ya lo mantiene fuera de la barra de tareas. Verificado en el `.exe` empaquetado: Settings y overlay quedan `mapped=1, viewable=1`.

---

## Cómo Ejecutar y Probar (Modo Desarrollo)

1. Tener Python 3.11 instalado.
2. Abrir la consola en la raíz del proyecto.
3. Ejecutar `python src/main.py`.
4. El ícono del corazón aparece en la bandeja del sistema; se puede interactuar (Ajustes, Pausar, cambiar intervalos).

---

## Cómo Compilar

1. Ejecutar `build.bat` (compila el ejecutable con PyInstaller y, si Inno Setup está instalado, genera el instalador).
2. Resultados:
   - Ejecutable portable: `dist\WorkHealthTimer\WorkHealthTimer.exe`
   - Instalador: `dist\WorkHealthTimer-Setup.exe`

> Importante: tras cualquier cambio en `src/`, hay que **recompilar** antes de distribuir. El instalador empaqueta lo que haya en `dist\WorkHealthTimer\` al momento de compilar.

---

## Diagnóstico de problemas

Si la app no inicia en la máquina de un usuario, revisar:
- `%APPDATA%\WorkHealthTimer\crash.log` — volcado de cualquier error fatal de inicio.
- `%APPDATA%\WorkHealthTimer\app.log` — log normal de operación.

---

## Mejoras de Interfaz y Diseño (UI/UX)

1. **Refactorización de Botones**: Se migró de botones dibujados en `Canvas` (que presentaban problemas de anti-aliasing y texto cortado en Windows) a botones nativos `tk.Button` con padding explícito (`ipadx`, `ipady`), logrando un acabado nítido y profesional.
2. **Sitio Web (Astro)**: Se rediseñó el portal con un layout moderno de *glass cards* alternadas en zigzag, adaptativo para dispositivos móviles, integrando limpiamente la iconografía y las capturas de pantalla de la app.
3. **Lenguaje Inclusivo y Ajustes**: Se estandarizaron todos los textos (App, Web, README) para ser completamente neutrales en cuanto a género, y se ajustaron saltos de línea para evitar problemas de _word wrap_ en los cuadros de diálogo.
