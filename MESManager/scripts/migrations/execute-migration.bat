@echo off
cd /d C:\Dev\MESManager\scripts\migrations

echo ============================================
echo Migrazione Gantt -^> MESManager_Prod
echo ============================================
echo.

echo Esecuzione script SQL...
sqlcmd -S "192.168.1.230\SQLEXPRESS01" -U FAB -P "password.123" -d MESManager_Prod -i "migrate-gantt-to-mesmanager.sql" -C

echo.
echo ============================================
echo Migrazione completata!
echo ============================================

pause
