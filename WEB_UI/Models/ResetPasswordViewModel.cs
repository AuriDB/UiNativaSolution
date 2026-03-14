using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class ResetPasswordViewModel
    {
        [Required] public string? Token { get; set; }

        [Required]
        [MinLength(6)]
        public string? NuevaContrasena { get; set; }

        [Required]
        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmarContrasena { get; set; }
    }
}
