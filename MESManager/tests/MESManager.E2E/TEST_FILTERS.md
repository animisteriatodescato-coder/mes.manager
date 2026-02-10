# Test E2E - Filtri disponibili

## Esecuzione test per categoria

### Solo test Core (base dell'applicazione)
```powershell
dotnet test --filter "Category=Core"
```

### Solo test su pagine modificate
```powershell
dotnet test --filter "Category=Modified"
```

### Solo test sulla pagina Pianificazione
```powershell
dotnet test --filter "Page=Pianificazione"
```

### Tutti i test (Core + Modified)
```powershell
dotnet test
```

## Esecuzione con browser visibile

### Solo pagine modificate con browser visibile
```powershell
$env:PLAYWRIGHT_HEADED="1"; dotnet test --filter "Category=Modified"
```

### Solo Pianificazione con browser visibile
```powershell
$env:PLAYWRIGHT_HEADED="1"; dotnet test --filter "Page=Pianificazione"
```

## Combinazione di filtri

### Pianificazione modificata con browser visibile
```powershell
$env:PLAYWRIGHT_HEADED="1"; dotnet test --filter "Category=Modified&Page=Pianificazione"
```

## Categorie disponibili

- **Core**: Test fondamentali sempre eseguiti (es. Home page)
- **Modified**: Test sulle pagine recentemente modificate/aggiunte
- **Page=Pianificazione**: Test specifici per la pagina Pianificazione Gantt

## Note

- I test con `Category=Modified` sono quelli che dovresti eseguire dopo aver fatto modifiche
- I test `Category=Core` verificano che le funzionalità base non siano state rotte
- Usa `PLAYWRIGHT_HEADED="1"` per vedere il browser durante i test (utile per debug)
