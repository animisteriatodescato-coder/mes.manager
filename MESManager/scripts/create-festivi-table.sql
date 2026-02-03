-- Script per creare tabella Festivi se non esiste
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Festivi')
BEGIN
    CREATE TABLE [dbo].[Festivi](
        [Id] [uniqueidentifier] NOT NULL PRIMARY KEY,
        [Data] [date] NOT NULL,
        [Descrizione] [nvarchar](200) NOT NULL,
        [Ricorrente] [bit] NOT NULL,
        [Anno] [int] NULL,
        [DataCreazione] [datetime2](7) NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT 'Tabella Festivi creata con successo';
END
ELSE
BEGIN
    PRINT 'Tabella Festivi già presente';
END
GO

-- Verifica creazione
SELECT 
    CASE 
        WHEN EXISTS(SELECT * FROM sys.tables WHERE name = 'Festivi') 
        THEN 'OK - Tabella Festivi presente' 
        ELSE 'ERRORE - Tabella Festivi mancante' 
    END AS Stato;
GO
