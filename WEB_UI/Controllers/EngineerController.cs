using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class EngineerController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}