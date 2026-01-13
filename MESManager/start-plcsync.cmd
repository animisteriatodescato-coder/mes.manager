@echo off
cd /d "%~dp0MESManager.PlcSync"
echo Avvio PlcSync Worker...
dotnet run --project MESManager.PlcSync.csproj
