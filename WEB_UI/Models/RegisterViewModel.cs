// ============================================================
// RegisterViewModel.cs — Modelo del formulario de registro
// Se usa en Register/Index.cshtml como @model RegisterViewModel.
// Captura todos los datos necesarios para crear una cuenta de Dueño.
// Las validaciones son del lado del servidor (ModelState).
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class RegisterViewModel
    {
        // Primer nombre del usuario. Campo obligatorio.
        [Required] public string? Nombre    { get; set; }

        // Primer apellido del usuario. Campo obligatorio.
        [Required] public string? Apellido1 { get; set; }

        // Segundo apellido del usuario. Campo obligatorio.
        [Required] public string? Apellido2 { get; set; }

        // Número de cédula en formato costarricense: X-XXXX-XXXX
        // Ejemplo válido: 1-2345-6789. La regex valida este formato exacto.
        [Required]
        [RegularExpression(@"^\d{1}-\d{4}-\d{4}$", ErrorMessage = "Formato de cédula inválido.")]
        public string? Cedula { get; set; }

        // Correo electrónico que se usará para iniciar sesión y recibir notificaciones.
        // La API verifica que no exista otro usuario con el mismo correo.
        [Required]
        [EmailAddress]
        public string? Correo { get; set; }

        // Contraseña deseada. Mínimo 6 caracteres.
        // La API puede tener requisitos adicionales (mayúsculas, números, símbolos).
        [Required]
        [MinLength(6)]
        public string? Contrasena { get; set; }

        // Campo de confirmación. Debe coincidir exactamente con Contrasena.
        // Compare valida esto del lado del servidor. El JS también lo valida
        // del lado del cliente para mejor experiencia de usuario.
        [Required]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmarContrasena { get; set; }
    }
}
