using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class OwnerController : Controller
    {
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");
            //if (HttpContext.Session.GetString("UserRole") != "Dueno")
            //    return RedirectToAction("Index", "Home");
            return null;
        }

        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult MyProperties()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult RegisterProperty()
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

        public IActionResult EditProperty(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;
            ViewBag.FincaId = id;
            return View();
        }

        public IActionResult PaymentHistory()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult BankAccount()
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
