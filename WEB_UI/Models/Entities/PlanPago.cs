// ============================================================
// PlanPago.cs — Plan de pago PSA activo para una finca aprobada
// Se crea cuando el ingeniero aprueba una finca y se activa el plan.
// Contiene un snapshot (foto) de los parámetros usados para calcular
// el monto, de modo que futuros cambios de parámetros no afecten
// planes ya vigentes. Genera 12 PagoMensual al activarse.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class PlanPago
{
    // Identificador único del plan de pago (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK a la finca (Activo) a la que pertenece este plan.
    public int IdActivo { get; set; }

    // FK al Ingeniero que aprobó la finca y activó el plan.
    public int IdIngeniero { get; set; }

    // Fecha en que se activó el plan de pago (inicio del período de 12 meses).
    public DateTime FechaActivacion { get; set; } = DateTime.UtcNow;

    // JSON con los parámetros de pago vigentes al momento de activar el plan
    // (precio base, porcentajes, tope). Se guarda para que el historial
    // refleje el cálculo correcto aunque los parámetros cambien después.
    [Required]
    public string SnapshotParametrosJson { get; set; } = string.Empty;

    // Monto calculado en colones que se pagará cada mes durante 12 meses.
    // Se calcula con la fórmula PSA al momento de activar el plan.
    [Column(TypeName = "decimal(12,2)")]
    public decimal MontoMensual { get; set; }

    // Fecha UTC en que se creó el registro del plan.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Finca a la que pertenece este plan (cargado via FK IdActivo).
    [ForeignKey(nameof(IdActivo))]
    public Activo Activo { get; set; } = null!;

    // Ingeniero que activó el plan (cargado via FK IdIngeniero).
    [ForeignKey(nameof(IdIngeniero))]
    public Sujeto Ingeniero { get; set; } = null!;

    // Los 12 pagos mensuales generados automáticamente al activar el plan.
    // Cada pago tiene su fecha de ejecución programada y estado (Pendiente/Ejecutado).
    public ICollection<PagoMensual> Pagos { get; set; } = [];
}
