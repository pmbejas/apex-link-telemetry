@echo off
setlocal
title Backend Telemetria SimRacing - Kestrel Host

echo ==========================================================
echo    Telemetria SimRacing: Servidor Kestrel (Puerto 8080)   
echo ==========================================================

cd /d "%~dp0\.."

echo Iniciando backend .NET...
dotnet run --project src\Telemetria.Api\Telemetria.Api.csproj %*

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] El servidor finalizo con codigo de error %ERRORLEVEL%.
    pause
)
