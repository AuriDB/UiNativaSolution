using Microsoft.AspNetCore.Mvc;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class RegisterController : Controller
    {
        public IActionResult Index()     => View();
        public IActionResult VerifyOtp() => View();
    }
}
