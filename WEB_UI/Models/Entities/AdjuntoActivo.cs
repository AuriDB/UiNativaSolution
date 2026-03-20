// ============================================================
// AdjuntoActivo.cs — Archivo adjunto de una finca
// Representa un documento (foto, plano, certificado) subido
// por el dueño al momento de registrar su finca. Los archivos
// se almacenan en Azure Blob Storage; aquí solo se guarda la URL.
// El ingeniero los revisa durante la evaluación.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class AdjuntoActivo
{
    // Identificador único del adjunto (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK a la finca (Activo) a la que pertenece este adjunto.
    public int IdActivo { get; set; }

    // URL completa del archivo en Azure Blob Storage.
    // Ejemplo: "https://nativastorage.blob.core.windows.net/adjuntos/abc123.pdf"
    // La API genera esta URL al subir el archivo al blob.
    [Required]
    public string BlobUrl { get; set; } = string.Empty;

    // Nombre original del archivo tal como lo subió el usuario.
    // Máximo 500 caracteres. Ejemplo: "plano_finca_norte.pdf"
    [Required, MaxLength(500)]
    public string NombreArchivo { get; set; } = string.Empty;

    // Fecha y hora UTC en que el archivo fue subido al blob storage.
    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    // Fecha UTC en que se creó el registro en la base de datos.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Finca a la que pertenece este adjunto (cargado via FK IdActivo).
    [ForeignKey(nameof(IdActivo))]
    public Activo Activo { get; set; } = null!;
}
