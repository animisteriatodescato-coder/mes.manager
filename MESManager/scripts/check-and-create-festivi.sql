-- Script per verificare e creare la tabella Festivi se non esiste
-- Eseguire contro il database MESManager_Dev o MESManager_Prod

-- 1. Verifica se la tabella esiste
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Festivi')
BEGIN
    PRINT '❌ Tabella Festivi NON trovata. Creazione in corso...';
    
    CREATE TABLE [dbo].[Festivi](
        [Id] [uniqueidentifier] NOT NULL,
        [Data] [date] NOT NULL,
        [Descrizione] [nvarchar](200) NOT NULL,
        [Ricorrente] [bit] NOT NULL DEFAULT 0,
        [Anno] [int] NULL,
        [DataCreazione] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Festivi] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX IX_Festivi_Data ON [dbo].[Festivi]([Data]);
    CREATE INDEX IX_Festivi_Ricorrente ON [dbo].[Festivi]([Ricorrente]);
    
    PRINT '✅ Tabella Festivi creata con successo';
END
ELSE
BEGIN
    PRINT '✅ Tabella Festivi già presente';
END
GO

-- 2. Verifica struttura tabella
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE,
    c.CHARACTER_MAXIMUM_LENGTH
FROM 
    INFORMATION_SCHEMA.COLUMNS c
WHERE 
    c.TABLE_NAME = 'Festivi'
ORDER BY 
    c.ORDINAL_POSITION;
GO

-- 3. Verifica indici
SELECT 
    i.name AS IndexName,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
    i.type_desc AS IndexType
FROM 
    sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE 
    i.object_id = OBJECT_ID('Festivi')
ORDER BY 
    i.name, ic.key_ordinal;
GO

-- 4. Conta i record presenti
SELECT COUNT(*) AS NumeroFestivi FROM [dbo].[Festivi];
GO
