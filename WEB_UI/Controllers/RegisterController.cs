// ============================================================
// RegisterController.cs — Registro de nuevos usuarios (Dueños)
// Maneja el flujo de registro en dos pasos:
//   1. Formulario de datos del usuario → /Register
//   2. Verificación del código OTP enviado al correo → /Register/VerifyOtp
// PENDIENTE: conectar con los endpoints de la API externa.
// Ruta base: /Register
// ============================================================

using Microsoft.AspNetCore.Mvc;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class RegisterController : Controller
    {
        // GET /Register
        // Muestra el formulario de registro con los campos:
        // nombre, apellidos, cédula, correo y contraseña.
        public IActionResult Index()     => View();

        // GET /Register/VerifyOtp
        // Muestra el formulario para ingresar el código OTP de 6 dígitos
        // que fue enviado al correo del usuario durante el registro.
        // El OTP valida que el correo es real antes de activar la cuenta.
        public IActionResult VerifyOtp() => View();
    }
}
