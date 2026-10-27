@echo off
setlocal
title Compilacion Release - Telemetria SimRacing

echo ==========================================================
echo    Telemetria SimRacing: Compilador de Produccion Release  
echo ==========================================================

cd /d "%~dp0\.."

echo [1/2] Limpiando compilaciones anteriores...
dotnet clean Telemetria.sln -c Release

echo.
echo [2/2] Compilando solucion completa en modo Release...
dotnet build Telemetria.sln -c Release --no-incremental

if %ERRORLEVEL% equ 0 (
    echo.
    echo [EXITO] Compilacion Release completada sin errores ni advertencias.
) else (
    echo.
    echo [ERROR] Fallo la compilacion Release con codigo %ERRORLEVEL%.
)

pause
