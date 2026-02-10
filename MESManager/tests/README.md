# Test Suite MESManager

Tutti i test sono centralizzati nella cartella `tests/`.

## Struttura

```
tests/
├── MESManager.E2E/          # Progetto E2E Playwright
├── run-area.ps1             # Esegue tutti i test per area
└── update-baselines.ps1     # Rigenera le baseline visuali
```

## Esecuzione per area (automatico)

```powershell
# Programma (Commesse + Gantt + Programma Macchine)
.\tests\run-area.ps1 -Area Programma -UseExistingServer -Seed

# Cataloghi
.\tests\run-area.ps1 -Area Cataloghi -UseExistingServer -Seed

# Produzione
.\tests\run-area.ps1 -Area Produzione -UseExistingServer -Seed

# Impostazioni
.\tests\run-area.ps1 -Area Impostazioni -UseExistingServer -Seed
```

## Baseline visuali

```powershell
.\tests\update-baselines.ps1 -UseExistingServer -Seed
```

## Variabili principali

- `E2E_USE_EXISTING_SERVER=1` -> usa app già avviata
- `E2E_BASE_URL=http://localhost:5156`
- `E2E_SEED=1` -> seed automatico DB

## Note

Quando chiedi di testare una parte del programma, verranno eseguiti **tutti** i test di quell'area (feature + visual).
