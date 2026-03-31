# FIX Dark Mode AG Grid CSS Cascade — 31 Marzo 2026

## 📋 PROBLEMA SEGNALATO

**Data**: 31 Marzo 2026  
**Versioni interessate**: v1.60.29 → v1.60.38 (fix completato)  
**Pagina**: `/produzione/plc-storico` (AG Grid con paginazione e colonna `% Scarti`)

### Sintomi

1. Testo barra paginazione invisibile in dark mode: `1 to 100 of 663`, `Page 1 of 7`, `100 ▼` → nero su sfondo scuro
2. Colori colonna `% Scarti` (`mes-scarti-ok/warn/error`) non visibili: sfondo chiaro con testo quasi-invisibile
3. Testo footer-info (`Righe caricate: 791`, `Ultimo aggiornamento`) invisibile

---

## 🔍 ANALISI — 5 ITERAZIONI DI DIAGNOSI

### Iterazione 1 — Tentativo CSS in layout-config.css (v1.60.29)
**Ipotesi**: Regole mancanti  
**Azione**: Aggiunte regole in `layout-config.css`  
**Risultato**: ❌ Non funzionava → cache browser (version hardcoded `app.css?v=1588`)

### Iterazione 2 — Cache bust (v1.60.32)
**Ipotesi**: Cache browser  
**Azione**: `app.css?v=1590`, regole in app.css con alta specificità `html body .mud-theme-dark .footer-info`  
**Risultato**: ❌ CSS presenti nel file servito, ma ancora invisibili

### Iterazione 3 — Cascade order AG Grid CDN (v1.60.33)
**Diagnosi via fetch CDN**:
- `ag-grid.css` imposta `--ag-foreground-color: #000` sul root
- `ag-theme-alpine.css` imposta `--ag-foreground-color: #181d1f` in `.ag-theme-alpine`
- Solo `.ag-theme-alpine-dark` ha `--ag-foreground-color: #fff`
- In `App.razor`, `app.css` caricava **PRIMA** dei CSS AG Grid CDN → AG Grid sovrascriveva

**Fix v1.60.33**:
- Riordinato CSS in App.razor: MudBlazor → **AG Grid CDN** → app.css → layout-config.css
- Aggiunto override CSS vars in app.css: `--ag-foreground-color: var(--mes-row-text)` su `.mud-theme-dark .ag-theme-alpine`
- Aggiunta dark background grid: `--ag-background-color: #1e2030`

**Risultato**: ✅ Grid background scuro, ✅ testo righe visibile — ma ❌ `% Scarti` ancora invisibili, ❌ paginazione ancora invisibile

### Iterazione 4 — MainLayout cascade (root cause finale, v1.60.34-37)
**Diagnosi definitiva**:

Il `<style>` inline di `MainLayout.razor` contiene:
```css
.ag-theme-alpine .ag-cell,
.ag-theme-alpine-dark .ag-cell {
    color: var(--mes-row-text) !important;  /* colore chiaro in dark */
}
```

In Blazor Server, il tag `<style>` inline è **nel DOM AFTER** i `<link>` CSS esterni. Il browser processa il CSS nell'ordine in cui appare nel documento HTML:
1. `<link href="app.css">` (nel `<head>`)
2. `<style>` di MainLayout (renderizzato inline nel body/head da Blazor)

Con `!important` a **parità di specificità** (entrambi `0,2,0` = `.ag-theme-alpine .ag-cell`), l'**ultimo nel sorgente vince**. MainLayout vince sempre su app.css.

Quindi:
- `.mes-scarti-ok { color: #64b5f6 !important }` in app.css → specificità `0,2,0`
- `.ag-cell { color: var(--mes-row-text) !important }` in MainLayout → specificità `0,2,0`, ma **DOPO** → **vince**

