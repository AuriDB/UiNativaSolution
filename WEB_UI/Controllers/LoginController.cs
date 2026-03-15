using Microsoft.AspNetCore.Mvc;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Index(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // TODO: llamar al API de autenticación
            // Simulación temporal:
            HttpContext.Session.SetString("UserName", model.Correo ?? "Usuario");
            HttpContext.Session.SetString("UserRole", "Dueno");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        public IActionResult ForgotPassword() => View();

        public IActionResult ResetPassword()   => View();
    }
}
