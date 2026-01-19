// Gantt Macchine - Vis-Timeline chart for machine scheduling with drag & drop
window.GanttMacchine = {
    timeline: null,
    settings: null,
    
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

        // Define items (commesse) from real data
        let items = [];
        
        if (this.settings.tasks && this.settings.tasks.length > 0) {
            console.log('Processing real tasks:', this.settings.tasks);
            
            // Create a lookup map: numeroMacchina -> machineGroup
            const machineMap = new Map();
            this.settings.machines.forEach(m => {
                // Extract number from codice (e.g., "M01" -> 1, "M02" -> 2)
                const match = m.codice.match(/\d+/);
                if (match) {
                    const numMacchina = parseInt(match[0], 10);
                    machineMap.set(numMacchina, m.codice || m.id);
                }
            });
            
            console.log('Machine number mapping:', Array.from(machineMap.entries()));
            
            items = this.settings.tasks
                .filter(task => task.dataInizio && task.dataFine && task.numeroMacchina)
                .map(task => {
                    // Find the correct machine group using numeroMacchina
                    const groupId = machineMap.get(task.numeroMacchina) || groups[0]?.id;
                    
                    console.log(`Task ${task.codice}: numeroMacchina=${task.numeroMacchina} -> groupId=${groupId}`);
                    
                    // Calculate progress color
                    const progress = task.percentualeCompletamento || 0;
                    const baseColor = this.getStatusColor(task.stato);
                    const progressStyle = `background: linear-gradient(to right, ${baseColor} ${progress}%, rgba(${this.hexToRgb(baseColor)}, 0.3) ${progress}%); color: white;`;
                    
                    return {
                        id: task.id,
                        group: groupId,
                        content: `${task.codice} (${Math.round(progress)}%)`,
                        start: new Date(task.dataInizio),
                        end: new Date(task.dataFine),
                        className: 'commessa-item',
                        style: progressStyle,
                        title: `${task.description || task.codice}\nQuantità: ${task.quantita}\nStato: ${task.stato}`
                    };
                });
            
            console.log('Items created from real data:', items);
        } else {
            console.warn('No tasks data available - Gantt will be empty');
            items = [];
        }

        // Configuration options
        const options = {
            editable: {
                add: false,
                updateTime: true,  // Enable drag to change time
                updateGroup: true, // Enable drag between groups (machines)
                remove: false,
                overrideItems: false
            },
            stack: false,  // Don't stack items - keep them on the same line
            orientation: 'top',
            groupOrder: 'order',  // Order groups by the 'order' property
            margin: {
                item: 10,
                axis: 5
            },
            start: items.length > 0 ? new Date(Math.min(...items.map(i => new Date(i.start)))) : '2026-01-15',
            end: items.length > 0 ? new Date(Math.max(...items.map(i => new Date(i.end)))) : '2026-01-25'
        };

        // Create Timeline
        this.timeline = new vis.Timeline(container, items, groups, options);
        
        // Auto-queue: when moving an item, snap it to avoid overlaps
        const self = this;
        this.timeline.on('itemover', function (properties) {
            console.log('Item over:', properties.item);
        });
        
        // Handle item movement to auto-queue
        this.timeline.on('moving', function (item, callback) {
            const allItems = self.timeline.itemsData.get();
            const itemsInGroup = allItems.filter(i => i.group === item.group && i.id !== item.id);
            
            // Find overlapping items
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

        console.log('Vis-Timeline chart initialized successfully');
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
    
    updateTasks: function(items, groups) {
        if (this.timeline) {
            this.timeline.setGroups(groups);
            this.timeline.setItems(items);
        }
    },
    
    refresh: function() {
        if (this.timeline) {
            this.timeline.redraw();
        }
    }
};


