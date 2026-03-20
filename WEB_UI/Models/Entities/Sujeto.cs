// ============================================================
// Sujeto.cs — Entidad que representa a cualquier usuario del sistema
// Cubre los tres roles: Dueño, Ingeniero y Admin.
// En WEB_UI se usa como modelo para deserializar respuestas de la API
// y como ViewModel tipado en vistas que muestran datos de usuarios.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WEB_UI.Models.Enums;

namespace WEB_UI.Models.Entities;

public class Sujeto
{
    // Identificador único del sujeto en la base de datos (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // Número de cédula nacional (formato: 1-2345-6789). Máximo 20 caracteres.
    // Único en la BD — no pueden existir dos sujetos con la misma cédula.
    [Required, MaxLength(20)]
    public string Cedula { get; set; } = string.Empty;

    // Nombre completo del sujeto (nombre + apellidos). Máximo 200 caracteres.
    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    // Correo electrónico usado para login y notificaciones. Máximo 200 caracteres.
    // Único en la BD — no pueden existir dos sujetos con el mismo correo.
    [Required, MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    // Hash BCrypt de la contraseña (factor 12). NUNCA se almacena la contraseña
    // en texto plano. La API es quien hashea y verifica.
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // Rol del sujeto en el sistema: Dueño=1, Ingeniero=2, Admin=3.
    // Determina a qué secciones puede acceder y qué acciones puede realizar.
    public RolEnum Rol { get; set; }

    // Estado actual de la cuenta. Por defecto "Activo" al crearse.
    // Un admin puede inactivar o bloquear la cuenta de un usuario.
    public EstadoSujetoEnum Estado { get; set; } = EstadoSujetoEnum.Activo;

    // Token de concurrencia optimista. EF Core lo usa automáticamente para
    // detectar actualizaciones simultáneas y lanzar DbUpdateConcurrencyException.
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // Fecha UTC en que se creó el registro del sujeto.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Hash del token de restablecimiento de contraseña (enviado por correo).
    // Es null cuando no hay solicitud activa de cambio de contraseña.
    public string? PasswordResetHash { get; set; }

    // Fecha de expiración del token de restablecimiento.
    // Si es null o es pasada, el token ya no es válido.
    public DateTime? PasswordResetExpira { get; set; }

    // ----------------------------------------------------------
    // Relaciones de navegación (se populan con Include() en la API)
    // ----------------------------------------------------------

    // Fincas registradas por este sujeto (solo aplica si Rol = Dueño).
    public ICollection<Activo> Fincas { get; set; } = [];

    // Cuentas bancarias (IBAN) asociadas a este sujeto (solo Dueños).
    public ICollection<CuentaBancaria> CuentasBancarias { get; set; } = [];

    // Sesiones OTP generadas para este sujeto durante el registro
    // o la recuperación de contraseña.
    public ICollection<OtpSesion> OtpSesiones { get; set; } = [];
}
