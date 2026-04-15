# ⭐ 05 — Modulo Core: [NOME_MODULO_PRINCIPALE]

> Documentazione del modulo principale di [NOME_PROGETTO].
> **Leggere PRIMA di modificare qualsiasi logica core.**

---

## 🎯 Scopo del Modulo

[Descrizione del modulo principale del sistema — es. "Gestione Ordini di Produzione", "Pianificazione Turni", "Gestione Magazzino", ecc.]

---

## 📐 Entità Principali

```
[NomeEntità1]           ← [descrizione breve]
[NomeEntità2]           ← [descrizione breve]
[NomeEntità3]           ← [descrizione breve]

Relazioni:
  [NomeEntità1] 1---N [NomeEntità2]
  [NomeEntità2] N---1 [NomeEntità3]
```

---

## 🔄 Flusso Business Principale

```
[Stato1] → [Stato2] → [Stato3] → [Stato4]
   ↑                               ↓
[AzioneAnnulla] ←─────────────────
```

### Transizioni di Stato

| Da | A | Condizione | Azione |
|----|---|------------|--------|
| [Stato1] | [Stato2] | [condizione] | [azione eseguita] |
| [Stato2] | [Stato3] | [condizione] | [azione eseguita] |

---

## 📊 Struttura Database

```sql
-- Tabella principale
CREATE TABLE [NomeTabella] (
    Id          INT PRIMARY KEY IDENTITY,
    [Campo1]    [TIPO] NOT NULL,
    [Campo2]    [TIPO] NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted   BIT NOT NULL DEFAULT 0   -- soft delete
);

-- Indici critici (performance)
CREATE INDEX IX_[NomeTabella]_[Campo] ON [NomeTabella]([Campo]);
```

---

## ⚙️ Algoritmi/Logiche Critiche

> Documentare qui qualsiasi algoritmo non ovvio.

### [Nome Algoritmo 1]

**Scopo**: [perché esiste]
**Input**: [cosa riceve]
**Output**: [cosa produce]
**Regole business**:
- [regola 1]
- [regola 2]

**⚠️ ATTENZIONE**: [warning se ci sono casi limite importanti]

---

## 🚫 Vincoli e Regole di Business

| Regola | Descrizione | Dove implementata |
|--------|-------------|-------------------|
| [Regola 1] | [descrizione] | `Application/Services/[Servizio].cs` |
| [Regola 2] | [descrizione] | `Domain/Entities/[Entità].cs` |

---

## 📈 Performance — Considerazioni

- **Query N+1**: evitare lazy loading su relazioni — usare `.Include()` esplicito
- **Paginazione**: obbligatoria per liste > 100 elementi
- **Cache**: [definire se/dove si usa caching]
- **Timeout query**: [definire soglie accettabili]

---

## 🔗 Dipendenze Esterne

| Sistema | Tipo | Scopo | Gestione fallimento |
|---------|------|-------|---------------------|
| [Sistema1] | [REST API/SQL/PLC] | [scopo] | [fallback/retry] |

---

*Versione: 1.0 — Da completare con logica specifica del progetto*
