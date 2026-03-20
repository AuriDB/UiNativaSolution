// ============================================================
// OwnerController.cs — Módulo del Dueño de finca (PSA)
// Agrupa todas las pantallas y acciones disponibles para el
// rol "Dueño". Cada acción verifica que haya sesión activa
// con el rol correcto antes de renderizar la vista.
// PENDIENTE: descomentar CheckSession() cuando se integre la API.
// Ruta base: /Owner
// ============================================================

using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class OwnerController : Controller
    {
        // ----------------------------------------------------------
        // Verificación de sesión y rol
        // ----------------------------------------------------------

        // Método privado auxiliar que valida si el usuario está autenticado
        // y tiene el rol "Dueno". Si no cumple, retorna una redirección;
        // si sí cumple, retorna null y el controller continúa normalmente.
        // NOTA: actualmente comentado para facilitar el desarrollo visual
        // sin depender de la API. Descomentar al integrar la autenticación real.
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");       // Sin sesión → login
            //if (HttpContext.Session.GetString("UserRole") != "Dueno")
            //    return RedirectToAction("Index", "Home");        // Rol incorrecto → home
            return null; // Sesión válida → continuar
        }

        // ----------------------------------------------------------
        // Vistas del módulo Owner
        // ----------------------------------------------------------

        // GET /Owner/Dashboard
        // Página principal del dueño: resumen de fincas, pagos y estado general.
        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Owner/MyProperties
        // Lista todas las fincas registradas por este dueño.
        // Los datos se cargan desde el JS via AJAX al endpoint de la API.
        public IActionResult MyProperties()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Owner/RegisterProperty
        // Formulario para registrar una nueva finca con coordenadas (mapa Leaflet),
        // hectáreas y porcentajes de vegetación, hidrología y topografía.
        public IActionResult RegisterProperty()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Owner/PropertyDetail/{id}
        // Detalle completo de una finca específica: datos, adjuntos, plan de pago.
        // El id de la finca se pasa a la vista mediante ViewBag para que el JS
        // lo use al hacer las llamadas AJAX correspondientes.
        public IActionResult PropertyDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id; // Disponible en la vista como @ViewBag.FincaId
            return View();
        }

        // GET /Owner/EditProperty/{id}
        // Formulario para editar los datos de una finca existente.
        // Solo permite editar fincas que estén en estado "Pendiente" o "Devuelta".
        public IActionResult EditProperty(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id;
            return View();
        }

        // GET /Owner/PaymentHistory
        // Historial de pagos mensuales PSA del dueño.
        // Muestra el estado (Pendiente / Ejecutado) de cada cuota.
        public IActionResult PaymentHistory()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Owner/BankAccount
        // Pantalla para registrar o ver el IBAN bancario del dueño.
        // El IBAN se almacena cifrado en la API; aquí solo se muestra ofuscado.
        public IActionResult BankAccount()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        // GET /Owner/Profile
        // Perfil del dueño: datos personales y opción de cambio de contraseña.
        public IActionResult Profile()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }
    }
}
