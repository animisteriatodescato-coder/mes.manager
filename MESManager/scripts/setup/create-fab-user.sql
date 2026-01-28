-- Script per creare utente FAB con accesso solo a MESManager_Dev
-- Eseguire su SQL Server 192.168.1.230\SQLEXPRESS01 come sa

USE [master]
GO

-- Crea il login FAB se non esiste
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'FAB')
BEGIN
    CREATE LOGIN [FAB] WITH PASSWORD = 'password.123', CHECK_POLICY = OFF
    PRINT 'Login FAB creato con successo'
END
ELSE
BEGIN
    PRINT 'Login FAB già esistente'
END
GO

-- Passa al database MESManager_Dev
USE [MESManager_Dev]
GO

-- Crea l'utente nel database se non esiste
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'FAB')
BEGIN
    CREATE USER [FAB] FOR LOGIN [FAB]
    PRINT 'Utente FAB creato nel database MESManager_Dev'
END
ELSE
BEGIN
    PRINT 'Utente FAB già esistente nel database'
END
GO

-- Assegna il ruolo db_owner al database MESManager_Dev
ALTER ROLE [db_owner] ADD MEMBER [FAB]
PRINT 'Utente FAB aggiunto al ruolo db_owner per MESManager_Dev'
GO

-- Verifica permessi
SELECT 
    dp.name AS UserName,
    dp.type_desc AS UserType,
    r.name AS RoleName
FROM sys.database_principals dp
LEFT JOIN sys.database_role_members drm ON dp.principal_id = drm.member_principal_id
LEFT JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
WHERE dp.name = 'FAB'
GO

PRINT 'Utente FAB configurato correttamente!'
PRINT 'Credenziali: User Id=FAB; Password=password.123'
PRINT 'Database: MESManager_Dev (solo questo database è accessibile)'
GO
