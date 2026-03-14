using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class LandingController : Controller
    {
        // "/" y "/Landing" → Landing principal del proyecto
        public IActionResult Index()   => View("Project");

        public IActionResult Project() => View();
        public IActionResult Team()    => View();
    }
}
