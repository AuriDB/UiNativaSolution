using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class RegisterViewModel
    {
        [Required] public string? Nombre    { get; set; }
        [Required] public string? Apellido1 { get; set; }
        [Required] public string? Apellido2 { get; set; }

        [Required]
        [RegularExpression(@"^\d{1}-\d{4}-\d{4}$", ErrorMessage = "Formato de cédula inválido.")]
        public string? Cedula { get; set; }

        [Required]
        [EmailAddress]
        public string? Correo { get; set; }

        [Required]
        [MinLength(6)]
        public string? Contrasena { get; set; }

        [Required]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmarContrasena { get; set; }
    }
}
