// ============================================================
// OtpViewModel.cs — Modelo del formulario de verificación OTP
// Se usa en Register/VerifyOtp.cshtml para capturar el código
// de 6 dígitos que el usuario recibió en su correo.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class OtpViewModel
    {
        // Código OTP de exactamente 6 dígitos enviado al correo del usuario.
        // StringLength valida tanto el mínimo como el máximo para asegurar
        // que siempre sean exactamente 6 caracteres.
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El OTP debe tener 6 dígitos.")]
        public string? Otp { get; set; }
    }
}
