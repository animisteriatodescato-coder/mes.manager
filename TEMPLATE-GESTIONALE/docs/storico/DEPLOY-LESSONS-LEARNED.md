# ⚠️ storico/DEPLOY-LESSONS-LEARNED.md

> Registro di tutto ciò che è andato storto nei deploy e come è stato risolto.
> **LEGGERE PRIMA DI OGNI DEPLOY.**

---

## Formato Voce

```
## [DD/MM/YYYY] — v[X.Y.Z] — [Titolo breve problema]

**Problema**: [Cosa è andato storto]
**Causa**: [Causa tecnica]
**Soluzione**: [Come è stato risolto]
**Prevenzione futura**: [Cosa fare per evitarlo]
**Impatto**: [Durata downtime, dati persi, ecc.]
```

---

## ✅ Lezioni Sempre Attive (regole fisse)

1. **NON sovrascrivere appsettings.Secrets.json** — usare `/XF` in robocopy
2. **NON sovrascrivere appsettings.Database.json** — usare `/XF` in robocopy
3. **Ferma i servizi prima di copiare** — ordine: Worker → Web
4. **Verifica versione dopo deploy** — controlla AppVersion nel HTML
5. **Backup DB prima di migration** — sempre, senza eccezioni
6. **Test in DEV prima di PROD** — ogni feature deve funzionare in locale

---

## Storico

*(Compilare durante il progetto)*

---

*Versione: 1.0 — Aggiornare dopo ogni deploy con problemi*
