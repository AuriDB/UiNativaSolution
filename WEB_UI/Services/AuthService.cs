using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;

namespace WEB_UI.Services;

/// <summary>
/// Gestiona autenticación: login, registro, recuperación y restablecimiento de contraseña.
/// </summary>
public class AuthService
{
    private readonly NativaDbContext _db;
    private readonly OtpService      _otp;
    private readonly EmailService    _email;
    private readonly IConfiguration  _config;

    public AuthService(
        NativaDbContext db,
        OtpService      otp,
        EmailService    email,
        IConfiguration  config)
    {
        _db     = db;
        _otp    = otp;
        _email  = email;
        _config = config;
    }

    // ── Login ──────────────────────────────────────────────────────────────────
    // Valida credenciales, emite la cookie de autenticación y retorna el resultado.
    public async Task<(bool ok, string? error)> LoginAsync(
        string correo, string contrasena, HttpContext ctx)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == correo);

        // Verificar credenciales (mensaje genérico para no revelar si el correo existe)
        if (sujeto == null || !BCrypt.Net.BCrypt.Verify(contrasena, sujeto.PasswordHash))
            return (false, "Credenciales incorrectas.");

        if (sujeto.Estado == EstadoSujetoEnum.Bloqueado)
            return (false, "Tu cuenta está bloqueada. Contacta al administrador.");

        if (sujeto.Estado == EstadoSujetoEnum.Inactivo)
            return (false, "Debes verificar tu cuenta con el código OTP enviado a tu correo.");

        // Crear identidad basada en Claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sujeto.Id.ToString()),
            new(ClaimTypes.Name,           sujeto.Nombre!),
            new(ClaimTypes.Email,          sujeto.Correo!),
            new(ClaimTypes.Role,           sujeto.Rol.ToString())
        };

        var identity  = new ClaimsIdentity(claims, "nativa_auth");
        var principal = new ClaimsPrincipal(identity);

        await ctx.SignInAsync("nativa_auth", principal,
            new AuthenticationProperties { IsPersistent = false });

        return (true, null);
    }

    // ── Register ───────────────────────────────────────────────────────────────
    // Crea un nuevo Sujeto con rol Dueño, estado Inactivo (pendiente OTP).
    public async Task<(bool ok, string? error)> RegisterAsync(
        string nombre, string apellido1, string apellido2,
        string cedula, string correo,
        string contrasena, string confirmarContrasena)
    {
        // Validación de contraseña
        if (contrasena != confirmarContrasena)
            return (false, "Las contraseñas no coinciden.");

        if (!ValidarContrasena(contrasena))
            return (false, "La contraseña no cumple los requisitos de seguridad (mín. 6 chars, 1 mayúscula, 1 número, 1 especial).");

        // Verificar duplicados (cédula y correo son únicos)
        if (await _db.Sujetos.AnyAsync(s => s.Cedula == cedula))
            return (false, "La cédula ya está registrada en el sistema.");

        if (await _db.Sujetos.AnyAsync(s => s.Correo == correo))
            return (false, "El correo electrónico ya está registrado.");

        var sujeto = new Sujeto
        {
            Cedula       = cedula,
            Nombre       = $"{nombre} {apellido1} {apellido2}",
            Correo       = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12),
            Rol          = RolEnum.Dueno,
            Estado       = EstadoSujetoEnum.Inactivo   // activa después de verificar OTP
        };

        _db.Sujetos.Add(sujeto);
        await _db.SaveChangesAsync();

        // Enviar OTP de verificación
        await _otp.GenerarYEnviarAsync(sujeto);

        return (true, null);
    }

    // ── ForgotPassword ─────────────────────────────────────────────────────────
    // OWASP: siempre silencioso. Solo envía email si el correo existe y está activo.
    public async Task ForgotPasswordAsync(string correo, HttpContext ctx)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == correo);

        // No revelar si el correo existe: retornar sin acción si no es válido
        if (sujeto == null || sujeto.Estado != EstadoSujetoEnum.Activo)
            return;

        var token    = GenerarHmacToken(sujeto.Id);
        var resetUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}" +
                       $"/Auth/RestablecerContrasena?token={Uri.EscapeDataString(token)}";

        await _email.EnviarResetPasswordAsync(sujeto.Correo!, sujeto.Nombre!, resetUrl);
    }

    // ── ResetPassword ──────────────────────────────────────────────────────────
    // Verifica el token HMAC y actualiza la contraseña.
    public async Task<(bool ok, string? error)> ResetPasswordAsync(
        string token, string nuevaContrasena)
    {
        if (!ValidarContrasena(nuevaContrasena))
            return (false, "La contraseña no cumple los requisitos de seguridad.");

        var (idSujeto, valido) = VerificarHmacToken(token);
        if (!valido)
            return (false, "El enlace es inválido o ha expirado.");

        var sujeto = await _db.Sujetos.FindAsync(idSujeto);
        if (sujeto == null || sujeto.Estado != EstadoSujetoEnum.Activo)
            return (false, "El enlace es inválido o ha expirado.");

        sujeto.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena, workFactor: 12);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // ── Helpers privados ───────────────────────────────────────────────────────

    // Valida política mínima de contraseña: 6+ chars, 1 mayúscula, 1 número, 1 especial
    private static bool ValidarContrasena(string pwd)
    {
        if (string.IsNullOrEmpty(pwd) || pwd.Length < 6) return false;
        if (!pwd.Any(char.IsUpper))                       return false;
        if (!pwd.Any(char.IsDigit))                       return false;
        if (pwd.All(char.IsLetterOrDigit))                return false; // sin especial
        return true;
    }

    // Genera token HMAC-SHA256 en Base64Url: "{id}|{ticks}|{hmac}"
    private string GenerarHmacToken(int idSujeto)
    {
        var secret  = _config["Auth:HmacSecret"]!;
        var payload = $"{idSujeto}|{DateTime.UtcNow.Ticks}";
        var hmac    = ComputeHmac(payload, secret);
        // Base64Url (sin padding, reemplaza +/ por -_)
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{hmac}"))
                      .TrimEnd('=')
                      .Replace('+', '-')
                      .Replace('/', '_');
    }

    // Verifica firma HMAC y TTL de 15 minutos del token de reset
    private (int id, bool valido) VerificarHmacToken(string token)
    {
        try
        {
            // Restaurar Base64 estándar
            var padded = token.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "=";  break;
            }

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var parts   = decoded.Split('|');
            if (parts.Length != 3) return (0, false);

            var id      = int.Parse(parts[0]);
            var ticks   = long.Parse(parts[1]);
            var hmac    = parts[2];
            var payload = $"{id}|{ticks}";

            // Comparación en tiempo constante para evitar timing attacks
            var secret   = _config["Auth:HmacSecret"]!;
            var expected = ComputeHmac(payload, secret);
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hmac),
                    Encoding.UTF8.GetBytes(expected)))
                return (0, false);

            // Verificar TTL de 15 minutos
            var emitido = new DateTime(ticks, DateTimeKind.Utc);
            if ((DateTime.UtcNow - emitido).TotalMinutes > 15)
                return (0, false);

            return (id, true);
        }
        catch
        {
            return (0, false);
        }
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(HMACSHA256.HashData(key, data));
    }
}
