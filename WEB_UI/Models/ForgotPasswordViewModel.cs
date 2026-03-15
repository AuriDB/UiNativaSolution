using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string? Correo { get; set; }
    }
}
