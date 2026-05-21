# ==============================================================================
# backup-auto.ps1 - Backup automatico database MESManager
# ==============================================================================
param(
    [switch]$Force
)

$ErrorActionPreference = 'Continue'
$BackupRoot = "C:\Dev\MESManager\backups\auto"
$LogFile    = "$BackupRoot\backup.log"
$Retention  = 30
$Timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $line = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $line -ForegroundColor $Color
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Backup-SqlDatabase {
    param([string]$Server,[string]$Database,[string]$OutputFile,[string]$Username=$null,[string]$Password=$null)
    if ($Username) { $connStr = "Server=$Server;Database=master;User Id=$Username;Password=$Password;TrustServerCertificate=True;" }
    else { $connStr = "Server=$Server;Database=master;Integrated Security=True;TrustServerCertificate=True;" }
    $sql = "BACKUP DATABASE [$Database] TO DISK = N'$OutputFile' WITH NOFORMAT,INIT,NAME=N'$Database-AutoBackup-$Timestamp',SKIP,NOREWIND,NOUNLOAD,STATS=10"
    try {
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.CommandTimeout = 600
        $cmd.ExecuteNonQuery() | Out-Null
        $conn.Close()
        return $true
    } catch {
        Write-Log "ERRORE backup $Database su ${Server}: $_" "Red"
        if ($conn -and $conn.State -eq 'Open') { $conn.Close() }
        return $false
    }
}

function Remove-OldBackups {
    param([string]$Database,[int]$Keep=30)
    $files = Get-ChildItem -Path $BackupRoot -Filter "${Database}_*.bak" | Sort-Object LastWriteTime -Descending
    if ($files.Count -gt $Keep) { $files | Select-Object -Skip $Keep | ForEach-Object { Remove-Item $_.FullName -Force } }
}

function Test-SqlReachable {
    param([string]$Ip,[int]$Port=1433)
    try { $t=New-Object Net.Sockets.TcpClient; $a=$t.BeginConnect($Ip,$Port,$null,$null); $ok=$a.AsyncWaitHandle.WaitOne(3000) -and $t.Connected; $t.Close(); return $ok } catch { return $false }
}

if (-not (Test-Path $BackupRoot)) { New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null }

Write-Log "====== BACKUP AUTOMATICO MESManager ======" "Cyan"

foreach ($db in @("MESManager","MESManager_Dev")) {
    $outFile = "$BackupRoot\${db}_$Timestamp.bak"
    $today = Get-Date -Format "yyyyMMdd"
    if ((Get-ChildItem -Path $BackupRoot -Filter "${db}_${today}*.bak" -ErrorAction SilentlyContinue) -and -not $Force) {
        Write-Log "$db - backup odierno gia' presente, skip" "Yellow"; continue
    }
    Write-Log "$db - avvio backup..." "White"
    $ok = Backup-SqlDatabase -Server "localhost\SQLEXPRESS01" -Database $db -OutputFile $outFile
    if ($ok -and (Test-Path $outFile)) {
        $sizeMB = [math]::Round((Get-Item $outFile).Length/1MB,2)
        Write-Log "$db - OK ($sizeMB MB)" "Green"
        Remove-OldBackups -Database $db -Keep $Retention
    } else { Write-Log "$db - FALLITO" "Red" }
}

$prodHost="192.168.1.230"; $prodPort=1433; $prodSqlUsr="FAB"; $prodSqlPwd="password.123"
$uncProdBackups="\\$prodHost\Dati\DBBackups"
$prodBackupUnc="$uncProdBackups\MESManager_Prod_$Timestamp.bak"
$prodBackupLocal="$BackupRoot\MESManager_Prod_$Timestamp.bak"

Write-Log "Verifica raggiungibilita' $prodHost..." "White"
if (Test-SqlReachable -Ip $prodHost -Port $prodPort) {
    $today = Get-Date -Format "yyyyMMdd"
    if ((Get-ChildItem -Path $BackupRoot -Filter "MESManager_Prod_${today}*.bak" -ErrorAction SilentlyContinue) -and -not $Force) {
        Write-Log "MESManager_Prod - backup odierno gia' presente, skip" "Yellow"
    } else {
        if (-not (Test-Path $uncProdBackups -ErrorAction SilentlyContinue)) {
            New-Item -ItemType Directory -Path $uncProdBackups -Force -ErrorAction SilentlyContinue | Out-Null
        }
        Write-Log "MESManager_Prod - avvio backup -> $prodBackupUnc" "White"
        $ok = Backup-SqlDatabase -Server "$prodHost\SQLEXPRESS01" -Database "MESManager_Prod" -OutputFile $prodBackupUnc -Username $prodSqlUsr -Password $prodSqlPwd
        if ($ok -and (Test-Path $prodBackupUnc -ErrorAction SilentlyContinue)) {
            $sizeMB=[math]::Round((Get-Item $prodBackupUnc).Length/1MB,2)
            Write-Log "MESManager_Prod - OK su Dati\DBBackups ($sizeMB MB)" "Green"
            Copy-Item -Path $prodBackupUnc -Destination $prodBackupLocal -Force
            $sizeLocal=[math]::Round((Get-Item $prodBackupLocal).Length/1MB,2)
            Write-Log "MESManager_Prod - copia locale OK ($sizeLocal MB)" "Green"
            Remove-OldBackups -Database "MESManager_Prod" -Keep $Retention
            $remoteOld = Get-ChildItem -Path $uncProdBackups -Filter "MESManager_Prod_*.bak" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
            if ($remoteOld.Count -gt 7) { $remoteOld | Select-Object -Skip 7 | ForEach-Object { Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue } }
        } else { Write-Log "MESManager_Prod - FALLITO" "Red" }
    }
} else { Write-Log "MESManager_Prod - server $prodHost non raggiungibile, skip" "Yellow" }

Write-Log "====== FINE BACKUP ======" "Cyan"
