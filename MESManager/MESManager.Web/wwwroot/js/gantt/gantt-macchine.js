// Gantt Macchine - Vis-Timeline chart for machine scheduling with drag & drop and SignalR sync
window.GanttMacchine = {
    timeline: null,
    settings: null,
    hubConnection: null,
    machineMap: new Map(),
    reverseMachineMap: new Map(),
    isProcessingUpdate: false,
    dotNetHelper: null,
    
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

        // Define items (commesse) from real data
        let items = this.createItemsFromTasks(this.settings.tasks);

        // Configuration options
        const options = {
            editable: {
                add: false,
                updateTime: true,  // Enable drag to change time
                updateGroup: true, // Enable drag between groups (machines)
                remove: false,
                overrideItems: false
            },
            stack: false,  // Don't stack items - keep them on the same line (accodamento rigido)
            orientation: 'top',
            groupOrder: 'order',
            margin: {
                item: 10,
                axis: 5
            },
            snap: null, // Disable snapping to allow precise positioning
            start: items.length > 0 ? new Date(Math.min(...items.map(i => new Date(i.start)))) : new Date(),
            end: items.length > 0 ? new Date(Math.max(...items.map(i => new Date(i.end)))) : new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
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
            
            const allItems = self.timeline.itemsData.get();
            const itemsInGroup = allItems.filter(i => i.group === item.group && i.id !== item.id);
            
            // Find overlapping items and queue after them
            for (let other of itemsInGroup) {
                const otherStart = new Date(other.start).getTime();
                const otherEnd = new Date(other.end).getTime();
                const itemStart = new Date(item.start).getTime();
                const itemEnd = new Date(item.end).getTime();
                
                // Check if overlapping
                if (itemStart < otherEnd && itemEnd > otherStart) {
                    // Queue after the other item
                    const duration = itemEnd - itemStart;
                    item.start = new Date(otherEnd);
                    item.end = new Date(otherEnd + duration);
                    break;
                }
            }
            
            callback(item);
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

                // Extract machine number from group code
                const targetMacchina = self.reverseMachineMap.get(item.group);
                
                if (!targetMacchina) {
                    console.error('Could not determine target machine for group:', item.group);
                    callback(null);
                    return;
                }

                console.log(`Moving item ${item.id} to machine ${targetMacchina} at ${item.start}`);

                try {
                    // Call server API to persist the move
                    const response = await fetch('/api/pianificazione/sposta', {
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
                        const errorData = await response.json();
                        console.error('Server error:', errorData);
                        alert('Errore spostamento: ' + (errorData.errorMessage || 'Errore sconosciuto'));
                        callback(null); // Cancel the move
                        return;
                    }

                    const result = await response.json();
                    console.log('Move result:', result);

                    if (result.success) {
                        // Update all items with server-calculated positions
                        self.updateItemsFromServer(result.commesseAggiornate);
                        
                        if (result.commesseMacchinaOrigine) {
                            self.updateItemsFromServer(result.commesseMacchinaOrigine);
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

        console.log('Vis-Timeline chart initialized successfully');
    },

    createItemsFromTasks: function(tasks) {
        if (!tasks || tasks.length === 0) {
            console.warn('No tasks data available - Gantt will be empty');
            return [];
        }

        const self = this;
        return tasks
            .filter(task => task.dataInizio && task.dataFine && task.numeroMacchina)
            .map(task => {
                const groupId = self.machineMap.get(task.numeroMacchina) || 'M01';
                const progress = task.percentualeCompletamento || 0;
                const baseColor = self.getStatusColor(task.stato);
                const progressStyle = `background: linear-gradient(to right, ${baseColor} ${progress}%, rgba(${self.hexToRgb(baseColor)}, 0.3) ${progress}%); color: white;`;
                
                // Aggiungi triangolino avviso se dati incompleti
                const warningIcon = task.datiIncompleti ? ' ⚠️' : '';
                
                return {
                    id: task.id,
                    group: groupId,
                    content: `${task.codice} (${Math.round(progress)}%)${warningIcon}`,
                    start: new Date(task.dataInizio),
                    end: new Date(task.dataFine),
                    className: 'commessa-item',
                    style: progressStyle,
                    title: self.createTooltip(task)
                };
            });
    },

    createTooltip: function(task) {
        const warningText = task.datiIncompleti ? '\n⚠️ ATTENZIONE: Dati incompleti (usato 8h standard)' : '';
        return `${task.description || task.codice}
Quantità: ${task.quantita || 0} ${task.uom || ''}
Durata: ${task.durataMinuti || 0} min
Stato: ${task.stato}
Ordine: ${task.ordineSequenza || '-'}${warningText}`;
    },

    updateItemsFromServer: function(commesse) {
        if (!commesse || !this.timeline) return;

        this.isProcessingUpdate = true;
        
        try {
            commesse.forEach(c => {
                const groupId = this.machineMap.get(c.numeroMacchina) || 'M01';
                const progress = c.percentualeCompletamento || 0;
                const baseColor = this.getStatusColor(c.stato);
                const progressStyle = `background: linear-gradient(to right, ${baseColor} ${progress}%, rgba(${this.hexToRgb(baseColor)}, 0.3) ${progress}%); color: white;`;

                // Aggiungi triangolino avviso se dati incompleti
                const warningIcon = c.datiIncompleti ? ' ⚠️' : '';
                
                const existingItem = this.timeline.itemsData.get(c.id);
                if (existingItem) {
                    this.timeline.itemsData.update({
                        id: c.id,
                        group: groupId,
                        content: `${c.codice} (${Math.round(progress)}%)${warningIcon}`,
                        start: new Date(c.dataInizioPrevisione),
                        end: new Date(c.dataFinePrevisione),
                        style: progressStyle,
                        title: this.createTooltip(c)
                    });
                } else {
                    this.timeline.itemsData.add({
                        id: c.id,
                        group: groupId,
                        content: `${c.codice} (${Math.round(progress)}%)${warningIcon}`,
                        start: new Date(c.dataInizioPrevisione),
                        end: new Date(c.dataFinePrevisione),
                        className: 'commessa-item',
                        style: progressStyle,
                        title: this.createTooltip(c)
                    });
                }
            });
        } finally {
            this.isProcessingUpdate = false;
        }
    },

    initSignalR: async function() {
        try {
            if (typeof signalR === 'undefined') {
                console.warn('SignalR library not loaded - real-time updates disabled');
                return;
            }

            this.hubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/pianificazione')
                .withAutomaticReconnect()
                .build();

            const self = this;

            this.hubConnection.on('PianificazioneUpdated', function(notification) {
                console.log('Received PianificazioneUpdated:', notification);
                
                if (notification.type === 'CommesseUpdated' && notification.commesseAggiornate) {
                    self.updateItemsFromServer(notification.commesseAggiornate);
                } else if (notification.type === 'FullRecalculation') {
                    // Reload all data
                    if (self.dotNetHelper) {
                        self.dotNetHelper.invokeMethodAsync('OnFullRecalculation');
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

    setDotNetHelper: function(helper) {
        this.dotNetHelper = helper;
    },
    
    getStatusColor: function(stato) {
        const statusColors = {
            'InProgrammazione': '#2196F3',
            'Programmata': '#4CAF50',
            'InCorso': '#FF9800',
            'Completata': '#9E9E9E',
            'Sospesa': '#F44336',
            'Default': '#607D8B'
        };
        return statusColors[stato] || statusColors['Default'];
    },
    
    hexToRgb: function(hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? 
            `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}` : 
            '96, 125, 139';
    },
    
    updateTasks: function(tasks) {
        if (this.timeline) {
            const items = this.createItemsFromTasks(tasks);
            this.timeline.setItems(items);
        }
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