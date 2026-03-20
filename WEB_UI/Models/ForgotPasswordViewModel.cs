// ============================================================
// ForgotPasswordViewModel.cs — Modelo del formulario "Olvidé mi contraseña"
// Se usa en Login/ForgotPassword.cshtml para capturar el correo
// al que se enviará el enlace de restablecimiento de contraseña.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class ForgotPasswordViewModel
    {
        // Correo registrado en el sistema. Campo obligatorio.
        // La API busca el Sujeto por este correo; si existe, envía el token
        // de reset. Si no existe, la API responde con éxito de todas formas
        // para no revelar qué correos están registrados (seguridad).
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string? Correo { get; set; }
    }
}
