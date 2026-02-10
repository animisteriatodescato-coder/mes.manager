# Verifica Stati Macchine - 13 Gennaio 2026

## Stato Reale (da verificare)
| Macchina | Stato Reale | NumeroOperatore Reale |
|----------|-------------|----------------------|
| M002 | MANUALE | 0 |
| M003 | MANUALE | 0 |
| M005 | MANUALE | 0 |
| M006 | SPENTA | 0 |
| M007 | MANUALE | 0 |
| M008 | MANUALE | 0 |
| M009 | MANUALE | 0 |
| M010 | MANUALE | 0 |

## Stato Letto dal Sistema
| Macchina | Stato Letto | NumeroOperatore Letto | Corretto? |
|----------|-------------|----------------------|-----------|
| M002 | MANUALE | 7 | ✓ Stato / ✗ Operatore |
| M003 | CICLO IN CORSO | 10 | ✗ Stato / ✗ Operatore |
| M005 | CICLO IN CORSO | 25 | ✗ Stato / ✗ Operatore |
| M006 | - | - | OFFLINE |
| M007 | MANUALE | 0 | ✓ Stato / ✓ Operatore |
| M008 | MANUALE | 0 | ✓ Stato / ✓ Operatore |
| M009 | MANUALE | 10 | ✓ Stato / ✗ Operatore |
| M010 | MANUALE | 9 | ✓ Stato / ✗ Operatore |

## Problemi Identificati

### Stati Macchina Errati
- **M003**: Legge "CICLO IN CORSO" invece di "MANUALE"
- **M005**: Legge "CICLO IN CORSO" invece di "MANUALE"

### NumeroOperatore Errati
Tutti i NumeroOperatore dovrebbero essere 0 ma vengono letti:
- **M002**: 7 (dovrebbe essere 0)
- **M003**: 10 (dovrebbe essere 0)
- **M005**: 25 (dovrebbe essere 0)
- **M009**: 10 (dovrebbe essere 0)
- **M010**: 9 (dovrebbe essere 0)

## Offset da Verificare

### StatoMacchina (offset 38)
- Verificare se l'offset è corretto per M003 e M005
- Possibile che il PLC scriva lo stato in modo diverso?

### OperatoreNumero (offset 22)
- Tutti gli offset sono uguali (22) ma i valori letti sono diversi
- Verificare se i PLC hanno effettivamente il valore 0 all'offset 22

## Azioni da Fare
1. [ ] Verificare offset StatoMacchina per M003 e M005 con lettura diretta PLC
2. [ ] Verificare offset OperatoreNumero per tutte le macchine
3. [ ] Pulire tabella PLCRealtime e fare nuova lettura con offset corretti
