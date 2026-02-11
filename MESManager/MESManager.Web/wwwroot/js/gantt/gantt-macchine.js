// Gantt Macchine - Vis-Timeline chart for machine scheduling with drag & drop and SignalR sync
// VERSIONE GANTT-FIRST: posizione esatta, no reload, avanzamento real-time - v20 NO MODULES

// Costanti inline (precedentemente in GanttConstants.js)
const PROGRESS_UPDATE_INTERVAL_MS = 60000;
const SIGNALR_RETRY_DELAYS_MS = [0, 2000, 10000, 30000];
const DEFAULT_WORKING_MINUTES = 480;
const STATUS_COLORS = {
    'NonProgrammata': '#9E9E9E',      // Grigio
    'Programmata': '#2196F3',         // Azzurro (era verde)
    'InProduzione': '#FF9800',        // Arancione
    'Completata': '#4CAF50',          // Verde (era grigio)
    'Archiviata': '#9E9E9E',          // Grigio
    'Aperta': '#2196F3',              // Azzurro
    'Chiusa': '#9E9E9E',              // Grigio
    'Default': '#607D8B'              // Grigio scuro
};
const GANTT_ITEM_MARGIN = 10;
const GANTT_AXIS_MARGIN = 5;
const MAX_PROGRESS_PERCENTAGE = 100;
const MIN_PROGRESS_PERCENTAGE = 0;
const MACHINE_NUMBER_MIN = 1;
const MACHINE_NUMBER_MAX = 99;

