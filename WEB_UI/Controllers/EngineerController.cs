using Microsoft.AspNetCore.Mvc;

namespace WEB_UI.Controllers
{
    public class EngineerController : Controller
    {
        private IActionResult? CheckSession()
        {
            //if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            //    return RedirectToAction("Index", "Login");
            //if (HttpContext.Session.GetString("UserRole") != "Ingeniero")
            //    return RedirectToAction("Index", "Home");
            return null;
        }

        public IActionResult Dashboard()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult FifoQueue()
        {
            var check = CheckSession();
            if (check != null) return check;
            return View();
        }

        public IActionResult Evaluation(int? id)
        {
            var check = CheckSession();
            if (check != null) return check;

            if (id == null)
                return RedirectToAction("FifoQueue");

            ViewBag.FincaId = id;
            return View();
        }

        public IActionResult History()
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