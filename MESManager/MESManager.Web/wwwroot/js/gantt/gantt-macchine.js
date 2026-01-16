// Gantt Macchine - Gantt chart for machine scheduling
window.GanttMacchine = {
    ganttInstance: null,
    
    initialize: function(elementId) {
        console.log('Initializing Gantt chart for element:', elementId);
        
        // Check if Gantt library is loaded
        if (typeof Gantt === 'undefined') {
            console.error('Gantt library not loaded!');
            return;
        }
        
        const container = document.getElementById(elementId);
        if (!container) {
            console.error('Gantt container not found:', elementId);
            return;
        }
        
        console.log('Container found:', container);

        // Test data - rappresenta commesse su diverse macchine
        const tasks = [
            {
                id: 'M1-C001',
                name: 'Macchina 1 - Commessa C001',
                start: '2026-01-16',
                end: '2026-01-18',
                progress: 45
            },
            {
                id: 'M1-C002',
                name: 'Macchina 1 - Commessa C002',
                start: '2026-01-19',
                end: '2026-01-21',
                progress: 0,
                dependencies: 'M1-C001'
            },
            {
                id: 'M2-C003',
                name: 'Macchina 2 - Commessa C003',
                start: '2026-01-16',
                end: '2026-01-20',
                progress: 60
            },
            {
                id: 'M2-C004',
                name: 'Macchina 2 - Commessa C004',
                start: '2026-01-20',
                end: '2026-01-23',
                progress: 25,
                dependencies: 'M2-C003'
            },
            {
                id: 'M3-C005',
                name: 'Macchina 3 - Commessa C005',
                start: '2026-01-17',
                end: '2026-01-22',
                progress: 10
            }
        ];

        console.log('Tasks prepared:', tasks.length, 'tasks');

        try {
            console.log('Creating Gantt instance...');
            this.ganttInstance = new Gantt(container, tasks, {
                view_mode: 'Day',
                bar_height: 30,
                bar_corner_radius: 3,
                arrow_curve: 5,
                padding: 18,
                on_click: function(task) {
                    console.log('Task clicked:', task);
                },
                on_date_change: function(task, start, end) {
                    console.log('Task date changed:', task, start, end);
                },
                on_progress_change: function(task, progress) {
                    console.log('Task progress changed:', task, progress);
                },
                on_view_change: function(mode) {
                    console.log('View mode changed:', mode);
                }
            });
            
            console.log('Gantt chart initialized successfully');
        } catch (error) {
            console.error('Error initializing Gantt chart:', error);
        }
    },
    
    updateTasks: function(tasks) {
        if (this.ganttInstance) {
            this.ganttInstance.refresh(tasks);
        }
    },
    
    changeViewMode: function(mode) {
        if (this.ganttInstance) {
            this.ganttInstance.change_view_mode(mode);
        }
    }
};
