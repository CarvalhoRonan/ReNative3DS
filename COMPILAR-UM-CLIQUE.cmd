@echo off
setlocal
cd /d "%~dp0"
title ReNative3D36S - Compilacao automatica
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\publicar-windows.ps1"
set "RESULTADO=%ERRORLEVEL%"
echo.
if "%RESULTADO%"=="0" (
  echo Publicacao concluida. O executavel esta em dist\portable\ReNative3D36S.exe
) else (
  echo Falha na publicacao. Confira o arquivo de log na pasta logs.
)
echo.
pause
exit /b %RESULTADO%
