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

// ── Preventivo: apre finestra di stampa isolata ───────────────────────────────
window.mesPreventivoPrint = function (html) {
    const win = window.open('', '_blank', 'width=900,height=700');
    if (!win) { alert('Popup bloccato dal browser. Consenti i popup per questa pagina e riprova.'); return; }
    win.document.open();
    win.document.write(html);
    win.document.close();
    win.focus();
    setTimeout(function () { win.print(); }, 800);
};
