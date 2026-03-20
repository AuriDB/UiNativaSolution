using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
                return RedirectToAction("Index", "Login");

            var role = HttpContext.Session.GetString("UserRole");

            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Ingeniero" => RedirectToAction("Dashboard", "Engineer"),
                "Dueno" => RedirectToAction("Dashboard", "Owner"),
                _ => RedirectToAction("Index", "Login")
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}