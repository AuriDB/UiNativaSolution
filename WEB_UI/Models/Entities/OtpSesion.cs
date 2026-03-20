// ============================================================
// OtpSesion.cs — Código de verificación OTP de un usuario
// Se usa en dos flujos:
//   1. Registro: el usuario verifica su correo con un OTP de 6 dígitos.
//   2. Recuperación de contraseña: se envía un OTP al correo registrado.
// El OTP se guarda como hash (nunca en texto plano) y expira
// después de N minutos. Tiene límite de intentos y reenvíos.
// ============================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEB_UI.Models.Entities;

public class OtpSesion
{
    // Identificador único del registro OTP (PK autoincremental).
    [Key]
    public int Id { get; set; }

    // FK al Sujeto al que pertenece este OTP.
    public int IdSujeto { get; set; }

    // Hash BCrypt del código OTP de 6 dígitos enviado por correo.
    // Se hashea para que aunque la BD sea comprometida, el OTP no sea visible.
    [Required]
    public string HashOtp { get; set; } = string.Empty;

    // Fecha y hora UTC en que el OTP deja de ser válido.
    // La API rechaza el OTP si DateTime.UtcNow > Expiracion.
    public DateTime Expiracion { get; set; }

    // Indica si este OTP ya fue usado exitosamente.
    // Un OTP usado no puede reutilizarse aunque aún no haya expirado.
    public bool Usada { get; set; }

    // Cantidad de intentos fallidos con este OTP.
    // La API bloquea el OTP después de N intentos incorrectos (ej: 3).
    public int Intentos { get; set; }

    // Fecha y hora del último reenvío del OTP al correo.
    // Es null si nunca se ha reenviado. Se usa para aplicar el cooldown
    // entre reenvíos (ej: mínimo 1 minuto entre solicitudes).
    public DateTime? UltimoReenvio { get; set; }

    // Cantidad total de veces que se ha reenviado el OTP.
    // La API limita el número máximo de reenvíos por sesión (ej: 3 veces).
    public int ConteoReenvios { get; set; }

    // Fecha UTC en que se creó este registro OTP.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ----------------------------------------------------------
    // Relaciones de navegación
    // ----------------------------------------------------------

    // Sujeto al que pertenece este OTP (cargado via FK IdSujeto).
    [ForeignKey(nameof(IdSujeto))]
    public Sujeto Sujeto { get; set; } = null!;
}
