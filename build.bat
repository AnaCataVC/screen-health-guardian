@echo off
setlocal enabledelayedexpansion

echo ===================================================
echo   Screen Health Guardian - Build Script (.NET 9)
echo ===================================================

if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=%LOCALAPPDATA%\Microsoft\dotnet"
    set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
) else (
    where dotnet >nul 2>&1
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] .NET SDK is not found.
        echo Install .NET SDK with: winget install Microsoft.DotNet.SDK.9
        exit /b 1
    )
)

echo [1/3] Restoring NuGet dependencies...
dotnet restore src\ScreenHealthGuardian.csproj
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to restore packages.
    exit /b 1
)

echo [2/3] Publishing standalone executable (Release win-x64)...
dotnet publish src\ScreenHealthGuardian.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o releases\win-x64

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Dotnet publish failed.
    exit /b 1
)

echo [3/3] Compiling Inno Setup installer...
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%ISCC%" (
    "%ISCC%" installer.iss
    echo [SUCCESS] Installer generated in releases\
) else (
    echo [INFO] Inno Setup compiler not found at default path.
    echo Standalone binary is ready at: releases\win-x64\ScreenHealthGuardian.exe
)

echo ===================================================
