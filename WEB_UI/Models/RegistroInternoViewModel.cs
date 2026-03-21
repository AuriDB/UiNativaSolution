using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class RegistroInternoViewModel
    {
        [Required]
        public string Nombre    { get; set; }

        [Required]
        public string Apellidos { get; set; }

        [Required, EmailAddress]
        public string Email     { get; set; }

        [Required]
        public string Cedula    { get; set; }

        [Required, MinLength(6)]
        public string Password  { get; set; }

        [Required, Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; }
    }
}
