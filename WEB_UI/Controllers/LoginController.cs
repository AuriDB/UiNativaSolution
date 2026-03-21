using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WEB_UI.Models;

namespace WEB_UI.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string API_URL = "https://nativasolutionback-fkg5f4gef9b8bgaa.canadacentral-01.azurewebsites.net/api";

        public LoginController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index() => View();

        // POST AJAX: recibe { Email, Password }, llama al API, setea sesion y retorna JSON 
        [HttpPost]
        public async Task<IActionResult> Authenticate([FromBody] LoginAjaxRequest request)
        {
            try
            {
                var client  = _httpClientFactory.CreateClient();
                var payload = JsonSerializer.Serialize(new { Email = request.Email, Password = request.Password });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var httpResponse = await client.PostAsync($"{API_URL}/Auth/Login", content);
                var json         = await httpResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root      = doc.RootElement;
                var result    = root.GetProperty("result").GetString();

                if (result == "ok")
                {
                    var data         = root.GetProperty("data");
                    var nombreUsuario = data.GetProperty("nombreUsuario").GetString() ?? request.Email;
                    var rol           = data.GetProperty("rol").GetString() ?? "Dueno";
                    var idRol         = data.GetProperty("idRol").GetInt32();

                    HttpContext.Session.SetString("UserName", nombreUsuario);
                    HttpContext.Session.SetString("UserRole", rol);
                    HttpContext.Session.SetInt32 ("IdRol",    idRol);

                    return Json(new { result = "ok" });
                }

                var message = root.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString()
                    : "Credenciales invalidas.";

                return Json(new { result = "error", message });
            }
            catch
            {
                return Json(new { result = "error", message = "Error de conexion con el servidor." });
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        public IActionResult ForgotPassword() => View();
        public IActionResult ResetPassword()   => View();
    }

    // Para leer lo que viene del AJAX POST en Authenticate
    public record LoginAjaxRequest(string Email, string Password);
}
