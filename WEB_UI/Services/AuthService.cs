using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WEB_UI.Data;
using WEB_UI.Models.Dtos;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Services;

public class AuthService
{
    private readonly NativaDbContext  _db;
    private readonly OtpService       _otpSvc;
    private readonly EmailService     _email;
    private readonly IConfiguration   _cfg;
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        NativaDbContext db,
        OtpService otpSvc,
        EmailService email,
        IConfiguration cfg,
        IHttpContextAccessor ctx,
        ILogger<AuthService> logger)
    {
        _db     = db;
        _otpSvc = otpSvc;
        _email  = email;
        _cfg    = cfg;
        _ctx    = ctx;
        _logger = logger;
    }

    // ── CU01 Registro ────────────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> RegistrarAsync(RegisterDto dto)
    {
        // Validación contraseña backend
        if (!ValidarContrasena(dto.Contrasena))
            return (false, "La contraseña no cumple los requisitos de seguridad.");

        if (dto.Contrasena != dto.ConfirmarContrasena)
            return (false, "Las contraseñas no coinciden.");

        // Cédula: aceptar con o sin guiones, normalizar a formato 1-XXXX-XXXX
        var cedulaNorm = NormalizarCedula(dto.Cedula);
        if (cedulaNorm is null)
            return (false, "Formato de cédula inválido. Use 1-2345-6789.");

        // Unicidad
        if (await _db.Sujetos.AnyAsync(s => s.Cedula == cedulaNorm))
            return (false, "La cédula ya está registrada.");

        if (await _db.Sujetos.AnyAsync(s => s.Correo == dto.Correo.ToLower()))
            return (false, "El correo ya está registrado.");

        var nombreCompleto = $"{dto.Nombre.Trim()} {dto.Apellido1.Trim()} {dto.Apellido2.Trim()}";

        var sujeto = new Sujeto
        {
            Cedula       = cedulaNorm,
            Nombre       = nombreCompleto[..Math.Min(200, nombreCompleto.Length)],
            Correo       = dto.Correo.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena, workFactor: 12),
            Rol          = RolEnum.Dueno,
            Estado       = EstadoSujetoEnum.Inactivo,  // activa al verificar OTP
            FechaCreacion = DateTime.UtcNow
        };

        _db.Sujetos.Add(sujeto);
        await _db.SaveChangesAsync();

        await GenerarYEnviarOtpAsync(sujeto);
        return (true, "Cuenta creada. Verifica tu correo.");
    }

    // ── CU01 VerificarOtp ────────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> VerificarOtpAsync(VerifyOtpDto dto)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == dto.Correo.ToLower());

        if (sujeto is null) return (false, "Correo no encontrado.");

        if (sujeto.Estado == EstadoSujetoEnum.Bloqueado)
            return (false, "Cuenta bloqueada. Contacta al administrador.");

        var sesion = await _db.OtpSesiones
            .Where(o => o.IdSujeto == sujeto.Id && !o.Usada)
            .OrderByDescending(o => o.FechaCreacion)
            .FirstOrDefaultAsync();

        if (sesion is null) return (false, "No hay código activo. Solicita uno nuevo.");

        if (DateTime.UtcNow > sesion.Expiracion)
            return (false, "El código ha expirado. Solicita uno nuevo.");

        if (!_otpSvc.VerificarOtp(dto.Otp, sesion.HashOtp))
        {
            sesion.Intentos++;
            if (sesion.Intentos >= 3)
            {
                sujeto.Estado = EstadoSujetoEnum.Bloqueado;
                await _db.SaveChangesAsync();
                _ = _email.EnviarGenericoAsync(sujeto.Correo, "Cuenta bloqueada — Sistema Nativa",
                    $"<p>Tu cuenta fue bloqueada por múltiples intentos fallidos. Contacta al administrador.</p>");
                return (false, "Cuenta bloqueada por demasiados intentos fallidos.");
            }
            await _db.SaveChangesAsync();
            return (false, $"Código incorrecto. Intentos restantes: {3 - sesion.Intentos}.");
        }

        sesion.Usada    = true;
        sujeto.Estado   = EstadoSujetoEnum.Activo;
        await _db.SaveChangesAsync();
        return (true, "Cuenta verificada correctamente.");
    }

    // ── CU01 ReenviarOtp ─────────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> ReenviarOtpAsync(ResendOtpDto dto)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == dto.Correo.ToLower());

        if (sujeto is null) return (false, "Correo no encontrado.");
        if (sujeto.Estado == EstadoSujetoEnum.Bloqueado)
            return (false, "Cuenta bloqueada.");

        // Cooldown 30s y límite 3 reenvíos en 5 min
        var ventana = DateTime.UtcNow.AddMinutes(-5);
        var reenvios = await _db.OtpSesiones
            .CountAsync(o => o.IdSujeto == sujeto.Id && o.FechaCreacion >= ventana);

        if (reenvios >= 3)
            return (false, "Límite de reenvíos alcanzado. Intenta en 5 minutos.");

        var ultima = await _db.OtpSesiones
            .Where(o => o.IdSujeto == sujeto.Id)
            .OrderByDescending(o => o.FechaCreacion)
            .FirstOrDefaultAsync();

        if (ultima?.UltimoReenvio is not null &&
            (DateTime.UtcNow - ultima.UltimoReenvio.Value).TotalSeconds < 30)
            return (false, "Espera 30 segundos antes de reenviar.");

        await GenerarYEnviarOtpAsync(sujeto);
        return (true, "Código reenviado.");
    }

    // ── CU02 Login ───────────────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> LoginAsync(LoginDto dto)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == dto.Correo.ToLower());

        if (sujeto is null || !BCrypt.Net.BCrypt.Verify(dto.Contrasena, sujeto.PasswordHash))
            return (false, "Credenciales incorrectas.");

        if (sujeto.Estado == EstadoSujetoEnum.Inactivo)
            return (false, "Cuenta pendiente de verificación. Revisa tu correo.");

        if (sujeto.Estado == EstadoSujetoEnum.Bloqueado)
            return (false, "Cuenta bloqueada. Contacta al administrador.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sujeto.Id.ToString()),
            new(ClaimTypes.Name,           sujeto.Nombre),
            new(ClaimTypes.Email,          sujeto.Correo),
            new(ClaimTypes.Role,           sujeto.Rol.ToString())
        };

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var props     = new AuthenticationProperties { IsPersistent = false };

        await _ctx.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

        return (true, "Inicio de sesión exitoso.");
    }

    // ── CU03 Logout ──────────────────────────────────────────────────────────
    public async Task LogoutAsync()
    {
        await _ctx.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    // ── CU04 ForgotPassword ──────────────────────────────────────────────────
    public async Task SolicitarResetAsync(ForgotPasswordDto dto)
    {
        // Respuesta genérica (OWASP: no revelar si correo existe)
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == dto.Correo.ToLower());

        if (sujeto is null || sujeto.Estado != EstadoSujetoEnum.Activo) return;

        var token    = GenerarTokenReset(sujeto.Id);
        var tokenHash = ComputeSha256(token);

        sujeto.PasswordResetHash   = tokenHash;
        sujeto.PasswordResetExpira = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        var request  = _ctx.HttpContext!.Request;
        var resetUrl = $"{request.Scheme}://{request.Host}/Auth/RestablecerContrasena?token={Uri.EscapeDataString(token)}";
        _ = _email.EnviarResetPasswordAsync(sujeto.Correo, sujeto.Nombre, resetUrl);
    }

    // ── CU04 ResetPassword ───────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (!ValidarContrasena(dto.NuevaContrasena))
            return (false, "La contraseña no cumple los requisitos de seguridad.");

        int sujetoId;
        try { sujetoId = ExtraerIdDeToken(dto.Token); }
        catch { return (false, "El enlace es inválido o ha expirado."); }

        var sujeto = await _db.Sujetos.FindAsync(sujetoId);
        if (sujeto is null) return (false, "El enlace es inválido o ha expirado.");

        var tokenHash = ComputeSha256(dto.Token);
        if (sujeto.PasswordResetHash != tokenHash)
            return (false, "El enlace es inválido o ya fue utilizado.");

        if (sujeto.PasswordResetExpira is null || DateTime.UtcNow > sujeto.PasswordResetExpira)
            return (false, "El enlace ha expirado. Solicita uno nuevo.");

        sujeto.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(dto.NuevaContrasena, 12);
        sujeto.PasswordResetHash   = null;
        sujeto.PasswordResetExpira = null;
        await _db.SaveChangesAsync();
        return (true, "Contraseña actualizada correctamente.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private async Task GenerarYEnviarOtpAsync(Sujeto sujeto)
    {
        var otp  = _otpSvc.GenerarOtp();
        var hash = _otpSvc.HashOtp(otp);

        var sesion = new OtpSesion
        {
            IdSujeto       = sujeto.Id,
            HashOtp        = hash,
            Expiracion     = DateTime.UtcNow.AddSeconds(90),
            Usada          = false,
            Intentos       = 0,
            ConteoReenvios = 0,
            UltimoReenvio  = DateTime.UtcNow,
            FechaCreacion  = DateTime.UtcNow
        };
        _db.OtpSesiones.Add(sesion);
        await _db.SaveChangesAsync();
        _ = _email.EnviarOtpAsync(sujeto.Correo, sujeto.Nombre, otp);
    }

    private static bool ValidarContrasena(string pwd) =>
        pwd.Length >= 6 &&
        pwd.Any(char.IsUpper) &&
        pwd.Any(char.IsDigit) &&
        pwd.Any(c => !char.IsLetterOrDigit(c));

    private static string? NormalizarCedula(string cedula)
    {
        // Acepta 1-2345-6789 o 123456789 (9 dígitos sin guiones)
        var limpia = cedula.Replace("-", "").Trim();
        if (limpia.Length == 9 && limpia.All(char.IsDigit))
            return $"{limpia[0]}-{limpia[1..5]}-{limpia[5..]}";
        // Formato ya formateado
        if (System.Text.RegularExpressions.Regex.IsMatch(cedula.Trim(), @"^\d{1}-\d{4}-\d{4}$"))
            return cedula.Trim();
        return null;
    }

    private string GenerarTokenReset(int sujetoId)
    {
        var expiry  = DateTime.UtcNow.AddMinutes(15).Ticks;
        var payload = $"{sujetoId}|{expiry}";
        var key     = Encoding.UTF8.GetBytes(
            _cfg["Auth:HmacSecret"] ?? "fallback_dev_secret_32_bytes!!!");
        using var hmac = new HMACSHA256(key);
        var sig   = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)) + "." + sig;
    }

    private int ExtraerIdDeToken(string token)
    {
        var parts   = token.Split('.');
        if (parts.Length != 2) throw new InvalidOperationException();
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
        var segs    = payload.Split('|');
        if (segs.Length != 2) throw new InvalidOperationException();
        var expiry  = new DateTime(long.Parse(segs[1]), DateTimeKind.Utc);
        if (DateTime.UtcNow > expiry) throw new InvalidOperationException("expired");

        // Re-verify HMAC
        var key  = Encoding.UTF8.GetBytes(
            _cfg["Auth:HmacSecret"] ?? "fallback_dev_secret_32_bytes!!!");
        using var hmac   = new HMACSHA256(key);
        var expectedSig  = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        if (parts[1] != expectedSig) throw new InvalidOperationException("tampered");

        return int.Parse(segs[0]);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
