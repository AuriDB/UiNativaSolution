// ============================================================
// LoginController.cs — Autenticación de usuarios
// Maneja el inicio de sesión, cierre de sesión y recuperación
// de contraseña. Cuando se conecte la API externa, los métodos
// POST deberán reemplazarse con llamadas HttpClient.
// Ruta base: /Login
// ============================================================

using Microsoft.AspNetCore.Mvc;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class LoginController : Controller
    {
        // GET /Login
        // Muestra el formulario de inicio de sesión.
        public IActionResult Index() => View();

        // POST /Login
        // Procesa las credenciales enviadas desde el formulario.
        // TEMPORAL: guarda el usuario en Session para simular autenticación
        // mientras la API externa no está disponible.
        // PENDIENTE: reemplazar el cuerpo con una llamada POST a la API
        // y manejar la respuesta (cookie de sesión o JWT).
        [HttpPost]
        public IActionResult Index(LoginViewModel model)
        {
            // Si el formulario tiene errores de validación (correo inválido,
            // contraseña vacía, etc.), devuelve la vista con los mensajes de error.
            if (!ModelState.IsValid) return View(model);

            // TODO: llamar al API de autenticación
            // Simulación temporal: se almacena el correo y el rol en la sesión
            // para que HomeController pueda redirigir al dashboard correcto.
            HttpContext.Session.SetString("UserName", model.Correo ?? "Usuario");
            HttpContext.Session.SetString("UserRole", "Dueno");

            // Redirige al HomeController que decide a qué dashboard enviar al usuario
            // según el rol almacenado en la sesión.
            return RedirectToAction("Index", "Home");
        }

        // GET /Login/Logout
        // Cierra la sesión del usuario eliminando todos los datos de la sesión
        // y redirige a la pantalla de login.
        public IActionResult Logout()
        {
            // Elimina todos los datos almacenados en la cookie de sesión.
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        // GET /Login/ForgotPassword
        // Muestra el formulario donde el usuario ingresa su correo
        // para solicitar un restablecimiento de contraseña.
        public IActionResult ForgotPassword() => View();

        // GET /Login/ResetPassword
        // Muestra el formulario donde el usuario ingresa su nueva contraseña
        // usando el token recibido por correo.
        public IActionResult ResetPassword()   => View();
    }
}
