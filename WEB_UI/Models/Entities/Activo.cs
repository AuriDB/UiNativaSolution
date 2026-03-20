using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class Activo
{
    [Key]
    public int Id { get; set; }

    public int IdDueno { get; set; }

    public int? IdIngeniero { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal Hectareas { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Vegetacion { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Hidrologia { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Topografia { get; set; }

    public bool EsNacional { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Lat { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Lng { get; set; }

    public EstadoActivoEnum Estado { get; set; } = EstadoActivoEnum.Pendiente;

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? Observaciones { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(IdDueno))]
    public Sujeto Dueno { get; set; } = null!;

    [ForeignKey(nameof(IdIngeniero))]
    public Sujeto? Ingeniero { get; set; }

    public ICollection<AdjuntoActivo> Adjuntos { get; set; } = [];
    public ICollection<PlanPago> Planes { get; set; } = [];
}
