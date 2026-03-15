using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class Sujeto
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Cedula { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public RolEnum Rol { get; set; }

    public EstadoSujetoEnum Estado { get; set; } = EstadoSujetoEnum.Activo;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Recuperación de contraseña (token HMAC, un solo uso)
    public string? PasswordResetHash { get; set; }
    public DateTime? PasswordResetExpira { get; set; }

    // Navegación
    public ICollection<Activo> Fincas { get; set; } = [];
    public ICollection<CuentaBancaria> CuentasBancarias { get; set; } = [];
    public ICollection<OtpSesion> OtpSesiones { get; set; } = [];
}
