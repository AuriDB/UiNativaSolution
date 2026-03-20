// ============================================================
// LandingController.cs — Páginas públicas del proyecto
// No requiere sesión activa. Accesible para cualquier visitante.
// Ruta base: /Landing  (también responde a "/")
// ============================================================

using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class LandingController : Controller
    {
        // GET /  o  GET /Landing
        // Redirige a la vista Project (página principal de presentación
        // del proyecto Nativa). Se usa "Project" como nombre de vista
        // para que la URL raíz "/" muestre la misma página que /Landing/Project.
        public IActionResult Index()   => View("Project");

        // GET /Landing/Project
        // Página de presentación general del sistema Nativa PSA.
        public IActionResult Project() => View();

        // GET /Landing/Team
        // Página con información del equipo de desarrollo.
        public IActionResult Team()    => View();
    }
}
