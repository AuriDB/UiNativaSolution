using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class PlanPago
{
    [Key]
    public int Id { get; set; }

    public int IdActivo { get; set; }

    public int IdIngeniero { get; set; }

    public DateTime FechaActivacion { get; set; } = DateTime.UtcNow;

    /// <summary>JSON de ParametrosPago al activar — INMUTABLE.</summary>
    [Required]
    public string SnapshotParametrosJson { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal MontoMensual { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey(nameof(IdActivo))]
    public Activo Activo { get; set; } = null!;

    [ForeignKey(nameof(IdIngeniero))]
    public Sujeto Ingeniero { get; set; } = null!;

    public ICollection<PagoMensual> Pagos { get; set; } = [];
}
