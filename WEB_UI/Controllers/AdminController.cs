using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class AdminController : Controller
    {
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");
            //if (HttpContext.Session.GetString("UserRole") != "Administrador")
            //    return RedirectToAction("Index", "Home");
            return null;
        }

        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Users()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult UserDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.UserId = id;
            return View();
        }

        public IActionResult RegisterAdmin()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult RegisterEngineer()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Properties()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult PropertyDetail(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id;
            return View();
        }

        public IActionResult PaymentSettings()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult AuditLog()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Reports()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Profile()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }
    }
}
