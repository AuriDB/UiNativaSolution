// ============================================================
// CuentaBancaria.cs — Cuenta bancaria IBAN del dueño
// Cada dueño puede registrar una o más cuentas bancarias para
// recibir los pagos PSA. El IBAN completo se almacena cifrado
// con AES-256 en la API. En WEB_UI solo se muestra el IBAN ofuscado
// (ej: CR21 **** **** **** **** 01) por seguridad.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class CuentaBancaria
{
    // Identificador único de la cuenta bancaria (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK al Sujeto dueño al que pertenece esta cuenta bancaria.
    public int IdDueno { get; set; }

    // Nombre del banco (ej: "Banco Nacional de Costa Rica"). Máximo 200 caracteres.
    [Required, MaxLength(200)]
    public string Banco { get; set; } = string.Empty;

    // Tipo de cuenta bancaria (ej: "Corriente", "Ahorro"). Máximo 20 caracteres.
    [Required, MaxLength(20)]
    public string TipoCuenta { get; set; } = string.Empty;

    // Nombre del titular de la cuenta bancaria. Máximo 200 caracteres.
    // Puede ser diferente al nombre del dueño en el sistema.
    [Required, MaxLength(200)]
    public string Titular { get; set; } = string.Empty;

    // IBAN completo cifrado con AES-256 (almacenado como string en Base64).
    // La API descifra este valor cuando necesita procesarlo.
    // NUNCA se muestra al usuario en texto plano.
    [Required]
    public string IbanCompleto { get; set; } = string.Empty;

    // Versión ofuscada del IBAN para mostrar en pantalla de forma segura.
    // Ejemplo: "CR21 **** **** **** **** 01". Máximo 24 caracteres.
    [Required, MaxLength(24)]
    public string IbanOfuscado { get; set; } = string.Empty;

    // Indica si esta cuenta está activa. El dueño puede tener múltiples cuentas
    // pero solo una activa a la vez para recibir pagos.
    public bool Activo { get; set; } = true;

    // Fecha UTC en que se registró la cuenta bancaria.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Dueño al que pertenece esta cuenta (cargado via FK IdDueno).
    [ForeignKey(nameof(IdDueno))]
    public Sujeto Dueno { get; set; } = null!;
}
