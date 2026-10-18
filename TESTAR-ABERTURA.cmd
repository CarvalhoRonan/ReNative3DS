@echo off
setlocal
cd /d "%~dp0"
title ReNative3D36S - Testar abertura
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\testar-abertura.ps1"
set "RC=%ERRORLEVEL%"
echo.
if "%RC%"=="0" (echo Teste inicial concluido.) else (echo Verifique o relatorio em logs.)
pause
exit /b %RC%
