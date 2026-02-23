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
