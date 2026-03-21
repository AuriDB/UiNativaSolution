using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace WEB_UI.Controllers
{
    public class RegisterController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string API_URL = "https://nativasolutionback-fkg5f4gef9b8bgaa.canadacentral-01.azurewebsites.net/api";

        public RegisterController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Register
        public IActionResult Index() => View();

        // GET: /Register/VerifyOtp
        public IActionResult VerifyOtp() => View();

        // POST AJAX: /Register/Registrar
        // Crea el usuario (Activo=0) y dispara el envio del OTP por email - este es el cambio que se le hizo a la logica
        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] RegistrarRequest request)
        {
            try
            {
                var client  = _httpClientFactory.CreateClient();
                var payload = JsonSerializer.Serialize(new
                {
                    Nombre          = request.Nombre,
                    PrimerApellido  = request.PrimerApellido,
                    SegundoApellido = request.SegundoApellido,
                    Email           = request.Email,
                    Cedula          = request.Cedula,
                    Password        = request.Password
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var httpResponse = await client.PostAsync($"{API_URL}/Usuario/Registrar", content);
                var json         = await httpResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root      = doc.RootElement;
                var result    = root.GetProperty("result").GetString();

                if (result == "ok")
                    return Json(new { result = "ok" });

                var message = root.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString()
                    : "Error al registrar el usuario.";

                return Json(new { result = "error", message });
            }
            catch
            {
                return Json(new { result = "error", message = "Error de conexion con el servidor." });
            }
        }

        // POST AJAX: /Register/VerificarOtp
        [HttpPost]
        public async Task<IActionResult> VerificarOtp([FromBody] VerificarOtpRequest request)
        {
            try
            {
                var client  = _httpClientFactory.CreateClient();
                var payload = JsonSerializer.Serialize(new { Email = request.Email, Codigo = request.Codigo });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var httpResponse = await client.PostAsync($"{API_URL}/Usuario/VerificarOtp", content);
                var json         = await httpResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root      = doc.RootElement;
                var result    = root.GetProperty("result").GetString();

                if (result == "ok")
                    return Json(new { result = "ok" });

                var message = root.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString()
                    : "Codigo incorrecto.";

                return Json(new { result = "error", message });
            }
            catch
            {
                return Json(new { result = "error", message = "Error de conexion con el servidor." });
            }
        }

        // POST AJAX: /Register/ReenviarOtp
        [HttpPost]
        public async Task<IActionResult> ReenviarOtp([FromBody] ReenviarOtpRequest request)
        {
            try
            {
                var client  = _httpClientFactory.CreateClient();
                var payload = JsonSerializer.Serialize(new { Email = request.Email });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var httpResponse = await client.PostAsync($"{API_URL}/Usuario/ReenviarOtp", content);
                var json         = await httpResponse.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root      = doc.RootElement;
                var result    = root.GetProperty("result").GetString();

                if (result == "ok")
                    return Json(new { result = "ok" });

                var message = root.TryGetProperty("message", out var msgProp)
                    ? msgProp.GetString()
                    : "No se pudo reenviar el codigo.";

                return Json(new { result = "error", message });
            }
            catch
            {
                return Json(new { result = "error", message = "Error de conexion con el servidor." });
            }
        }
    }

    public record RegistrarRequest(
        string Nombre,
        string PrimerApellido,
        string SegundoApellido,
        string Email,
        string Cedula,
        string Password);

    public record VerificarOtpRequest(string Email, string Codigo);
    public record ReenviarOtpRequest(string Email);
}
