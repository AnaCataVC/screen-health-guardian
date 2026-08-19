@echo off
REM Screen Health Guardian — Build script
REM 1. Compiles Python to a directory (better performance)
REM 2. Compiles Inno Setup script to a final Setup.exe (if installed)

echo ===========================================
echo 1. Building Python Executable (Directory Mode)
echo ===========================================

python -m PyInstaller ^
    --onedir ^
    --noconfirm ^
    --windowed ^
    --name "ScreenHealthGuardian" ^
    --icon "icon.ico" ^
    --add-data "icon.ico;." ^
    --paths src ^
    --hidden-import pystray._win32 ^
    src\main.py

if %ERRORLEVEL% NEQ 0 (
    echo ❌ PyInstaller build failed. Check the output above.
    pause
    exit /b %ERRORLEVEL%
)

echo ✅ PyInstaller build successful!
echo.

echo ===========================================
echo 2. Building Setup Installer (Inno Setup)
echo ===========================================

set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if exist "%ISCC%" (
    "%ISCC%" "installer.iss"
    if errorlevel 1 (
        echo ❌ Installer build failed.
    ) else (
        echo ✅ Installer built successfully!
        echo    Location: dist\ScreenHealthGuardian-Setup.exe
        echo 🧹 Cleaning up portable directory...
        rmdir /s /q "dist\ScreenHealthGuardian"
    )
) else (
    echo ⚠️ Inno Setup Compiler ISCC.exe not found.
    echo Please install Inno Setup 6 from https://jrsoftware.org/isdl.php
    echo to generate the professional installer.
    echo.
    echo The portable version is still available at dist\ScreenHealthGuardian\ScreenHealthGuardian.exe
)

echo.
echo Build process complete.