window.GanttMacchine = {
    timeline: null,
    settings: null,
    hubConnection: null,
    machineMap: new Map(),
    reverseMachineMap: new Map(),
    isProcessingUpdate: false,
    dotNetHelper: null,
    lastUpdateVersion: 0, // Traccia ultima versione aggiornamento ricevuta
    
    initialize: function(elementId, settings) {
        console.log('Initializing Vis-Timeline chart for element:', elementId);
        console.log('Settings received:', settings);
        
        this.settings = settings || {};
        
        // Check if vis library is loaded
        if (typeof vis === 'undefined') {
            console.error('Vis-Timeline library not loaded!');
            return;
        }
        
        const container = document.getElementById(elementId);
        if (!container) {
            console.error('Timeline container not found:', elementId);
            return;
        }
        
        console.log('Container found:', container);

        // Define groups from settings (machines) with explicit ordering
        const groups = this.settings.machines && this.settings.machines.length > 0
            ? this.settings.machines.map(m => ({ 
                id: m.codice || m.id, 
                content: m.nome,
                order: m.ordineVisualizazione || 0
            }))
            : [
                { id: 'M01', content: 'Macchina 01', order: 1 },
                { id: 'M02', content: 'Macchina 02', order: 2 },
                { id: 'M03', content: 'Macchina 03', order: 3 }
            ];

        console.log('Groups created:', groups);

        // Create machine mapping: numeroMacchina <-> machineCode
        this.machineMap.clear();
        this.reverseMachineMap.clear();
        this.settings.machines.forEach(m => {
            const match = m.codice.match(/\d+/);
            if (match) {
                const numMacchina = parseInt(match[0], 10);
                this.machineMap.set(numMacchina, m.codice || m.id);
                this.reverseMachineMap.set(m.codice || m.id, numMacchina);
            }
        });
        
        console.log('Machine number mapping:', Array.from(this.machineMap.entries()));

        // Define items (commesse) from real data - SENZA FILTRI DISTRUTTIVI
        let items = this.createItemsFromTasks(this.settings.tasks);

        // Configuration options - FIX SOVRAPPOSIZIONE
        const options = {
            editable: {
                add: false,
                updateTime: true,  // Enable drag to change time
                updateGroup: true, // Enable drag between groups (machines)
                remove: false,
                overrideItems: false
            },
            stack: false,  // ❌ DISABILITATO stack: le commesse POSSONO sovrapporsi sulla stessa riga
            stackSubgroups: false,
            orientation: 'top',
            groupOrder: 'order',
            margin: {
                item: {horizontal: 5, vertical: 8}, // Margine aumentato per separazione
                axis: 5
            },
            snap: function(date, scale, step) {
                // Snap a inizio ora lavorativa (8:00)
                const hour = 8 * 60 * 60 * 1000;
                return Math.round(date / hour) * hour;
            },
            verticalScroll: true,
            zoomable: true,
            moveable: true,
            start: items.length > 0 ? new Date(Math.min(...items.map(i => new Date(i.start)))) : new Date(),
            end: items.length > 0 ? new Date(Math.max(...items.map(i => new Date(i.end)))) : new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
            // Tooltip personalizzato (se supportato)
            tooltip: {
                followMouse: true,
                overflowMethod: 'cap'
            }
        };

        // Create Timeline
        this.timeline = new vis.Timeline(container, items, groups, options);
        
        const self = this;
        
        // Handle item movement - auto-queue on client side for visual feedback
        this.timeline.on('moving', function (item, callback) {
            if (self.isProcessingUpdate) {
                callback(null); // Cancel if processing server update
                return;
            }
            
            // LOCK: Verifica se item è bloccato
            if (item.bloccata) {
                console.warn('Tentativo di spostare commessa bloccata:', item.id);
                callback(null); // Blocca movimento
                return;
            }
            
            callback(item);
        });
        
        // Handle item selection
        this.timeline.on('select', function(properties) {
            console.log('Gantt select event:', properties);
            if (properties.items && properties.items.length > 0 && self.dotNetHelper) {
                const selectedId = properties.items[0];
                console.log('Calling OnCommessaSelected with ID:', selectedId);
                self.dotNetHelper.invokeMethodAsync('OnCommessaSelected', selectedId);
            } else {
                console.log('No items selected or dotNetHelper not set. Items:', properties.items, 'Helper:', self.dotNetHelper);
            }
        });

        // Handle item moved (after drop) - persist to server
        this.timeline.on('changed', async function () {
            // This event fires on any change, we handle specific moves in 'move' callback
        });

        // Use the 'move' callback for final position after drag
        this.timeline.setOptions({
            onMove: async function(item, callback) {
                if (self.isProcessingUpdate) {
                    callback(null);
                    return;
                }

                // LOCK: Double-check bloccata
                if (item.bloccata) {
                    alert('Impossibile spostare una commessa bloccata. Sbloccarla prima.');
                    callback(null);
                    return;
                }

                // Extract machine number from group code
                const targetMacchina = self.reverseMachineMap.get(item.group);
                
                // VALIDAZIONE ROBUSTA numero macchina
                if (!targetMacchina || isNaN(parseInt(targetMacchina)) || parseInt(targetMacchina) < MACHINE_NUMBER_MIN || parseInt(targetMacchina) > MACHINE_NUMBER_MAX) {
                    console.error('❌ Numero macchina non valido:', { group: item.group, targetMacchina, validRange: `${MACHINE_NUMBER_MIN}-${MACHINE_NUMBER_MAX}` });
                    alert(`Errore: numero macchina non valido. Range consentito: ${MACHINE_NUMBER_MIN}-${MACHINE_NUMBER_MAX}`);
                    callback(null);
                    return;
                }

                console.log(`✓ Moving item ${item.id} to machine ${targetMacchina} at ${item.start}`);

                try {
                    // Call server API to persist the move - URL CORRETTO
                    const response = await fetch('/api/pianificazione/sposta-commessa', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            commessaId: item.id,
                            targetMacchina: targetMacchina,
                            targetDataInizio: item.start.toISOString(),
                            insertBeforeCommessaId: null
                        })
                    });

                    if (!response.ok) {
                        let errorMessage = `Errore HTTP ${response.status}`;
                        try {
                            const errorData = await response.json();
                            errorMessage = errorData.errorMessage || errorMessage;
                        } catch (e) {
                            const errorText = await response.text();
                            console.error('❌ Response text:', errorText);
                        }
                        console.error('❌ Server error:', errorMessage);
                        alert('Errore spostamento: ' + errorMessage);
                        callback(null);
                        return;
                    }

                    const result = await response.json();
                    console.log('✓ Move result:', result);

                    if (result.success) {
                        // Update all items with server-calculated positions
                        self.updateItemsFromServer(result.commesseAggiornate, result.updateVersion);
                        
                        if (result.commesseMacchinaOrigine) {
                            self.updateItemsFromServer(result.commesseMacchinaOrigine, result.updateVersion);
                        }

                        // Notify Blazor component if available
                        if (self.dotNetHelper) {
                            self.dotNetHelper.invokeMethodAsync('OnCommessaMoved', result);
                        }

                        callback(item); // Accept the move
                    } else {
                        alert('Errore: ' + result.errorMessage);
                        callback(null); // Cancel the move
                    }
                } catch (error) {
                    console.error('Error moving item:', error);
                    alert('Errore di comunicazione con il server');
                    callback(null); // Cancel the move
                }
            }
        });

        // Initialize SignalR connection
        this.initSignalR();
        
        // v16: Update ciclico % avanzamento ogni 60 secondi
        this.startProgressUpdateTimer();

        console.log('Vis-Timeline chart initialized successfully');
    },
    
    /**
     * Avvia timer ciclico per aggiornamento % avanzamento commesse in produzione
     * Update ogni PROGRESS_UPDATE_INTERVAL_MS (60 secondi)
     */
    startProgressUpdateTimer: function() {
        const self = this;
        
        // Update ogni PROGRESS_UPDATE_INTERVAL_MS
        setInterval(function() {
            if (!self.timeline || !self.timeline.itemsData) return;
            
            const now = new Date();
            const items = self.timeline.itemsData.get();
            
            items.forEach(function(item) {
                // Solo per item con currentProgress definito (in produzione)
                if (item.currentProgress !== undefined && item.start && item.end) {
                    const start = new Date(item.start);
                    const end = new Date(item.end);
                    
                    if (now >= start && now <= end) {
                        const totalDuration = end - start;
                        const elapsed = now - start;
                        const newProgress = Math.min(MAX_PROGRESS_PERCENTAGE, Math.max(MIN_PROGRESS_PERCENTAGE, (elapsed / totalDuration) * 100));
                        
                        // Update content con nuova %
                        const contentMatch = item.content.match(/^(.*?)\s*\((\d+)%\)(.*)$/);
                        if (contentMatch) {
                            const updatedContent = `${contentMatch[1]} (${Math.round(newProgress)}%)${contentMatch[3]}`;
                            self.timeline.itemsData.update({
                                id: item.id,
                                content: updatedContent,
                                currentProgress: newProgress
                            });
                        }
                    }
                }
            });
        }, PROGRESS_UPDATE_INTERVAL_MS); // Costante configurabile
    },

    createItemsFromTasks: function(tasks) {
        if (!tasks || tasks.length === 0) {
            console.warn('No tasks data available - Gantt will be empty');
            return [];
        }

        const self = this;
        
        // ❌ RIMOSSO FILTRO DISTRUTTIVO: mostra TUTTE le commesse assegnate a macchina
        // Il backend deve garantire che abbiano date
        return tasks
            .filter(task => task.numeroMacchina != null) // Solo filtro: deve avere macchina
            .map(task => {
                const groupId = self.machineMap.get(task.numeroMacchina) || 'M01';
                
                // CALCOLO AVANZAMENTO: basato su finestra pianificata
                let progress = task.percentualeCompletamento || 0;
                if (task.dataInizioPrevisione && task.dataFinePrevisione) {
                    const now = new Date();
                    const start = new Date(task.dataInizioPrevisione);
                    const end = new Date(task.dataFinePrevisione);
                    if (now >= start && now <= end) {
                        const totalDuration = end - start;
                        const elapsed = now - start;
                        progress = Math.min(100, Math.max(0, (elapsed / totalDuration) * 100));
                    } else if (now > end) {
                        progress = 100;
                    } else {
                        progress = 0;
                    }
                }
                
                const statoProgramma = task.statoProgramma || '';
                // Usa statoProgramma o fallback a stato per determinare colore
                const statoPerColore = statoProgramma || task.stato;
                const baseColor = self.getStatusColor(statoPerColore);
                const isCompletata = statoProgramma === 'Completata';
                // ⚡ PERFORMANCE: Log rimossi dal loop (rallentavano rendering con molte commesse)
                
                // v17: Background con gradazione per avanzamento - scurisce per la parte completata
                const backgroundColor = task.bloccata ? '#d32f2f' : baseColor;
                const darkerColor = self.darkenColor(backgroundColor, 30); // Scurisce del 30%
                
                // Gradiente: parte completata più scura, parte rimanente normale
                const progressStyle = `background: linear-gradient(to right, ${darkerColor} 0%, ${darkerColor} ${progress}%, ${backgroundColor} ${progress}%, ${backgroundColor} 100%); color: white; font-weight: 500;`;
                
                // Icone stato speciale - PRIMA del codice
                let icons = '';
                if (task.datiIncompleti) icons += '⚠️ '; // Triangolino PRIMA
                if (task.bloccata) icons += '🔒 '; // Lucchetto
                if (task.vincoloDataFineSuperato) icons += '⚠️ '; // Vincolo superato
                
                // Priorità nel content se diversa da default
                let priorityIndicator = '';
                if (task.priorita && task.priorita < 100) {
                    priorityIndicator = ` [P${task.priorita}]`; // Alta priorità
                }
                
                // PERCENTUALE PRIMA del codice cassa
                const displayCode = task.codice || task.id;
                const percentagePrefix = `${Math.round(progress)}% `;
                
                // Fallback date (se non ci sono usiamo now, ma dovrebbero sempre esserci)
                const dataInizio = task.dataInizioPrevisione || task.dataInizio || new Date().toISOString();
                const dataFine = task.dataFinePrevisione || task.dataFine || new Date(Date.now() + 8*60*60*1000).toISOString();
                
                const classNames = [
                    'commessa-item',
                    task.bloccata ? 'commessa-bloccata' : null,
                    isCompletata ? 'commessa-completata' : null
                ].filter(Boolean).join(' ');

                return {
                    id: task.id,
                    group: groupId,
                    content: `${icons}${percentagePrefix}${displayCode}${priorityIndicator}`,
                    start: new Date(dataInizio),
                    end: new Date(dataFine),
                    className: classNames,
                    style: progressStyle,
                    title: self.createTooltip(task),
                    // Dati custom per drag handling
                    bloccata: task.bloccata || false,
                    priorita: task.priorita || 100,
                    vincoloDataInizio: task.vincoloDataInizio,
                    vincoloDataFine: task.vincoloDataFine,
                    datiIncompleti: task.datiIncompleti || false,
                    // Store progress per update
                    currentProgress: progress
                };
            });
    },

    createTooltip: function(task) {
        let tooltip = `${task.description || task.codice}
Quantità: ${task.quantita || task.quantitaRichiesta || 0} ${task.uom || task.uoM || ''}
Durata: ${task.durataMinuti || task.durataPrevistaMinuti || 0} min
Stato: ${task.stato}
Ordine: ${task.ordineSequenza || '-'}`;

        // Priorità
        if (task.priorita && task.priorita !== 100) {
            tooltip += `\nPriorità: ${task.priorita} ${task.priorita < 100 ? '(ALTA)' : ''}`;
        }

        // Vincoli
        if (task.vincoloDataInizio) {
            tooltip += `\n⏰ Inizio dopo: ${new Date(task.vincoloDataInizio).toLocaleString('it-IT')}`;
        }
        if (task.vincoloDataFine) {
            const vincoloStr = new Date(task.vincoloDataFine).toLocaleString('it-IT');
            if (task.vincoloDataFineSuperato) {
                tooltip += `\n⚠️ VINCOLO SUPERATO! Deve finire entro: ${vincoloStr}`;
            } else {
                tooltip += `\n⏰ Finire entro: ${vincoloStr}`;
            }
        }

        // Blocco
        if (task.bloccata) {
            tooltip += '\n🔒 BLOCCATA (non trascinabile)';
        }

        // Classe lavorazione
        if (task.classeLavorazione) {
            tooltip += `\nClasse: ${task.classeLavorazione}`;
        }

        // Warning dati incompleti
        if (task.datiIncompleti) {
            tooltip += '\n⚠️ Dati incompleti (usato 8h standard)';
        }

        return tooltip;
    },

    updateItemsFromServer: function(commesse, updateVersion) {
        if (!commesse || !this.timeline) return;

        // Verifica updateVersion per evitare aggiornamenti stali
        if (updateVersion && updateVersion <= this.lastUpdateVersion) {
            console.log(`⏭️ Skipping stale update: v${updateVersion} <= v${this.lastUpdateVersion}`);
            return;
        }

        // ✅ FIX: Usa try-finally per garantire rilascio flag
        this.isProcessingUpdate = true;
        
        // Debouncing: se arriva update troppo velocemente, accoda
        if (this._updateTimeout) {
            clearTimeout(this._updateTimeout);
        }
        
        try {
            if (updateVersion) {
                this.lastUpdateVersion = updateVersion;
                console.log(`Applying update v${updateVersion}`);
            }

            commesse.forEach(c => {
                const groupId = this.machineMap.get(c.numeroMacchina) || 'M01';
                let progress = c.percentualeCompletamento || 0;
                if (c.dataInizioPrevisione && c.dataFinePrevisione) {
                    const now = new Date();
                    const start = new Date(c.dataInizioPrevisione);
                    const end = new Date(c.dataFinePrevisione);
                    if (now >= start && now <= end) {
                        const totalDuration = end - start;
                        const elapsed = now - start;
                        progress = Math.min(100, Math.max(0, (elapsed / totalDuration) * 100));
                    } else if (now > end) {
                        progress = 100;
                    } else {
                        progress = 0;
                    }
                }
                const statoProgramma = c.statoProgramma || '';
                const baseColor = this.getStatusColor(statoProgramma || c.stato);
                const progressStyle = `background: linear-gradient(to right, ${baseColor} ${progress}%, rgba(${this.hexToRgb(baseColor)}, 0.3) ${progress}%); color: white;`;
                const isCompletata = statoProgramma === 'Completata';

                // Icone
                let icons = '';
                if (c.bloccata) icons += ' 🔒';
                if (c.vincoloDataFineSuperato) icons += ' ⚠️';
                if (c.datiIncompleti) icons += ' ⚠️';

                let priorityIndicator = '';
                if (c.priorita && c.priorita < 100) {
                    priorityIndicator = ` [P${c.priorita}]`;
                }
                
                const dataInizio = c.dataInizioPrevisione || c.dataInizio || new Date().toISOString();
                const dataFine = c.dataFinePrevisione || c.dataFine || new Date(Date.now() + 8*60*60*1000).toISOString();
                
                const existingItem = this.timeline.itemsData.get(c.id);
                const classNames = [
                    'commessa-item',
                    c.bloccata ? 'commessa-bloccata' : null,
                    isCompletata ? 'commessa-completata' : null
                ].filter(Boolean).join(' ');

                const itemData = {
                    id: c.id,
                    group: groupId,
                    content: `${c.codice} (${Math.round(progress)}%)${priorityIndicator}${icons}`,
                    start: new Date(dataInizio),
                    end: new Date(dataFine),
                    className: classNames,
                    style: progressStyle,
                    title: this.createTooltip(c),
                    bloccata: c.bloccata || false,
                    priorita: c.priorita || 100,
                    vincoloDataInizio: c.vincoloDataInizio,
                    vincoloDataFine: c.vincoloDataFine,
                    datiIncompleti: c.datiIncompleti || false,
                    currentProgress: progress
                };

                if (existingItem) {
                    this.timeline.itemsData.update(itemData);
                } else {
                    this.timeline.itemsData.add(itemData);
                }
            });
        } finally {
            // ✅ GARANTITO rilascio flag anche in caso di errore
            const self = this;
            this._updateTimeout = setTimeout(function() {
                self.isProcessingUpdate = false;
                console.log('✅ Update completed, flag released');
            }, 100); // Debounce 100ms
        }
    },

    /**
     * Inizializza la connessione SignalR con retry automatico configurabile
     * @returns {Promise<void>}
     */
    initSignalR: async function() {
        try {
            if (typeof signalR === 'undefined') {
                console.warn('SignalR library not loaded - real-time updates disabled');
                return;
            }

            this.hubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/pianificazione')
                .withAutomaticReconnect(SIGNALR_RETRY_DELAYS_MS) // Retry configurabile: 0ms, 2s, 10s, 30s
                .build();

            const self = this;

            this.hubConnection.on('PianificazioneUpdated', function(notification) {
                console.log('Received PianificazioneUpdated:', notification);
                
                // Verifica updateVersion per evitare update stali o loop
                if (notification.updateVersion && notification.updateVersion <= self.lastUpdateVersion) {
                    console.log(`Ignoring stale notification: v${notification.updateVersion} <= v${self.lastUpdateVersion}`);
                    return;
                }
                
                if (notification.type === 'CommesseUpdated' && notification.commesseAggiornate) {
                    self.updateItemsFromServer(notification.commesseAggiornate, notification.updateVersion);
                } else if (notification.type === 'FullRecalculation') {
                    // v16 GANTT-FIRST: NO location.reload() - aggiorna via Blazor
                    console.log('✓ Full recalculation - updating via Blazor (no page reload)');
                    if (self.dotNetHelper) {
                        self.dotNetHelper.invokeMethodAsync('OnFullRecalculation');
                    } else {
                        console.warn('⚠️ DotNetHelper not available - manual refresh required');
                    }
                }
            });

            await this.hubConnection.start();
            console.log('SignalR PianificazioneHub connected');

            // Subscribe to gantt updates
            await this.hubConnection.invoke('SubscribeToGantt');

        } catch (error) {
            console.error('SignalR connection error:', error);
        }
    },

    /**
     * Registra helper .NET per comunicazione bidirezionale Blazor ↔ JavaScript
     * @param {DotNetObjectReference} helper - Reference .NET object
     */
    setDotNetHelper: function(helper) {
        this.dotNetHelper = helper;
    },
    
    /**
     * Ottiene il colore associato allo stato commessa
     * @param {string} stato - Stato commessa
     * @returns {string} Codice colore hex
     */
    getStatusColor: function(stato) {
        return STATUS_COLORS[stato] || STATUS_COLORS['Default'];
    },
    
    hexToRgb: function(hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? 
            `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : 
            '96, 125, 139';
    },
    
    /**
     * Scurisce un colore hex di una percentuale specificata
     * @param {string} hex - Colore in formato hex (#RRGGBB)
     * @param {number} percent - Percentuale di scurimento (0-100)
     * @returns {string} Colore scurito in formato hex
     */
    darkenColor: function(hex, percent) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        if (!result) return hex;
        
        const r = Math.max(0, parseInt(result[1], 16) * (100 - percent) / 100);
        const g = Math.max(0, parseInt(result[2], 16) * (100 - percent) / 100);
        const b = Math.max(0, parseInt(result[3], 16) * (100 - percent) / 100);
        
        const toHex = (n) => {
            const hex = Math.round(n).toString(16);
            return hex.length === 1 ? '0' + hex : hex;
        };
        
        return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
    },
    
    updateTasks: function(tasks) {
        if (this.timeline) {
            const items = this.createItemsFromTasks(tasks);
            this.timeline.setItems(items);
        }
    },

    calculateProgress: function(dataInizioPrevisione, dataFinePrevisione, isCompletata) {
        if (isCompletata) {
            return 100;
        }

        if (!dataInizioPrevisione || !dataFinePrevisione) {
            return 0;
        }

        const now = new Date();
        const start = new Date(dataInizioPrevisione);
        const end = new Date(dataFinePrevisione);

        if (now <= start) {
            return 0;
        }

        if (now >= end) {
            return 100;
        }

        const totalDuration = end - start;
        const elapsed = now - start;
        return Math.min(100, Math.max(0, (elapsed / totalDuration) * 100));
    },
    
    refresh: function() {
        if (this.timeline) {
            this.timeline.redraw();
        }
    },

    dispose: function() {
        if (this.hubConnection) {
            this.hubConnection.stop();
            this.hubConnection = null;
        }
        if (this.timeline) {
            this.timeline.destroy();
            this.timeline = null;
        }
    }
};