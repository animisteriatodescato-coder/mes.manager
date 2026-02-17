# IMPORTAZIONE RICETTE DA GANTT A MESMANAGER_PROD
# ====================================================
# Importa 132 ricette dal database Gantt.ArticoliRicetta
# nel database MESManager_Prod (Ricette + ParametriRicetta)

$serverGantt = "192.168.1.230\SQLEXPRESS"
$dbGantt = "Gantt"
$serverMES = "192.168.1.230\SQLEXPRESS01"
$dbMES = "MESManager_Prod"
$user = "FAB"
$pwd = "password.123"
$userSA = "sa"

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "   IMPORTAZIONE RICETTE DA GANTT" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# Connessione a Gantt (fonte dati)
Write-Host "📡 Connessione a Gantt ($serverGantt)..." -ForegroundColor Yellow
$connGantt = New-Object System.Data.SqlClient.SqlConnection("Server=$serverGantt;Database=$dbGantt;User Id=$userSA;Password=$pwd;TrustServerCertificate=True;")
try {
    $connGantt.Open()
    Write-Host "   ✅ Connesso a database Gantt" -ForegroundColor Green
} catch {
    Write-Host "   ❌ ERRORE connessione Gantt: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Connessione a MESManager_Prod (destinazione)
Write-Host "📡 Connessione a MESManager_Prod ($serverMES)..." -ForegroundColor Yellow
$connMES = New-Object System.Data.SqlClient.SqlConnection("Server=$serverMES;Database=$dbMES;User Id=$user;Password=$pwd;TrustServerCertificate=True;")
try {
    $connMES.Open()
    Write-Host "   ✅ Connesso a database MESManager_Prod" -ForegroundColor Green
} catch {
    Write-Host "   ❌ ERRORE connessione MESManager_Prod: $($_.Exception.Message)" -ForegroundColor Red
    $connGantt.Close()
    exit 1
}

$cmdGantt = $connGantt.CreateCommand()
$cmdMES = $connMES.CreateCommand()

# Statistiche iniziali
Write-Host "`n📊 STATISTICHE INIZIALI:" -ForegroundColor Cyan
$cmdMES.CommandText = "SELECT COUNT(*) FROM Ricette"
$ricetteEsistenti = $cmdMES.ExecuteScalar()
$cmdMES.CommandText = "SELECT COUNT(*) FROM ParametriRicetta"
$parametriEsistenti = $cmdMES.ExecuteScalar()
Write-Host "   Ricette esistenti: $ricetteEsistenti" -ForegroundColor Gray
Write-Host "   Parametri esistenti: $parametriEsistenti" -ForegroundColor Gray

$cmdGantt.CommandText = "SELECT COUNT(DISTINCT CodiceArticolo) FROM ArticoliRicetta WHERE CodiceArticolo IS NOT NULL AND CodiceArticolo != ''"
$ricetteDaImportare = $cmdGantt.ExecuteScalar()
Write-Host "   Ricette da importare: $ricetteDaImportare" -ForegroundColor Yellow
Write-Host "`n⚠️  ATTENZIONE: Verranno importate $ricetteDaImportare ricette" -ForegroundColor Yellow
Write-Host "   Procedimento automatico..." -ForegroundColor Green

# Contatori
$ricetteImportate = 0
$ricetteSaltate = 0
$parametriImportati = 0
$errori = 0

# Leggi tutti gli articoli con ricette da Gantt
Write-Host "`n🔄 INIZIO IMPORTAZIONE..." -ForegroundColor Cyan
$cmdGantt.CommandText = @"
SELECT DISTINCT CodiceArticolo
FROM ArticoliRicetta
WHERE CodiceArticolo IS NOT NULL AND CodiceArticolo != ''
ORDER BY CodiceArticolo
"@

$readerArticoli = $cmdGantt.ExecuteReader()
$articoliDaImportare = @()
while ($readerArticoli.Read()) {
    if ($readerArticoli['CodiceArticolo'] -ne [DBNull]::Value) {
        $articoliDaImportare += $readerArticoli['CodiceArticolo']
    }
}
$readerArticoli.Close()

Write-Host "   Trovati $($articoliDaImportare.Count) articoli con ricette`n" -ForegroundColor White

# Processa ogni articolo
foreach ($codiceArticolo in $articoliDaImportare) {
    Write-Host "  Importo: $codiceArticolo... " -NoNewline -ForegroundColor Gray
    
    try {
        # 1. Verifica se l'articolo esiste in MESManager_Prod
        $cmdMES.CommandText = "SELECT Id FROM Articoli WHERE Codice = @codice"
        $cmdMES.Parameters.Clear()
        $cmdMES.Parameters.AddWithValue("@codice", $codiceArticolo) | Out-Null
        $articoloId = $cmdMES.ExecuteScalar()
        
        if ($null -eq $articoloId) {
            Write-Host "❌ Articolo non trovato (saltato)" -ForegroundColor Yellow
            $ricetteSaltate++
            continue
        }
        
        # 2. Verifica se esiste già una ricetta per questo articolo
        $cmdMES.CommandText = "SELECT Id FROM Ricette WHERE ArticoloId = @articoloId"
        $cmdMES.Parameters.Clear()
        $cmdMES.Parameters.AddWithValue("@articoloId", $articoloId) | Out-Null
        $ricettaEsistente = $cmdMES.ExecuteScalar()
        
        if ($null -ne $ricettaEsistente) {
            Write-Host "⚠️  Ricetta già esistente (ID: $ricettaEsistente) - sovrascrivo parametri" -ForegroundColor Yellow
            
            # Elimina parametri esistenti
            $cmdMES.CommandText = "DELETE FROM ParametriRicetta WHERE RicettaId = @ricettaId"
            $cmdMES.Parameters.Clear()
            $cmdMES.Parameters.AddWithValue("@ricettaId", $ricettaEsistente) | Out-Null
            $cmdMES.ExecuteNonQuery() | Out-Null
            
            $ricettaId = $ricettaEsistente
        } else {
            # 3. Crea nuova ricetta (GUID come Id)
            $ricettaId = [Guid]::NewGuid()
            $cmdMES.CommandText = "INSERT INTO Ricette (Id, ArticoloId) VALUES (@id, @articoloId)"
            $cmdMES.Parameters.Clear()
            $cmdMES.Parameters.AddWithValue("@id", $ricettaId) | Out-Null
            $cmdMES.Parameters.AddWithValue("@articoloId", $articoloId) | Out-Null
            $risultato = $cmdMES.ExecuteNonQuery()
            
            if ($risultato -eq 0) {
                Write-Host "❌ Errore creazione ricetta" -ForegroundColor Red
                $errori++
                continue
            }
        }
        
        # 4. Leggi parametri da Gantt in memoria
        $cmdGantt.CommandText = @"
SELECT 
    CodiceParametro,
    DescrizioneParametro,
    Indirizzo,
    Area,
    Tipo,
    UM,
    Valore
FROM ArticoliRicetta
WHERE CodiceArticolo = @codiceArticolo
ORDER BY CodiceParametro, Indirizzo
"@
        $cmdGantt.Parameters.Clear()
        $cmdGantt.Parameters.AddWithValue("@codiceArticolo", $codiceArticolo) | Out-Null
        
        $readerParametri = $cmdGantt.ExecuteReader()
        $parametriDaInserire = @()
        
        # Leggi tutti i parametri in memoria
        while ($readerParametri.Read()) {
            $parametriDaInserire += [PSCustomObject]@{
                CodiceParametro = if ($readerParametri['CodiceParametro'] -ne [DBNull]::Value) { $readerParametri['CodiceParametro'] } else { $null }
                DescrizioneParametro = if ($readerParametri['DescrizioneParametro'] -ne [DBNull]::Value -and $readerParametri['DescrizioneParametro'] -ne '') { $readerParametri['DescrizioneParametro'] } else { "-" }
                Valore = if ($readerParametri['Valore'] -ne [DBNull]::Value -and $readerParametri['Valore'] -ne '') { $readerParametri['Valore'] } else { "0" }
                UM = if ($readerParametri['UM'] -ne [DBNull]::Value -and $readerParametri['UM'].ToString().Trim() -ne '') { $readerParametri['UM'].ToString().Trim() } else { "" }
                Indirizzo = if ($readerParametri['Indirizzo'] -ne [DBNull]::Value) { $readerParametri['Indirizzo'] } else { $null }
                Area = if ($readerParametri['Area'] -ne [DBNull]::Value) { $readerParametri['Area'].ToString() } else { $null }
                Tipo = if ($readerParametri['Tipo'] -ne [DBNull]::Value) { $readerParametri['Tipo'].ToString() } else { $null }
            }
        }
        
        $readerParametri.Close()
        
        # 5. Inserisci parametri in MESManager_Prod
        $numParam = 0
        foreach ($param in $parametriDaInserire) {
            $parametroId = [Guid]::NewGuid()
            $cmdMESInsert = $connMES.CreateCommand()
            $cmdMESInsert.CommandText = @"
INSERT INTO ParametriRicetta 
    (Id, RicettaId, NomeParametro, Valore, UnitaMisura, CodiceParametro, Indirizzo, Area, Tipo)
VALUES 
    (@id, @ricettaId, @nome, @valore, @um, @codParam, @indirizzo, @area, @tipo)
"@
            
            $cmdMESInsert.Parameters.AddWithValue("@id", $parametroId) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@ricettaId", $ricettaId) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@nome", $param.DescrizioneParametro) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@valore", $param.Valore) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@um", $param.UM) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@codParam", $(if ($param.CodiceParametro) { $param.CodiceParametro } else { [DBNull]::Value })) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@indirizzo", $(if ($param.Indirizzo) { $param.Indirizzo } else { [DBNull]::Value })) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@area", $(if ($param.Area) { $param.Area } else { [DBNull]::Value })) | Out-Null
            $cmdMESInsert.Parameters.AddWithValue("@tipo", $(if ($param.Tipo) { $param.Tipo } else { [DBNull]::Value })) | Out-Null
            
            $cmdMESInsert.ExecuteNonQuery() | Out-Null
            $numParam++
            $parametriImportati++
        }
        
        Write-Host "✅ Importata ($numParam parametri)" -ForegroundColor Green
        $ricetteImportate++
        
    } catch {
        Write-Host "❌ ERRORE: $($_.Exception.Message)" -ForegroundColor Red
        $errori++
    }
    
    # Progress ogni 10 ricette
    if (($ricetteImportate + $ricetteSaltate) % 10 -eq 0) {
        $progresso = [math]::Round((($ricetteImportate + $ricetteSaltate) / $articoliDaImportare.Count) * 100, 1)
        Write-Host "`n  📊 Progresso: $progresso% ($ricetteImportate importate, $ricetteSaltate saltate)`n" -ForegroundColor Cyan
    }
}

