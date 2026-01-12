@echo off
echo Aggiunta IP secondario 192.168.17.221 per comunicazione PLC
echo Richiede privilegi amministratore...
netsh interface ip add address "Ethernet" 192.168.17.221 255.255.255.0
if %errorlevel% equ 0 (
    echo.
    echo IP aggiunto con successo!
    echo Verifico configurazione...
    ipconfig | findstr "192.168"
) else (
    echo.
    echo ERRORE: impossibile aggiungere IP
    echo Verifica di aver eseguito come Amministratore
)
pause
