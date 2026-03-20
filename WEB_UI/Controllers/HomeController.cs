using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WEB_UI.Models;

namespace WEB_UI.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View();

    [HttpGet("Home/Perfil")]
    public IActionResult Perfil()
    {
<<<<<<< HEAD
        ViewBag.Nombre = User.FindFirstValue(ClaimTypes.Name)  ?? "—";
        ViewBag.Correo = User.FindFirstValue(ClaimTypes.Email) ?? "—";
        ViewBag.Rol    = User.FindFirstValue(ClaimTypes.Role)  ?? "—";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
=======
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
>>>>>>> 8938498ba942204ca8456128102b364380d3999e
