// ============================================================
// LoginViewModel.cs — Modelo del formulario de inicio de sesión
// Se usa en Login/Index.cshtml como @model LoginViewModel.
// Las anotaciones de validación se evalúan automáticamente por
// el ModelState de ASP.NET Core antes de procesar el POST.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class LoginViewModel
    {
        // Correo electrónico del usuario. Campo obligatorio.
        // Se valida que tenga formato de email (usuario@dominio.com).
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string? Correo { get; set; }

        // Contraseña del usuario. Campo obligatorio.
        // Se renderiza como <input type="password"> en la vista.
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string? Contrasena { get; set; }
    }
}
