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

        // Define items (commesse) - multiple items can be on the same group (machine)
        const items = [
            // Commesse for Macchina 01
            { id: 1, group: 'machine_01', content: 'C001', start: '2026-01-16', end: '2026-01-18', style: 'background-color: #2196F3; color: white;' },
            { id: 2, group: 'machine_01', content: 'C002', start: '2026-01-19', end: '2026-01-21', style: 'background-color: #4CAF50; color: white;' },
            
            // Commesse for Macchina 02
            { id: 3, group: 'machine_02', content: 'C003', start: '2026-01-16', end: '2026-01-20', style: 'background-color: #FF9800; color: white;' },
            { id: 4, group: 'machine_02', content: 'C004', start: '2026-01-20', end: '2026-01-23', style: 'background-color: #9C27B0; color: white;' },
            
            // Commesse for Macchina 03
            { id: 5, group: 'machine_03', content: 'C005', start: '2026-01-17', end: '2026-01-22', style: 'background-color: #F44336; color: white;' }
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

        // Event handlers
        this.timeline.on('changed', function () {
            console.log('Timeline changed');
        });

        this.timeline.on('itemover', function (properties) {
            console.log('Item over:', properties.item);
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


