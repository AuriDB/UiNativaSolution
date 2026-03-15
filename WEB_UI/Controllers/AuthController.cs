using Microsoft.AspNetCore.Mvc;
using WEB_UI.Models.Dtos;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    // ── GET /Auth/Login ──────────────────────────────────────────────────────
    public IActionResult Login() => View();

    // ── POST /Auth/Login ─────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Correo) ||
            string.IsNullOrWhiteSpace(dto.Contrasena))
            return Json(new { success = false, message = "Datos incompletos." });

        var (ok, mensaje) = await _auth.LoginAsync(dto);
        return Json(new { success = ok, message = mensaje });
    }

    // ── POST /Auth/Logout ────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _auth.LogoutAsync();
        return RedirectToAction("Login");
    }

    // GET logout para compatibilidad con link <a>
    public async Task<IActionResult> LogoutGet()
    {
        await _auth.LogoutAsync();
        return RedirectToAction("Login");
    }

    // ── GET /Auth/Registro ───────────────────────────────────────────────────
    public IActionResult Registro() => View();

    // ── POST /Auth/Register ──────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto is null) return Json(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _auth.RegistrarAsync(dto);
        return Json(new { success = ok, message = mensaje });
    }

    // ── GET /Auth/VerificarOtp ───────────────────────────────────────────────
    public IActionResult VerificarOtp() => View();

    // ── POST /Auth/VerifyOtp ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        if (dto is null) return Json(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _auth.VerificarOtpAsync(dto);
        return Json(new { success = ok, message = mensaje });
    }

    // ── POST /Auth/ResendOtp ─────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
    {
        if (dto is null) return Json(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _auth.ReenviarOtpAsync(dto);
        return Json(new { success = ok, message = mensaje });
    }

    // ── GET /Auth/OlvidoContrasena ───────────────────────────────────────────
    public IActionResult OlvidoContrasena() => View();

    // ── POST /Auth/ForgotPassword ────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        if (dto is not null)
            await _auth.SolicitarResetAsync(dto);
        // Siempre retornar success=true (OWASP: no revelar existencia del correo)
        return Json(new { success = true });
    }

    // ── GET /Auth/RestablecerContrasena ──────────────────────────────────────
    public IActionResult RestablecerContrasena() => View();

    // ── POST /Auth/ResetPassword ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (dto is null) return Json(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _auth.ResetPasswordAsync(dto);
        return Json(new { success = ok, message = mensaje });
    }

    // ── GET /Auth/AccesoDenegado ─────────────────────────────────────────────
    public IActionResult AccesoDenegado() => View("Login");
}
