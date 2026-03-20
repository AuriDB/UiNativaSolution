using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class AdjuntoActivo
{
    [Key]
    public int Id { get; set; }

    public int IdActivo { get; set; }

    [Required]
    public string BlobUrl { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string NombreArchivo { get; set; } = string.Empty;

    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(IdActivo))]
    public Activo Activo { get; set; } = null!;
}
