// ============================================================
// Activo.cs — Entidad que representa una finca inscrita en el PSA
// "Activo" en el contexto del sistema Nativa es sinónimo de "finca"
// o predio que solicita pagos por servicios ambientales.
// La finca pasa por estados: Pendiente → EnRevision → Aprobada/Devuelta/Rechazada.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class Activo
{
    // Identificador único de la finca (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK al Sujeto que registró esta finca (debe tener Rol = Dueño).
    public int IdDueno { get; set; }

    // FK al Ingeniero asignado para evaluar esta finca.
    // Es null hasta que el ingeniero "toma" la finca de la cola FIFO.
    public int? IdIngeniero { get; set; }

    // Extensión de la finca en hectáreas. Precisión: 10 dígitos, 4 decimales.
    // Ejemplo: 123.4567 ha
    [Column(TypeName = "decimal(10,4)")]
    public decimal Hectareas { get; set; }

    // Porcentaje de cobertura vegetal de la finca (0-100).
    // Influye en el cálculo del monto mensual PSA.
    [Column(TypeName = "decimal(5,2)")]
    public decimal Vegetacion { get; set; }

    // Porcentaje de recursos hídricos presentes en la finca (0-100).
    // Influye en el cálculo del monto mensual PSA.
    [Column(TypeName = "decimal(5,2)")]
    public decimal Hidrologia { get; set; }

    // Porcentaje de topografía especial (pendientes, protección de cuencas, etc.).
    // Influye en el cálculo del monto mensual PSA.
    [Column(TypeName = "decimal(5,2)")]
    public decimal Topografia { get; set; }

    // Indica si la finca pertenece a un propietario nacional (costarricense).
    // Los nacionales reciben un porcentaje adicional en el pago PSA.
    public bool EsNacional { get; set; }

    // Latitud geográfica de la ubicación de la finca. Precisión: 9 dígitos, 6 decimales.
    // Usado para mostrar la finca en el mapa Leaflet.
    [Column(TypeName = "decimal(9,6)")]
    public decimal Lat { get; set; }

    // Longitud geográfica de la ubicación de la finca. Precisión: 9 dígitos, 6 decimales.
    [Column(TypeName = "decimal(9,6)")]
    public decimal Lng { get; set; }

    // Estado actual de la finca en el flujo de evaluación PSA.
    // Por defecto "Pendiente" al registrarse.
    public EstadoActivoEnum Estado { get; set; } = EstadoActivoEnum.Pendiente;

    // Fecha en que se registró la finca en el sistema.
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Observaciones del ingeniero al momento de emitir su dictamen.
    // Requeridas cuando el dictamen es "Devuelta" para indicar qué corregir.
    [MaxLength(2000)]
    public string? Observaciones { get; set; }

    // Token de concurrencia optimista. Evita que dos usuarios editen
    // la misma finca al mismo tiempo sin conflicto detectado.
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // Fecha UTC en que se creó el registro.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Sujeto dueño de esta finca (cargado via FK IdDueno).
    [ForeignKey(nameof(IdDueno))]
    public Sujeto Dueno { get; set; } = null!;

    // Sujeto ingeniero asignado. Puede ser null si aún no se tomó de la cola.
    [ForeignKey(nameof(IdIngeniero))]
    public Sujeto? Ingeniero { get; set; }

    // Documentos adjuntos (fotos, planos, certificados) subidos al blob storage.
    public ICollection<AdjuntoActivo> Adjuntos { get; set; } = [];

    // Planes de pago PSA asociados a esta finca.
    // Cada vez que se aprueba la finca, se puede activar un nuevo plan.
    public ICollection<PlanPago> Planes { get; set; } = [];
}
