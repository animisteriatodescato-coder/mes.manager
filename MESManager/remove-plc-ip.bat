@echo off
echo Rimozione IP secondario per ripristinare connessione...
netsh interface ip delete address "Ethernet" 192.168.17.221
if %errorlevel% equ 0 (
    echo IP rimosso con successo
) else (
    echo IP non presente o errore rimozione
)
pause
