using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class ParametrosPago
{
    [Key]
    public int Id { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioBase { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal PctVegetacion { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal PctHidrologia { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal PctNacional { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal PctTopografia { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal Tope { get; set; }

    public bool Vigente { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public int CreadoPor { get; set; }

    [ForeignKey(nameof(CreadoPor))]
    public Sujeto Admin { get; set; } = null!;
}
