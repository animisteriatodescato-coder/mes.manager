// File download utility for Preventivi module
window.downloadFile = function (bytes, fileName, contentType) {
    // Convert base64 to blob if needed
    let blob;
    if (typeof bytes === 'string') {
        // If it's a base64 string
        const byteCharacters = atob(bytes);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        blob = new Blob([byteArray], { type: contentType });
    } else {
        // If it's already a Uint8Array from Blazor
        blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    }

    // Create download link
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    
    // Trigger download
    document.body.appendChild(link);
    link.click();
    
    // Cleanup
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Download tramite DotNetStreamReference (consigliato per file grandi in Blazor Server)
// Evita la serializzazione base64 via SignalR che può corrompere file binari grandi
window.downloadFileFromStream = async function (fileName, contentStreamRef) {
    const arrayBuffer = await contentStreamRef.arrayBuffer();
    const blob = new Blob([arrayBuffer], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Print PDF directly
window.printPdf = function (bytes, fileName) {
    let blob;
    if (typeof bytes === 'string') {
        const byteCharacters = atob(bytes);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        blob = new Blob([byteArray], { type: 'application/pdf' });
    } else {
        blob = new Blob([new Uint8Array(bytes)], { type: 'application/pdf' });
    }

    const url = URL.createObjectURL(blob);
    const printWindow = window.open(url);
    if (printWindow) {
        printWindow.onload = function () {
            printWindow.print();
        };
    }
};

// ── Preventivo: apre finestra di stampa con banner istruzioni "Salva come PDF" (v1.65.55) ──
// Usa la stessa logica di mesPreventivoPrint (che già funziona) ma aggiunge un banner
// blu con istruzioni + bottone "Salva come PDF". Il titolo della pagina diventa il nome file
// suggerito da Chrome/Edge nel dialog di salvataggio.
window.mesPreventivoDownloadPdf = function (htmlContent, fileName) {
    return new Promise(function (resolve) {
        var win = window.open('', '_blank', 'width=960,height=720');
        if (!win) {
            alert('Popup bloccato dal browser. Consenti i popup per questa pagina e riprova.');
            resolve();
            return;
        }

        // Imposta il titolo = nome file per far sì che Chrome lo suggerisca nel salvataggio
        var withTitle = htmlContent.replace(/<title>[^<]*<\/title>/i, '<title>' + fileName + '<\/title>');

        // Inietta <base href> per risorse CSS/font (stesso trick di mesPreventivoPrint)
        var baseTag = '<base href="' + window.location.origin + '/">';
        var injected = withTitle.replace(/<head>/i, '<head>' + baseTag);

        win.document.open();
        win.document.write(injected);
        win.document.close();
        win.focus();

        // Aspetta che il browser applichi CSS prima di iniettare il banner
        setTimeout(function () {
            try {
                // Banner istruzioni (nascosto durante la stampa via @media print)
                var banner = win.document.createElement('div');
                banner.className = 'mes-pdf-banner';
                banner.style.cssText =
                    'position:fixed;top:0;left:0;right:0;z-index:99999;' +
                    'background:#1565c0;color:white;padding:9px 20px;' +
                    'font-family:Arial,sans-serif;font-size:13px;' +
                    'display:flex;align-items:center;justify-content:space-between;gap:16px;' +
                    'box-shadow:0 2px 8px rgba(0,0,0,.35);';

                var msg = win.document.createElement('span');
                msg.innerHTML =
                    '📄 Premi <strong>Ctrl+P</strong> oppure il pulsante &rarr; ' +
                    'scegli destinazione <strong>&ldquo;Salva come PDF&rdquo;</strong> &rarr; ' +
                    'il nome file suggerito sarà <em>' + fileName + '</em>';

                var btn = win.document.createElement('button');
                btn.innerHTML = '🖨&nbsp; Salva come PDF';
                btn.style.cssText =
                    'background:white;color:#1565c0;border:none;' +
                    'padding:7px 18px;border-radius:4px;cursor:pointer;' +
                    'font-weight:bold;font-size:13px;white-space:nowrap;flex-shrink:0;';
                btn.onclick = function () { win.print(); };

                banner.appendChild(msg);
                banner.appendChild(btn);
                win.document.body.insertBefore(banner, win.document.body.firstChild);

                // Nascondi il banner durante la stampa effettiva
                var ps = win.document.createElement('style');
                ps.textContent = '@media print { .mes-pdf-banner { display:none !important; } }';
                win.document.head.appendChild(ps);
            } catch (e) { /* se il popup viene chiuso subito, ignora */ }
            resolve();
        }, 700);
    });
};

// ── Preventivo: apre eM Client (o qualunque client mail default) con oggetto e corpo preimpostati ──
window.mesPreventivoApriMail = function (destinatario, oggetto, corpo) {
    var mailto = 'mailto:' + encodeURIComponent(destinatario)
        + '?subject=' + encodeURIComponent(oggetto)
        + '&body=' + encodeURIComponent(corpo);
    var a = document.createElement('a');
    a.href = mailto;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

// ── Allegati Manutenzione Casse: upload diretto HTTP (bypassa SignalR, funziona su mobile) ──

// Converte HEIC/HEIF in JPEG via canvas (iPhone → browser desktop compatibile)
async function _cassaConvertToJpeg(file) {
    return new Promise(function (resolve) {
        try {
            var img = new window.Image();
            var url = URL.createObjectURL(file);
            img.onload = function () {
                var canvas = document.createElement('canvas');
                canvas.width = img.naturalWidth;
                canvas.height = img.naturalHeight;
                var ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0);
                canvas.toBlob(function (blob) {
                    URL.revokeObjectURL(url);
                    if (blob) {
                        var newName = file.name.replace(/\.(heic|heif)$/i, '.jpg');
                        resolve(new File([blob], newName, { type: 'image/jpeg' }));
                    } else {
                        resolve(file); // fallback: invia originale
                    }
                }, 'image/jpeg', 0.85);
            };
            img.onerror = function () {
                URL.revokeObjectURL(url);
                resolve(file); // fallback: invia originale
            };
            img.src = url;
        } catch (e) {
            resolve(file);
        }
    });
}

window.cassaAllegatoUpload = {
    openAndUpload: function (inputId, schedaId, dotnetRef) {
        const el = document.getElementById(inputId);
        if (!el) { console.error('[cassaAllegatoUpload] input non trovato: ' + inputId); return; }

        el.onchange = async function () {
            if (!el.files || el.files.length === 0) return;

            await dotnetRef.invokeMethodAsync('OnUploadStarted');

            const results = [];
            for (let i = 0; i < el.files.length; i++) {
                let file = el.files[i];
                // Converti HEIC/HEIF in JPEG (file iPhone) per compatibilità browser desktop
                if (/\.(heic|heif)$/i.test(file.name)) {
                    try { file = await _cassaConvertToJpeg(file); } catch (e) { /* usa originale */ }
                }
                const fd = new FormData();
                fd.append('file', file);
                try {
                    const resp = await fetch('/api/allegati-manutenzione-casse/upload/' + schedaId, {
                        method: 'POST',
                        body: fd
                    });
                    if (resp.ok) {
                        results.push({ Name: file.name, Success: true, Size: file.size, Error: null });
                    } else {
                        const msg = await resp.text();
                        results.push({ Name: file.name, Success: false, Size: file.size, Error: msg || 'HTTP ' + resp.status });
                    }
                } catch (err) {
                    results.push({ Name: file.name, Success: false, Size: file.size, Error: err.message || 'Errore di rete' });
                }
            }

            el.value = ''; // reset per permettere upload dello stesso file
            await dotnetRef.invokeMethodAsync('OnUploadComplete', results);
        };

        el.click();
    }
};

// ── Download file da base64 (allegati preventivi) ──────────────────────────
window.mesDownloadFile = function (base64, contentType, fileName) {
    var bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    var blob = new Blob([bytes], { type: contentType });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
};

// ── Preventivo: apre finestra di stampa isolata (solo base-href, senza fetch) ──
window.mesPreventivoPrint = function (html) {    const win = window.open('', '_blank', 'width=900,height=700');
    if (!win) { alert('Popup bloccato dal browser. Consenti i popup per questa pagina e riprova.'); return; }
    var baseTag = '<base href="' + window.location.origin + '/">';
    var injected = html.replace(/<head>/i, '<head>' + baseTag);
    win.document.open();
    win.document.write(injected);
    win.document.close();
    win.focus();
    setTimeout(function () { win.print(); }, 1000);
};

// ── Stampa scheda cassa: combina base64 C# + fetch autenticato per foto restanti ──
// Strategia tripla:
//   1. Foto già data-URI (precaricate da C#): usate direttamente
//   2. Foto con src relativo (/api/...): fetch con credenziali dalla pagina principale
//   3. Fetch fallita: placeholder testo (non immagine rotta)
// Infine: <base href> come ultimo fallback per risorse CSS/font.
window.mesStampaCassa = async function (html) {
    var parser = new DOMParser();
    var doc = parser.parseFromString(html, 'text/html');

    // Solo img con src relativo (non data URI, non external)
    var imgs = Array.from(doc.querySelectorAll('img[src]')).filter(function (img) {
        var src = img.getAttribute('src') || '';
        return src.startsWith('/') && !src.startsWith('//');
    });

    if (imgs.length > 0) {
        await Promise.all(imgs.map(async function (img) {
            var originalSrc = img.getAttribute('src');
            try {
                var resp = await fetch(originalSrc, { credentials: 'include' });
                if (resp.ok) {
                    var blob = await resp.blob();
                    var dataUrl = await new Promise(function (resolve, reject) {
                        var reader = new FileReader();
                        reader.onload = function () { resolve(reader.result); };
                        reader.onerror = reject;
                        reader.readAsDataURL(blob);
                    });
                    img.src = dataUrl;
                } else {
                    // File non trovato o auth fallita: sostituisci con testo placeholder
                    var p = doc.createElement('p');
                    p.textContent = '\u26a0 Immagine non disponibile: ' + (img.alt || originalSrc);
                    p.setAttribute('style', 'color:#999;font-style:italic;text-align:center;padding:8px;border:1px dashed #ccc;margin:0 0 8px');
                    if (img.parentNode) img.parentNode.replaceChild(p, img);
                }
            } catch (e) {
                console.warn('[mesStampaCassa] fetch fallita:', originalSrc, e);
                img.setAttribute('style', (img.getAttribute('style') || '') + ';opacity:0.2');
                img.title = 'Immagine non disponibile';
            }
        }));
    }

    // Inietta <base href> per CSS/font relativi (failsafe)
    var head = doc.querySelector('head');
    if (head && !head.querySelector('base')) {
        var base = doc.createElement('base');
        base.href = window.location.origin + '/';
        head.insertBefore(base, head.firstChild);
    }

    var newHtml = '<!DOCTYPE html>' + doc.documentElement.outerHTML;
    var win = window.open('', '_blank', 'width=900,height=700');
    if (!win) { alert('Popup bloccato dal browser. Consenti i popup per questa pagina e riprova.'); return; }
    win.document.open();
    win.document.write(newHtml);
    win.document.close();
    win.focus();
    setTimeout(function () { win.print(); }, 1500);
};

// ── Stampa con foto autenticate (legacy - mantenuta per compatibilità) ──────────
window.mesStampaConFoto = async function (html) {
    await window.mesStampaCassa(html);
    return true;
};
