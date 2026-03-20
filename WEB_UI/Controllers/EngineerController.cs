// ============================================================
// EngineerController.cs — Módulo del Ingeniero ambiental
// Agrupa todas las pantallas disponibles para el rol "Ingeniero".
// El ingeniero evalúa las fincas en orden FIFO (primera en entrar,
// primera en salir) y emite dictámenes de aprobación o rechazo.
// PENDIENTE: descomentar CheckSession() cuando se integre la API.
// Ruta base: /Engineer
// ============================================================

using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class EngineerController : Controller
    {
        // ----------------------------------------------------------
        // Verificación de sesión y rol
        // ----------------------------------------------------------

        // Método privado auxiliar que valida sesión activa con rol "Ingeniero".
        // Retorna null si todo está bien, o una redirección si no cumple.
        // NOTA: comentado temporalmente para desarrollo visual sin API.
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");         // Sin sesión → login
            //if (HttpContext.Session.GetString("UserRole") != "Ingeniero")
            //    return RedirectToAction("Index", "Home");          // Rol incorrecto → home
            return null; // Sesión válida → continuar
        }

        // ----------------------------------------------------------
        // Vistas del módulo Engineer
        // ----------------------------------------------------------

        // GET /Engineer/Dashboard
        // Panel principal del ingeniero: resumen de fincas pendientes
        // de evaluación y estadísticas de su trabajo.
        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Engineer/FifoQueue
        // Cola de fincas pendientes ordenadas por fecha de registro (FIFO).
        // El ingeniero toma la primera disponible para evaluarla.
        // Solo puede tener una finca "En Revisión" a la vez.
        public IActionResult FifoQueue()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Engineer/Evaluation/{id}
        // Formulario de evaluación de una finca específica.
        // El ingeniero revisa los datos, adjuntos y emite su dictamen:
        // Aprobar, Devolver (con observaciones) o Rechazar.
        // El id de la finca se pasa al JS via ViewBag para las llamadas AJAX.
        public IActionResult Evaluation(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id; // Disponible en la vista como @ViewBag.FincaId
            return View();
        }

        // GET /Engineer/History
        // Historial de todas las fincas evaluadas por este ingeniero:
        // aprobadas, devueltas y rechazadas, con fecha y observaciones.
        public IActionResult History()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Engineer/Profile
        // Datos del perfil del ingeniero y opción de cambio de contraseña.
        public IActionResult Profile()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }
    }
}
