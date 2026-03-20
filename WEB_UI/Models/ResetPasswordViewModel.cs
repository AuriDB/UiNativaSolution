// ============================================================
// ResetPasswordViewModel.cs — Modelo del formulario "Restablecer contraseña"
// Se usa en Login/ResetPassword.cshtml. El usuario llega aquí desde
// el enlace del correo que contiene el token de restablecimiento.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class ResetPasswordViewModel
    {
        // Token de restablecimiento generado por la API y enviado al correo.
        // Generalmente llega como query string en la URL del enlace y
        // se carga en un campo hidden del formulario.
        // Tiene una fecha de expiración limitada (ej: 30 minutos).
        [Required] public string? Token { get; set; }

        // Nueva contraseña deseada por el usuario. Mínimo 6 caracteres.
        [Required]
        [MinLength(6)]
        public string? NuevaContrasena { get; set; }

        // Campo de confirmación que debe coincidir con NuevaContrasena.
        // Valida que el usuario no haya escrito mal su nueva contraseña.
        [Required]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmarContrasena { get; set; }
    }
}
