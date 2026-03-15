using System.ComponentModel.DataAnnotations;

namespace WEB_UI.Models
{
    public class OtpViewModel
    {
        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El OTP debe tener 6 dígitos.")]
        public string? Otp { get; set; }
    }
}
