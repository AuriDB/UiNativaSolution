using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string? Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string? Contrasena { get; set; }
    }
}
