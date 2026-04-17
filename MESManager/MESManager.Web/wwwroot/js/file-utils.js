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

// ── Preventivo: apre finestra di stampa isolata (solo base-href, senza fetch) ──
window.mesPreventivoPrint = function (html) {
    const win = window.open('', '_blank', 'width=900,height=700');
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
