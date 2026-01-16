// Gantt Macchine - Vis-Timeline chart for machine scheduling with drag & drop
window.GanttMacchine = {
    timeline: null,
    
    initialize: function(elementId) {
        console.log('Initializing Vis-Timeline chart for element:', elementId);
        
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

        // Define groups (one per machine)
        const groups = [
            { id: 'machine_01', content: 'Macchina 01' },
            { id: 'machine_02', content: 'Macchina 02' },
            { id: 'machine_03', content: 'Macchina 03' }
        ];

        // Define items (commesse) with progress - multiple items can be on the same group (machine)
        const items = [
            // Commesse for Macchina 01
            { 
                id: 1, 
                group: 'machine_01', 
                content: 'C001 (65%)', 
                start: '2026-01-16', 
                end: '2026-01-18', 
                className: 'commessa-item',
                style: 'background: linear-gradient(to right, #2196F3 65%, rgba(33, 150, 243, 0.3) 65%); color: white;'
            },
            { 
                id: 2, 
                group: 'machine_01', 
                content: 'C002 (30%)', 
                start: '2026-01-19', 
                end: '2026-01-21', 
                className: 'commessa-item',
                style: 'background: linear-gradient(to right, #4CAF50 30%, rgba(76, 175, 80, 0.3) 30%); color: white;'
            },
            
            // Commesse for Macchina 02
            { 
                id: 3, 
                group: 'machine_02', 
                content: 'C003 (80%)', 
                start: '2026-01-16', 
                end: '2026-01-20', 
                className: 'commessa-item',
                style: 'background: linear-gradient(to right, #FF9800 80%, rgba(255, 152, 0, 0.3) 80%); color: white;'
            },
            { 
                id: 4, 
                group: 'machine_02', 
                content: 'C004 (15%)', 
                start: '2026-01-20', 
                end: '2026-01-23', 
                className: 'commessa-item',
                style: 'background: linear-gradient(to right, #9C27B0 15%, rgba(156, 39, 176, 0.3) 15%); color: white;'
            },
            
            // Commesse for Macchina 03
            { 
                id: 5, 
                group: 'machine_03', 
                content: 'C005 (45%)', 
                start: '2026-01-17', 
                end: '2026-01-22', 
                className: 'commessa-item',
                style: 'background: linear-gradient(to right, #F44336 45%, rgba(244, 67, 54, 0.3) 45%); color: white;'
            }
        ];

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
            groupOrder: 'id',
            margin: {
                item: 10,
                axis: 5
            },
            start: '2026-01-15',
            end: '2026-01-25'
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


