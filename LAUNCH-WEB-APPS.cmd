@echo off
title Launching Kashtrix Web Playout & CG Controller
echo ==============================================================
echo        KASHTRIX BROADCAST SUITE - WEB APPLICATIONS & API
echo ==============================================================
echo.
echo Checking Kashtrix API Gateway (Port 9080)...
netstat -ano | findstr :9080 >nul
if %errorlevel% neq 0 (
    echo Starting Kashtrix API Gateway Backend on http://127.0.0.1:9080 ...
    if exist "%LOCALAPPDATA%\KashtrixPlayout\BuildSessions\active\bin\Kashtrix.ApiGateway\Debug\net10.0-windows10.0.26100.0\win-x64\Kashtrix.ApiGateway.exe" (
        start "" "%LOCALAPPDATA%\KashtrixPlayout\BuildSessions\active\bin\Kashtrix.ApiGateway\Debug\net10.0-windows10.0.26100.0\win-x64\Kashtrix.ApiGateway.exe"
    ) else (
        start cmd /k "title Kashtrix API Gateway && dotnet run --project src\Kashtrix.ApiGateway\Kashtrix.ApiGateway.csproj"
    )
    timeout /t 2 >nul
) else (
    echo Kashtrix API Gateway is already running on port 9080.
)

echo.
echo [1] Launch Kashtrix Playout Web (Port 5173)
echo [2] Launch Kashtrix CG Controller Web (Port 5174)
echo [3] Launch Both Web Applications (Default)
echo.
set /p choice="Select an option [1-3] (Default: 3): "
if "%choice%"=="" set choice=3

if "%choice%"=="1" (
    echo Starting Playout Web on http://localhost:5173 ...
    cd web\playout-web
    start http://localhost:5173
    npm run dev -- --port 5173
    exit /b
)

if "%choice%"=="2" (
    echo Starting CG Controller Web on http://localhost:5174 ...
    cd web\cg-web
    start http://localhost:5174
    npm run dev -- --port 5174
    exit /b
)

if "%choice%"=="3" (
    echo Starting Both Web Applications...
    start cmd /k "title Kashtrix Playout Web && cd web\playout-web && npm run dev -- --port 5173"
    start cmd /k "title Kashtrix CG Controller Web && cd web\cg-web && npm run dev -- --port 5174"
    timeout /t 2 >nul
    start http://localhost:5173
    start http://localhost:5174
    exit /b
)
