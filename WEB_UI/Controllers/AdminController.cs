// ============================================================
// AdminController.cs — Módulo del Administrador del sistema
// Agrupa las pantallas de gestión de usuarios, fincas, parámetros
// de pago, auditoría y reportes. Solo accesible con rol "Admin".
// PENDIENTE: descomentar CheckSession() cuando se integre la API.
// Ruta base: /Admin
// ============================================================

using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class AdminController : Controller
    {
        // ----------------------------------------------------------
        // Verificación de sesión y rol
        // ----------------------------------------------------------

        // Método privado auxiliar que valida sesión activa con rol "Admin".
        // Retorna null si todo está bien, o una redirección si no cumple.
        // NOTA: comentado temporalmente para desarrollo visual sin API.
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");      // Sin sesión → login
            //if (HttpContext.Session.GetString("UserRole") != "Admin")
            //    return RedirectToAction("Index", "Home");       // Rol incorrecto → home
            return null; // Sesión válida → continuar
        }

        // ----------------------------------------------------------
        // Vistas del módulo Admin
        // ----------------------------------------------------------

        // GET /Admin/Dashboard
        // Panel principal del administrador: KPIs globales del sistema
        // (usuarios activos, fincas en cola, pagos del mes, etc.).
        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/Users
        // Lista de todos los usuarios del sistema (dueños, ingenieros, admins).
        // Permite crear ingenieros, activar/inactivar cuentas y filtrar por rol.
        public IActionResult Users()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/UserDetail/{id}
        // Detalle de un usuario específico: datos personales, estado, historial.
        // El id del usuario se pasa al JS via ViewBag para las llamadas AJAX.
        public IActionResult UserDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.UserId = id; // Disponible en la vista como @ViewBag.UserId
            return View();
        }

        // GET /Admin/Properties
        // Vista global de todas las fincas registradas en el sistema,
        // con filtros por estado (Pendiente, Aprobada, Rechazada, etc.).
        public IActionResult Properties()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/PropertyDetail/{id}
        // Detalle de una finca específica desde la perspectiva del administrador:
        // datos completos, adjuntos, ingeniero asignado y plan de pago activo.
        public IActionResult PropertyDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id;
            return View();
        }

        // GET /Admin/PaymentSettings
        // Pantalla para configurar los parámetros globales de cálculo del pago PSA:
        // precio base, porcentajes de vegetación, hidrología, topografía y tope máximo.
        // Solo el administrador puede crear nuevos parámetros vigentes.
        public IActionResult PaymentSettings()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/AuditLog
        // Registro de auditoría del sistema: acciones importantes realizadas
        // por los usuarios (logins, cambios de estado, creación de parámetros, etc.).
        public IActionResult AuditLog()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/Reports
        // Generación y descarga de reportes del sistema en formato PDF
        // (pagos del período, fincas aprobadas, usuarios activos, etc.).
        public IActionResult Reports()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Admin/Profile
        // Datos del perfil del administrador y opción de cambio de contraseña.
        public IActionResult Profile()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }
    }
}
