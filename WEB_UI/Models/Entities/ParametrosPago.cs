// ============================================================
// ParametrosPago.cs — Parámetros globales de cálculo del pago PSA
// Solo puede haber un registro con Vigente = true en cualquier momento.
// El administrador crea nuevos parámetros; al hacerlo, el anterior
// queda con Vigente = false (historial). Los planes activos no se ven
// afectados porque usan el SnapshotParametrosJson al momento de activarse.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class ParametrosPago
{
    // Identificador único del conjunto de parámetros (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // Precio base en colones por hectárea para el cálculo PSA.
    // Ejemplo: si PrecioBase = 50000 y la finca tiene 10 ha → base = 500000.
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioBase { get; set; }

    // Factor multiplicador por cobertura vegetal (0.0000 - 1.0000).
    // Ejemplo: PctVegetacion = 0.30 significa que la vegetación aporta 30% extra.
    [Column(TypeName = "decimal(5,4)")]
    public decimal PctVegetacion { get; set; }

    // Factor multiplicador por recursos hídricos (0.0000 - 1.0000).
    [Column(TypeName = "decimal(5,4)")]
    public decimal PctHidrologia { get; set; }

    // Factor adicional para propietarios nacionales (0.0000 - 1.0000).
    // Solo se aplica si Activo.EsNacional = true.
    [Column(TypeName = "decimal(5,4)")]
    public decimal PctNacional { get; set; }

    // Factor multiplicador por topografía especial (0.0000 - 1.0000).
    [Column(TypeName = "decimal(5,4)")]
    public decimal PctTopografia { get; set; }

    // Límite máximo del monto mensual en colones.
    // Si el cálculo excede el tope, se usa el tope como monto final.
    [Column(TypeName = "decimal(5,4)")]
    public decimal Tope { get; set; }

    // Indica si este conjunto de parámetros es el actualmente en uso.
    // Solo un registro puede tener Vigente = true al mismo tiempo.
    public bool Vigente { get; set; }

    // Fecha UTC en que el administrador creó estos parámetros.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // FK al Sujeto administrador que creó estos parámetros.
    public int CreadoPor { get; set; }

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Administrador que configuró estos parámetros (cargado via FK CreadoPor).
    [ForeignKey(nameof(CreadoPor))]
    public Sujeto Admin { get; set; } = null!;
}
