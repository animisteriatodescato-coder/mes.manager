// Gantt Macchine - Costanti di configurazione
// VERSIONE v18 - FASE 1 Refactoring

// ==========================================
// TIMING E PERFORMANCE
// ==========================================
/** Intervallo aggiornamento % avanzamento (ms) */
export const PROGRESS_UPDATE_INTERVAL_MS = 60000; // 60 secondi

/** Intervallo retry SignalR in caso di disconnessione (ms) */
export const SIGNALR_RETRY_DELAYS_MS = [0, 2000, 10000, 30000]; // Retry immediato, poi 2s, 10s, 30s

// ==========================================
// VALORI DEFAULT PRODUZIONE
// ==========================================
/** Minuti lavorativi di default per commessa senza dati (8 ore) */
export const DEFAULT_WORKING_MINUTES = 480;

/** Minuti setup default se non specificato */
export const DEFAULT_SETUP_MINUTES = 90;

// ==========================================
// COLORI STATI COMMESSA
// ==========================================
export const STATUS_COLORS = {
    'InProgrammazione': '#2196F3', // Blu
    'Programmata': '#4CAF50',      // Verde
    'InProduzione': '#FF9800',     // Arancione
    'InCorso': '#FF9800',          // Arancione (alias)
    'Completata': '#9E9E9E',       // Grigio
    'Sospesa': '#F44336',          // Rosso
    'Default': '#607D8B'           // Grigio scuro
};

// ==========================================
// GANTT CHART CONFIGURAZIONE
// ==========================================
/** Margine item Gantt (px) */
export const GANTT_ITEM_MARGIN = 10;

/** Margine asse Gantt (px) */
export const GANTT_AXIS_MARGIN = 5;

/** Percentuale massima barra avanzamento */
export const MAX_PROGRESS_PERCENTAGE = 100;

/** Percentuale minima barra avanzamento */
export const MIN_PROGRESS_PERCENTAGE = 0;

// ==========================================
// VALIDAZIONE
// ==========================================
/** Range valido numero macchina */
export const MACHINE_NUMBER_MIN = 1;
export const MACHINE_NUMBER_MAX = 99;

/** Priorità commessa default */
export const DEFAULT_PRIORITY = 100;

/** Range priorità (più basso = più urgente) */
export const PRIORITY_MIN = 1;
export const PRIORITY_MAX = 999;
