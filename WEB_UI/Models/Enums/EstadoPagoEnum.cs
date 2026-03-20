// ============================================================
// EstadoPagoEnum.cs — Estado de una cuota mensual PSA
// Cada PagoMensual nace en estado Pendiente y el BackgroundService
// de la API lo cambia a Ejecutado cuando llega la fecha programada.
// ============================================================

namespace WEB_UI.Models.Enums;

public enum EstadoPagoEnum
{
    // El pago aún no se ha procesado. La fecha de ejecución es futura
    // o aún no ha sido procesada por el BackgroundService.
    Pendiente = 1,

    // El BackgroundService procesó el pago exitosamente.
    // Se registró la FechaEjecucion en el momento del procesamiento.
    Ejecutado = 2
}
