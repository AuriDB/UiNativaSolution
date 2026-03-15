using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class PagoMensual
{
    [Key]
    public int Id { get; set; }

    public int IdPlan { get; set; }

    public int NumeroPago { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Monto { get; set; }

    public DateTime FechaPago { get; set; }

    public EstadoPagoEnum Estado { get; set; } = EstadoPagoEnum.Pendiente;

    public DateTime? FechaEjecucion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey(nameof(IdPlan))]
    public PlanPago Plan { get; set; } = null!;
}