# Statistiche finali
Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "   IMPORTAZIONE COMPLETATA" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

Write-Host "📈 RIEPILOGO:" -ForegroundColor Yellow
Write-Host "   ✅ Ricette importate: $ricetteImportate" -ForegroundColor Green
Write-Host "   ⚠️  Ricette saltate (articolo non trovato): $ricetteSaltate" -ForegroundColor Yellow
Write-Host "   📦 Parametri importati: $parametriImportati" -ForegroundColor Green
Write-Host "   ❌ Errori: $errori" -ForegroundColor $(if ($errori -gt 0) { 'Red' } else { 'Green' })

# Verifica finale
$cmdMES.CommandText = "SELECT COUNT(*) FROM Ricette"
$totaleRicette = $cmdMES.ExecuteScalar()
$cmdMES.CommandText = "SELECT COUNT(*) FROM ParametriRicetta"
$totaleParametri = $cmdMES.ExecuteScalar()

Write-Host "`n📊 STATO FINALE DATABASE:" -ForegroundColor Cyan
Write-Host "   Ricette totali: $totaleRicette (prima: $ricetteEsistenti, +$($totaleRicette - $ricetteEsistenti))" -ForegroundColor White
Write-Host "   Parametri totali: $totaleParametri (prima: $parametriEsistenti, +$($totaleParametri - $parametriEsistenti))" -ForegroundColor White

# Chiudi connessioni
$connGantt.Close()
$connMES.Close()

Write-Host "`n✅ IMPORTAZIONE COMPLETATA CON SUCCESSO!`n" -ForegroundColor Green
