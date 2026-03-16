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
        ViewBag.Nombre = User.FindFirstValue(ClaimTypes.Name)  ?? "—";
        ViewBag.Correo = User.FindFirstValue(ClaimTypes.Email) ?? "—";
        ViewBag.Rol    = User.FindFirstValue(ClaimTypes.Role)  ?? "—";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
