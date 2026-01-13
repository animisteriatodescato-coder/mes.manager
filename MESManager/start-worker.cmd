@echo off
setlocal
set "ROOT=%~dp0publish\win-x64\Release\MESManager.Worker"

if not exist "%ROOT%\MESManager.Worker.exe" (
  echo [ERRORE] Eseguibile non trovato: %ROOT%\MESManager.Worker.exe
  echo Esegui prima: publish-win.ps1
  exit /b 1
)

echo Avvio Worker...
"%ROOT%\MESManager.Worker.exe"
