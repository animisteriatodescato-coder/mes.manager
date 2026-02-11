using MESManager.Application.DTOs;

namespace MESManager.Application.Interfaces;

/// <summary>
/// Interfaccia per il servizio di sincronizzazione PLC
/// </summary>
public interface IPlcSyncService
{
    /// <summary>
    /// Evento scatenato quando cambia il barcode su DB55 (indica cambio commessa)
    /// </summary>
    event EventHandler<CommessaCambiataEventArgs>? CommessaCambiata;
}
