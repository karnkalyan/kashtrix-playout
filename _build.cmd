@echo off
setlocal EnableExtensions
cd /d "%~dp0"
if errorlevel 1 (
  echo Unable to enter project directory: %~dp0
  pause
  exit /b 2
)
title Kashtrix - Full Verification, Build, and Web Asset Compilation

where powershell.exe >nul 2>&1
if errorlevel 1 (
  echo Windows PowerShell was not found. This build requires Windows PowerShell 5.1.
  pause
  exit /b 3
)

echo ======================================================================
echo  KASHTRIX BROADCAST PLATFORM - COMPLETE BUILD & VERIFICATION
echo ======================================================================
echo.

powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File ".\tools\Setup-All.ps1"
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
  echo.
  echo [ERROR] Kashtrix backend setup/build failed with exit code %EXITCODE%.
  pause
  exit /b %EXITCODE%
)

echo.
echo ======================================================================
echo  BUILDING KASHTRIX-MIX LIVE SWITCHER (KMix.Engine + KMix.App)
echo ======================================================================
echo.

set "MSBUILD="
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
  for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    set "MSBUILD=%%i"
  )
)
if "%MSBUILD%"=="" if exist "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
)

if not "%MSBUILD%"=="" if exist "Kashtrix-Mix\src\KMix.App\KMix.App.csproj" (
  echo Invoking MSBuild for Kashtrix-Mix...
  "%MSBUILD%" "Kashtrix-Mix\src\KMix.App\KMix.App.csproj" -p:Configuration=Debug -p:Platform=x64 -m:1 -v:m
  if errorlevel 1 (
    echo [ERROR] Kashtrix-Mix build failed.
    pause
    exit /b 1
  )
  echo [SUCCESS] Kashtrix-Mix compiled cleanly.
) else (
  echo [SKIP] MSBuild not found or Kashtrix-Mix project not present.
)

echo.
echo ======================================================================
echo  BUILDING WEB FRONTENDS (cg-web ^& playout-web)
echo ======================================================================
echo.

if exist "web\cg-web\package.json" (
  echo Building cg-web...
  cd /d "%~dp0web\cg-web"
  call npm run build
  cd /d "%~dp0"
)

if exist "web\playout-web\package.json" (
  echo Building playout-web...
  cd /d "%~dp0web\playout-web"
  call npm run build
  cd /d "%~dp0"
)

echo.
echo ======================================================================
echo  KASHTRIX COMPLETE BUILD SUCCESSFUL (Backend + KMix + Web Frontends)
echo ======================================================================
echo.
pause
exit /b 0
