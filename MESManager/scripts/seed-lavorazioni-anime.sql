-- ============================================================================
-- SEED DATA: Modulo Lavorazioni Anime - Configurazione Iniziale
-- ============================================================================
-- Data: 20 Febbraio 2026
-- Descrizione: Popolamento tipi lavorazione e parametri economici standard
-- ============================================================================

-- Pulizia dati esistenti (se necessario - ATTENZIONE in produzione!)
-- DELETE FROM WorkProcessingParameters;
-- DELETE FROM WorkProcessingTypes;

-- ============================================================================
-- STEP 1: Creazione Tipi Lavorazione Master
-- ============================================================================

DECLARE @Now DATETIME2 = GETUTCDATE();

-- 1. DISTRIBUTORI (33BD600X)
DECLARE @IdDistributori UNIQU EIDENTIFIER = NEWID();
INSERT INTO WorkProcessingTypes (Id, Nome, Codice, Descrizione, Categoria, Ordinamento, Attivo, Archiviato, CreatedAt)
VALUES (
    @IdDistributori,
    'Distributori (33BD600X)',
    'DISTRIBUT',
    'Lavorazione anime per distributori idraulici modello 33BD600X',
    'Sabbiatura',
    1,
    1, -- Attivo
    0, -- Non archiviato
    @Now
);

-- 2. CERABEADS
DECLARE @IdCerabeads UNIQUEIDENTIFIER = NEWID();
INSERT INTO WorkProcessingTypes (Id, Nome, Codice, Descrizione, Categoria, Ordinamento, Attivo, Archiviato, CreatedAt)
VALUES (
    @IdCerabeads,
    'Cerabeads',
    'CERABEADS',
    'Lavorazione anime con sabbia Cerabeads (alta precisione)',
    'Sabbiatura',
    2,
    1,
    0,
    @Now
);

-- 3. SABBIA GHISA NORMALE
DECLARE @IdSabbiaNorm UNIQUEIDENTIFIER = NEWID();
INSERT INTO WorkProcessingTypes (Id, Nome, Codice, Descrizione, Categoria, Ordinamento, Attivo, Archiviato, CreatedAt)
VALUES (
    @IdSabbiaNorm,
    'Sabbia Ghisa Normale',
    'SABBIA_NORM',
    'Lavorazione anime con sabbia ghisa standard',
    'Sabbiatura',
    3,
    1,
    0,
    @Now
);

-- ============================================================================
-- STEP 2: Parametri Economici per DISTRIBUTORI
-- ============================================================================

INSERT INTO WorkProcessingParameters (
    Id,
    WorkProcessingTypeId,
    EuroOra,
    SabbiaCostoKg,
    CostoAttrezzatura,
    VerniceCostoPezzo,
    VerniciaturaCostoOra,
    IncollaggioCostoOra,
    ImballaggioOra,
    MargineDefaultPercent,
    ValidFrom,
    ValidTo,
    IsCurrent,
    VersionNotes,
    CreatedAt
)
VALUES (
    NEWID(),
    @IdDistributori,
    70.00,      -- €/Ora lavorazione
    1.30,       -- Costo sabbia €/kg
    100.00,     -- Costo attrezzatura €
    3.00,       -- Vernice €/pezzo
    40.00,      -- Verniciatura €/ora
    40.00,      -- Incollaggio €/ora
    40.00,      -- Imballo €/ora
    30.00,      -- Margine default 30%
    @Now,       -- Valid From
    NULL,       -- Valid To (NULL = attivo)
    1,          -- IsCurrent = true
    'Parametri iniziali Distributori 33BD600X - Febbraio 2026',
    @Now
);

-- ============================================================================
-- STEP 3: Parametri Economici per CERABEADS
-- ============================================================================

INSERT INTO WorkProcessingParameters (
    Id,
    WorkProcessingTypeId,
    EuroOra,
    SabbiaCostoKg,
    CostoAttrezzatura,
    VerniceCostoPezzo,
    VerniciaturaCostoOra,
    IncollaggioCostoOra,
    ImballaggioOra,
    MargineDefaultPercent,
    ValidFrom,
    ValidTo,
    IsCurrent,
    VersionNotes,
    CreatedAt
)
VALUES (
    NEWID(),
    @IdCerabeads,
    75.00,      -- €/Ora lavorazione (più costosa)
    1.80,       -- Costo sabbia €/kg (Cerabeads più cara)
    100.00,     -- Costo attrezzatura €
    3.00,       -- Vernice €/pezzo
    40.00,      -- Verniciatura €/ora
    40.00,      -- Incollaggio €/ora
    40.00,      -- Imballo €/ora
    30.00,      -- Margine default 30%
    @Now,
    NULL,
    1,
    'Parametri iniziali Cerabeads - Febbraio 2026',
    @Now
);

-- ============================================================================
-- STEP 4: Parametri Economici per SABBIA GHISA NORMALE
-- ============================================================================

INSERT INTO WorkProcessingParameters (
    Id,
    WorkProcessingTypeId,
    EuroOra,
    SabbiaCostoKg,
    CostoAttrezzatura,
    VerniceCostoPezzo,
    VerniciaturaCostoOra,
    IncollaggioCostoOra,
    ImballaggioOra,
    MargineDefaultPercent,
    ValidFrom,
    ValidTo,
    IsCurrent,
    VersionNotes,
    CreatedAt
)
VALUES (
    NEWID(),
    @IdSabbiaNorm,
    60.00,      -- €/Ora lavorazione (meno costosa)
    0.28,       -- Costo sabbia €/kg (normale economica)
    100.00,     -- Costo attrezzatura €
    3.00,       -- Vernice €/pezzo
    40.00,      -- Verniciatura €/ora
    40.00,      -- Incollaggio €/ora
    40.00,      -- Imballo €/ora
    30.00,      -- Margine default 30%
    @Now,
    NULL,
    1,
    'Parametri iniziali Sabbia Ghisa Normale - Febbraio 2026',
    @Now
);

-- ============================================================================
-- VERIFICA INSERIMENTI
-- ============================================================================

PRINT '✅ Seed completato con successo!';
PRINT '';
PRINT 'Tipi Lavorazione inseriti:';
SELECT Codice, Nome, Attivo, Ordinamento FROM WorkProcessingTypes ORDER BY Ordinamento;

PRINT '';
PRINT 'Parametri Economici inseriti:';
SELECT 
    wpt.Codice AS TipoLavorazione,
    wpp.EuroOra AS [€/Ora],
    wpp.SabbiaCostoKg AS [Sabbia €/kg],
    wpp.MargineDefaultPercent AS [Margine %],
    wpp.IsCurrent AS Attivo
FROM WorkProcessingParameters wpp
INNER JOIN WorkProcessingTypes wpt ON wpp.WorkProcessingTypeId = wpt.Id
WHERE wpp.IsCurrent = 1
ORDER BY wpt.Ordinamento;

PRINT '';
PRINT 'Database pronto per modulo Preventivi Lavorazioni Anime.';
