using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class OtpSesion
{
    [Key]
    public int Id { get; set; }

    public int IdSujeto { get; set; }

    [Required]
    public string HashOtp { get; set; } = string.Empty;

    public DateTime Expiracion { get; set; }

    public bool Usada { get; set; }

    public int Intentos { get; set; }

    public DateTime? UltimoReenvio { get; set; }

    public int ConteoReenvios { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey(nameof(IdSujeto))]
    public Sujeto Sujeto { get; set; } = null!;
}
