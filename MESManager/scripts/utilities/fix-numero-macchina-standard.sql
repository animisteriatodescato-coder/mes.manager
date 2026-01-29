-- Script per standardizzare TUTTO il sistema al formato M001, M002, etc.
-- Esegui questo script per risolvere definitivamente i problemi di formato

-- PARTE 1: Standardizza Anime.MacchineSuDisponibili da "1;2;3" a "M001;M002;M003"
UPDATE Anime
SET MacchineSuDisponibili = 
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(
    REPLACE(MacchineSuDisponibili, 
        ';11', ';M011'),
        ';10', ';M010'),
        ';9', ';M009'),
        ';8', ';M008'),
        ';7', ';M007'),
        ';6', ';M006'),
        ';5', ';M005'),
        ';4', ';M004'),
        ';3', ';M003'),
        ';2', ';M002'),
        ';1', ';M001')
WHERE MacchineSuDisponibili IS NOT NULL
  AND MacchineSuDisponibili NOT LIKE '%M0%';

-- Gestisci anche il primo numero (senza ; davanti)
UPDATE Anime
SET MacchineSuDisponibili = 
    CASE 
        WHEN MacchineSuDisponibili LIKE '11;%' THEN 'M011' + SUBSTRING(MacchineSuDisponibili, 3, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '10;%' THEN 'M010' + SUBSTRING(MacchineSuDisponibili, 3, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '9;%' THEN 'M009' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '8;%' THEN 'M008' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '7;%' THEN 'M007' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '6;%' THEN 'M006' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '5;%' THEN 'M005' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '4;%' THEN 'M004' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '3;%' THEN 'M003' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '2;%' THEN 'M002' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        WHEN MacchineSuDisponibili LIKE '1;%' THEN 'M001' + SUBSTRING(MacchineSuDisponibili, 2, LEN(MacchineSuDisponibili))
        ELSE MacchineSuDisponibili
    END
WHERE MacchineSuDisponibili IS NOT NULL
  AND MacchineSuDisponibili NOT LIKE 'M0%';

-- PARTE 2: Standardizza Commesse.NumeroMacchina
-- Converti formati:  "1" -> "M001", "01" -> "M001", "M1" -> "M001", etc.

-- Prima cancella i valori non validi
UPDATE Commesse
SET NumeroMacchina = NULL
WHERE NumeroMacchina IS NOT NULL
  AND (
    LEN(NumeroMacchina) = 0
    OR NumeroMacchina NOT LIKE '%[0-9]%'  -- Non contiene numeri
    OR TRY_CAST(REPLACE(REPLACE(NumeroMacchina, 'M', ''), '0', '') AS INT) > 11  -- Fuori range
    OR TRY_CAST(REPLACE(REPLACE(NumeroMacchina, 'M', ''), '0', '') AS INT) < 1
  );

-- Poi normalizza i valori validi
UPDATE Commesse
SET NumeroMacchina = 
    'M' + RIGHT('00' + CAST(
        TRY_CAST(
            REPLACE(
                REPLACE(NumeroMacchina, 'M', ''),
            '0', '')  -- Rimuove tutti gli zeri
        AS INT) AS NVARCHAR(10)
    ), 3)
WHERE NumeroMacchina IS NOT NULL
  AND NumeroMacchina NOT LIKE 'M[0-9][0-9][0-9]';  -- Già nel formato corretto

-- VERIFICA RISULTATI
SELECT 'Anime - MacchineSuDisponibili' as Tabella, 
       COUNT(*) as Totale,
       COUNT(CASE WHEN MacchineSuDisponibili LIKE 'M0%' THEN 1 END) as FormatoCorretto,
       COUNT(CASE WHEN MacchineSuDisponibili NOT LIKE 'M0%' AND MacchineSuDisponibili IS NOT NULL THEN 1 END) as FormatoVecchio
FROM Anime
UNION ALL
SELECT 'Commesse - NumeroMacchina',
       COUNT(*) as Totale,
       COUNT(CASE WHEN NumeroMacchina LIKE 'M[0-9][0-9][0-9]' THEN 1 END) as FormatoCorretto,
       COUNT(CASE WHEN NumeroMacchina NOT LIKE 'M[0-9][0-9][0-9]' AND NumeroMacchina IS NOT NULL THEN 1 END) as FormatoVecchio
FROM Commesse;

-- Mostra esempio risultati
SELECT TOP 5 
    'Anime' as Fonte,
    CodiceArticolo as Codice,
    MacchineSuDisponibili as Valore
FROM Anime
WHERE MacchineSuDisponibili IS NOT NULL
UNION ALL
SELECT TOP 5
    'Commesse' as Fonte,
    Codice,
    NumeroMacchina as Valore
FROM Commesse
WHERE NumeroMacchina IS NOT NULL
ORDER BY Fonte, Codice;
