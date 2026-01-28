-- Test query per verificare i dati nel database Gantt
-- Esegui questa query direttamente sul database Gantt per vedere se ci sono dati

SELECT TOP 10
    [IdArticolo],
    [CodiceArticolo],
    [DescrizioneArticolo],
    [Cliente],
    [Colla],
    [Sabbia],
    [Vernice],
    [TogliereSparo],
    [QuantitaPiano],
    [NumeroPiani],
    [Figure],
    [Piastra],
    [Maschere],
    [Incollata],
    [Assemblata],
    [ArmataL]
FROM [Gantt].[dbo].[tbArticoli]
WHERE 
    [Cliente] IS NOT NULL 
    OR [Colla] IS NOT NULL 
    OR [Sabbia] IS NOT NULL 
    OR [Vernice] IS NOT NULL
    OR [TogliereSparo] IS NOT NULL
    OR [Figure] IS NOT NULL
ORDER BY [IdArticolo] DESC;

-- Query per contare quanti record hanno dati in queste colonne
SELECT 
    COUNT(*) as TotaleRecord,
    COUNT([Cliente]) as ConCliente,
    COUNT([Colla]) as ConColla,
    COUNT([Sabbia]) as ConSabbia,
    COUNT([Vernice]) as ConVernice,
    COUNT([TogliereSparo]) as ConTogliereSparo,
    COUNT([QuantitaPiano]) as ConQuantitaPiano,
    COUNT([NumeroPiani]) as ConNumeroPiani,
    COUNT([Figure]) as ConFigure,
    COUNT([Piastra]) as ConPiastra,
    COUNT([Maschere]) as ConMaschere,
    COUNT([Incollata]) as ConIncollata,
    COUNT([Assemblata]) as ConAssemblata,
    COUNT([ArmataL]) as ConArmataL
FROM [Gantt].[dbo].[tbArticoli];
