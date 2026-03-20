// ============================================================
// HomeController.cs — Enrutador central post-login
// No tiene una vista propia. Su único propósito es leer el rol
// del usuario desde la sesión y redirigirlo al dashboard correcto.
// También sirve como handler de errores globales.
// Ruta base: /Home
// ============================================================

using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class HomeController : Controller
    {
        // GET /Home  (también es la ruta de retorno después del login)
        // Verifica si hay sesión activa y redirige al dashboard según el rol.
        // Si no hay sesión, manda al login.
        public IActionResult Index()
        {
            // Si "UserName" no está en sesión, el usuario no está autenticado.
            // Se redirige al login para que inicie sesión.
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToAction("Index", "Login");

            // Lee el rol almacenado en sesión durante el login.
            var role = HttpContext.Session.GetString("UserRole");

            // Redirige al dashboard correspondiente según el rol del usuario.
            // Si el rol no coincide con ninguno conocido, vuelve al login.
            return role switch
            {
                "Admin"     => RedirectToAction("Dashboard", "Admin"),
                "Ingeniero" => RedirectToAction("Dashboard", "Engineer"),
                "Dueno"     => RedirectToAction("Dashboard", "Owner"),
                _           => RedirectToAction("Index", "Login")
            };
        }

        // GET /Home/Error
        // Vista de error global. Se invoca cuando ocurre una excepción no manejada.
        // ResponseCache evita que el navegador guarde en caché la página de error,
        // para que siempre muestre el error actual y no uno anterior.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // RequestId permite al equipo de desarrollo rastrear el error específico
            // en los logs del servidor usando el ID de la actividad actual o el
            // identificador de la traza del request HTTP.
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