Risultato: tutti i colori testo celle sovrascritta dal colore riga (#E6E6F0 in dark) = bianco su sfondo quasi-bianco = INVISIBILE.

---

## ✅ SOLUZIONE FINALE (v1.60.37)

### Principio
Spostare le regole `cellClassRules` e paginazione nello **stesso `<style>` block di MainLayout.razor** dove già vivono `mes-stato-aperta`, `mes-count-foto` ecc. (che funzionavano già correttamente per lo stesso motivo).

### Modifiche applicate

**1. `MainLayout.razor` — `:root` CSS vars block**:
```razor
--mes-scarti-ok-bg:       @(_isDarkMode ? "#1a3a5c" : "#e3f2fd");
--mes-scarti-ok-color:    @(_isDarkMode ? "#64b5f6" : "#1565c0");
--mes-scarti-warn-bg:     @(_isDarkMode ? "#4a2e00" : "#fff3e0");
--mes-scarti-warn-color:  @(_isDarkMode ? "#ffa726" : "#e65100");
--mes-scarti-error-bg:    @(_isDarkMode ? "#5c1022" : "#fce4ec");
--mes-scarti-error-color: @(_isDarkMode ? "#e57373" : "#b71c1c");
```

**2. `MainLayout.razor` — `<style>` AG Grid block** (aggiunto DOPO le regole già presenti):
```css
/* % Scarti (PlcStorico) */
.ag-theme-alpine .ag-cell.mes-scarti-ok {
    background-color: var(--mes-scarti-ok-bg) !important;
    color: var(--mes-scarti-ok-color) !important;
    font-weight: bold;
}
.ag-theme-alpine .ag-cell.mes-scarti-warn { ... }
.ag-theme-alpine .ag-cell.mes-scarti-error { ... }

/* Barra paginazione */
.ag-theme-alpine .ag-paging-panel,
.ag-theme-alpine .ag-paging-panel span,
.ag-theme-alpine .ag-paging-row-summary-panel,
.ag-theme-alpine .ag-paging-page-summary-panel,
.ag-theme-alpine .ag-paging-page-size,
.ag-theme-alpine .ag-paging-page-size span,
.ag-theme-alpine .ag-paging-button,
.ag-theme-alpine .ag-picker-field-display {
    color: var(--mes-row-text) !important;
}
```

**3. `app.css` — rimosse le regole duplicate/inefficaci** che erano in conflitto.

---

## 📐 DIAGRAMMA CASCADE ORDER

```
Documento HTML (ordine rendering browser):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
<head>
 1. MudBlazor.min.css              (esterno, link)
 2. ag-grid.css CDN                (esterno, link)  ← v1.60.33: spostato prima
 3. ag-theme-alpine.css CDN        (esterno, link)  ← di app.css
 4. ag-theme-alpine-dark.css CDN   (esterno, link)
 5. app.css?v=1594                 (esterno, link)  ← le nostre regole globali
 6. layout-config.css?v=3          (esterno, link)
 7. MESManager.Web.styles.css      (esterno, link)
</head>
<body>
 8. <style> MainLayout.razor       (inline Blazor)  ← VINCE SEMPRE SU 1-7
    ":root { --mes-* vars }"
    ".ag-theme-alpine .ag-cell { color !important }"
    ".ag-theme-alpine .ag-cell.mes-scarti-ok { ... }"
</body>
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Regola: a parità di !important e specificità, VINCE L'ULTIMO NEL SORGENTE
```

---

## 📚 LESSON LEARNED

| # | Lezione | Regola |
|---|---------|--------|
| 1 | `<style>` inline Blazor carica DOPO tutti i `<link>` esterni | Qualsiasi regola in MainLayout `<style>` batte app.css a parità di specificità |
| 2 | `cellClassRules` AG Grid con colori dark/light → MainLayout | NON in app.css con `.mud-theme-dark` selector |
| 3 | CSS custom properties (`--var`) NON supportano `!important` | Usare specificity/cascade order per vincere, non `!important` sulle vars |
| 4 | `ag-theme-alpine` ha `--ag-foreground-color: #181d1f` (quasi nero) | In dark mode sovrascrivere TUTTE le background vars AG Grid, non solo foreground |
| 5 | Ordine `<link>` in App.razor è critico | AG Grid CDN CSS PRIMA di app.css, altrimenti sovrascrive a parità di specificità |

---

## 🗂️ FILE MODIFICATI (fix completo)

| File | Modifica |
|------|----------|
| `MESManager.Web/Components/App.razor` | Riordino links: AG Grid CDN prima di app.css |
| `MESManager.Web/wwwroot/app.css` | Dark background grid vars + rimosse regole inutili mes-scarti/paging |
| `MESManager.Web/Components/Layout/MainLayout.razor` | CSS vars mes-scarti-* + regole ag-cell + ag-paging-panel nel `<style>` block |

---

## 🔗 RIFERIMENTI

- BIBBIA: sezione "AG Grid: cellClassRules e paginazione DEVONO stare in MainLayout.razor"
- Commit: `fix(dark-mode): v1.60.33`, `fix(dark-mode): v1.60.34`, `fix(paginazione): v1.60.37`
