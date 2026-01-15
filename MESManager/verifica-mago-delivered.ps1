# Verifica valori Delivered in Mago

Write-Host "=== VERIFICA CAMPO DELIVERED IN MAGO ===" -ForegroundColor Cyan

# Conta per valore Delivered
sqlcmd -S "SRV-MAGO\SQLMAGO" -d "DBMAGO" -E -C -Q "SELECT Delivered, COUNT(*) AS Numero FROM MA_ProductionJobs WHERE InternalOrdNo IS NOT NULL AND InternalOrdNo != '' GROUP BY Delivered ORDER BY Delivered" -W -s "|"

Write-Host "`n=== CAMPIONE COMMESSE CON DELIVERED = 0 ===" -ForegroundColor Yellow
sqlcmd -S "SRV-MAGO\SQLMAGO" -d "DBMAGO" -E -C -Q "SELECT TOP 5 InternalOrdNo, Delivered, Invoiced FROM MA_ProductionJobs WHERE InternalOrdNo IS NOT NULL AND InternalOrdNo != '' AND Delivered = 0" -W -s "|"

Write-Host "`n=== CAMPIONE COMMESSE CON DELIVERED = 1 ===" -ForegroundColor Yellow
sqlcmd -S "SRV-MAGO\SQLMAGO" -d "DBMAGO" -E -C -Q "SELECT TOP 5 InternalOrdNo, Delivered, Invoiced FROM MA_ProductionJobs WHERE InternalOrdNo IS NOT NULL AND InternalOrdNo != '' AND Delivered = 1" -W -s "|"
