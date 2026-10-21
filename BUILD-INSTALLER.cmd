@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title Kashtrix Broadcast Suite - Build Installer Setup

echo ===================================================
echo   Kashtrix Broadcast Suite - Installer Builder
echo ===================================================
echo.

set "ISCC=C:\Program Files\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" (
    set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)
if not exist "%ISCC%" (
    where ISCC.exe >nul 2>&1
    if not errorlevel 1 (
        set "ISCC=ISCC.exe"
    ) else (
        echo [ERROR] Inno Setup compiler ^(ISCC.exe^) was not found!
        echo Please ensure Inno Setup is installed at "C:\Program Files\Inno Setup 7\ISCC.exe"
        pause
        exit /b 1
    )
)

echo [1/2] Verifying that active build artifacts exist...
if not exist "%LOCALAPPDATA%\KashtrixPlayout\BuildSessions\active\bin\BroadcastPlayout.App\x64\Release\net10.0-windows10.0.26100.0\win-x64\Kashtrix.Playout.exe" (
    echo [INFO] Release binaries not found in active cache. Running Release build first...
    dotnet build BroadcastPlayout.sln -c Release
    if errorlevel 1 (
        echo [ERROR] Release build failed. Cannot proceed with installer creation.
        pause
        exit /b 2
    )
) else (
    echo [OK] Active compiled Release binaries found.
)

echo.
echo [2/2] Compiling Inno Setup package...
if not exist "installer-output" mkdir "installer-output"
"%ISCC%" ".\tools\KashtrixSetup.iss"
if errorlevel 1 (
    echo.
    echo [ERROR] Inno Setup compilation failed!
    pause
    exit /b 3
)

echo.
echo ===================================================
echo   SUCCESS: Kashtrix Installer Package Created!
echo   Location: .\installer-output\KashtrixPlayoutSuite_Setup_x64.exe
echo ===================================================
echo.
pause
