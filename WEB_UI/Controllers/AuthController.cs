using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

/// <summary>
/// Maneja autenticación: vistas (GET) y endpoints AJAX (POST).
/// Las acciones POST devuelven JSON { success, message } consumido por el JS.
/// </summary>
public class AuthController : Controller
{
    private readonly AuthService _auth;
    private readonly OtpService  _otp;

    public AuthController(AuthService auth, OtpService otp)
    {
        _auth = auth;
        _otp  = otp;
    }

    // ── Vistas (GET) ───────────────────────────────────────────────────────────
    public IActionResult Login()                => View();
    public IActionResult Registro()             => View();
    public IActionResult VerificarOtp()         => View();
    public IActionResult OlvidoContrasena()     => View();
    public IActionResult RestablecerContrasena() => View();

    // ── Endpoints AJAX (POST) ──────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Correo) || string.IsNullOrWhiteSpace(req.Contrasena))
            return Json(new { success = false, message = "Completa todos los campos." });

        var (ok, error) = await _auth.LoginAsync(req.Correo, req.Contrasena, HttpContext);
        return Json(new { success = ok, message = error });
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (ok, error) = await _auth.RegisterAsync(
            req.Nombre, req.Apellido1, req.Apellido2,
            req.Cedula, req.Correo,
            req.Contrasena, req.ConfirmarContrasena);

        return Json(new { success = ok, message = error });
    }

    [HttpPost]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        var (ok, error) = await _otp.VerificarAsync(req.Correo, req.Otp);
        return Json(new { success = ok, message = error });
    }

    [HttpPost]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest req)
    {
        var (ok, error) = await _otp.ReenviarAsync(req.Correo);
        return Json(new { success = ok, message = error });
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        // OWASP: siempre retorna éxito, nunca revelar si el correo existe
        if (!string.IsNullOrWhiteSpace(req.Correo))
            await _auth.ForgotPasswordAsync(req.Correo, HttpContext);

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var (ok, error) = await _auth.ResetPasswordAsync(req.Token, req.NuevaContrasena);
        return Json(new { success = ok, message = error });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("nativa_auth");
        return RedirectToAction("Login");
    }
}

// ── DTOs (request bodies para AJAX) ───────────────────────────────────────────
public record LoginRequest(string Correo, string Contrasena);

public record RegisterRequest(
    string Nombre, string Apellido1, string Apellido2,
    string Cedula, string Correo,
    string Contrasena, string ConfirmarContrasena);

public record VerifyOtpRequest(string Correo, string Otp);

public record ResendOtpRequest(string Correo);

public record ForgotPasswordRequest(string Correo);

public record ResetPasswordRequest(string Token, string NuevaContrasena);
