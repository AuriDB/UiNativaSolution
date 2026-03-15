using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class CuentaBancaria
{
    [Key]
    public int Id { get; set; }

    public int IdDueno { get; set; }

    [Required, MaxLength(200)]
    public string Banco { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string TipoCuenta { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Titular { get; set; } = string.Empty;

    /// <summary>IBAN cifrado en BD (AES).</summary>
    [Required]
    public string IbanCompleto { get; set; } = string.Empty;

    /// <summary>IBAN ofuscado para vistas Admin: CR********************</summary>
    [Required, MaxLength(24)]
    public string IbanOfuscado { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    [ForeignKey(nameof(IdDueno))]
    public Sujeto Dueno { get; set; } = null!;
}
