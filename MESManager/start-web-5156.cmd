@echo off
setlocal
set "ROOT=%~dp0publish\win-x64\Release\MESManager.Web"

if not exist "%ROOT%\MESManager.Web.exe" (
  echo [ERRORE] Eseguibile non trovato: %ROOT%\MESManager.Web.exe
  echo Esegui prima: publish-win.ps1
  exit /b 1
)

rem Trova una porta libera partendo da 5156 fino a 5200
set "PORT="
for /f %%P in ('powershell -NoProfile -Command "$start=5156; $end=5200; for($p=$start; $p -le $end; $p++){ if(-not (Test-NetConnection -ComputerName 127.0.0.1 -Port $p -WarningAction SilentlyContinue).TcpTestSucceeded){ $p; break } }"') do set PORT=%%P
if "%PORT%"=="" set PORT=5156

set "ASPNETCORE_CONTENTROOT=%ROOT%"
set "ASPNETCORE_URLS=http://localhost:%PORT%"
echo Avvio Web su %ASPNETCORE_URLS% con content root %ASPNETCORE_CONTENTROOT%
"%ROOT%\MESManager.Web.exe"
