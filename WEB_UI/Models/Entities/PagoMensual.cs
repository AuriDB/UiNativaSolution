// ============================================================
// PagoMensual.cs — Cuota mensual individual de un plan PSA
// Al activarse un PlanPago, se crean automáticamente 12 registros
// de PagoMensual (uno por mes). Un BackgroundService en la API
// revisa diariamente qué pagos están vencidos y los ejecuta,
// cambiando su estado de "Pendiente" a "Ejecutado".
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class PagoMensual
{
    // Identificador único de este pago mensual (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK al plan de pago al que pertenece esta cuota.
    public int IdPlan { get; set; }

    // Número secuencial de la cuota dentro del plan (1 al 12).
    // Permite identificar en qué mes del plan se encuentra.
    public int NumeroPago { get; set; }

    // Monto en colones de esta cuota. Igual al MontoMensual del PlanPago.
    [Column(TypeName = "decimal(12,2)")]
    public decimal Monto { get; set; }

    // Fecha programada de ejecución del pago (calculada como FechaActivacion + N meses).
    public DateTime FechaPago { get; set; }

    // Estado actual de la cuota: Pendiente (aún no vence) o Ejecutado (ya procesado).
    // El BackgroundService lo cambia a "Ejecutado" cuando FechaPago <= hoy.
    public EstadoPagoEnum Estado { get; set; } = EstadoPagoEnum.Pendiente;

    // Fecha y hora exacta en que el BackgroundService ejecutó el pago.
    // Es null mientras el pago está pendiente.
    public DateTime? FechaEjecucion { get; set; }

    // Fecha UTC en que se creó el registro.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Plan de pago al que pertenece esta cuota (cargado via FK IdPlan).
    [ForeignKey(nameof(IdPlan))]
    public PlanPago Plan { get; set; } = null!;
}
