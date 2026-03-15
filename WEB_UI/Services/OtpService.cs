using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;

namespace WEB_UI.Services;

/// <summary>
/// Gestiona el ciclo de vida de las sesiones OTP:
/// generación, verificación y reenvío.
/// </summary>
public class OtpService
{
    private readonly NativaDbContext _db;
    private readonly EmailService    _email;

    public OtpService(NativaDbContext db, EmailService email)
    {
        _db    = db;
        _email = email;
    }

    // Genera un OTP de 6 dígitos, invalida sesiones previas, guarda y envía por email
    public async Task GenerarYEnviarAsync(Sujeto sujeto)
    {
        // Invalidar sesiones OTP anteriores no usadas del mismo sujeto
        var sesionesAbiertas = await _db.OtpSesiones
            .Where(o => o.IdSujeto == sujeto.Id && !o.Usada)
            .ToListAsync();
        sesionesAbiertas.ForEach(s => s.Usada = true);

        var otp  = Random.Shared.Next(100000, 999999).ToString();
        var hash = BCrypt.Net.BCrypt.HashPassword(otp, workFactor: 12);

        var sesion = new OtpSesion
        {
            IdSujeto       = sujeto.Id,
            HashOtp        = hash,
            Expiracion     = DateTime.UtcNow.AddSeconds(90),
            Usada          = false,
            Intentos       = 0,
            ConteoReenvios = 0
        };

        _db.OtpSesiones.Add(sesion);
        await _db.SaveChangesAsync();

        await _email.EnviarOtpAsync(sujeto.Correo!, sujeto.Nombre!, otp);
    }

    // Verifica el OTP ingresado.
    // Retorna (true, null) si es correcto; (false, mensaje) si no.
    public async Task<(bool ok, string? error)> VerificarAsync(string correo, string otp)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == correo);

        if (sujeto == null)
            return (false, "Correo no encontrado.");

        // Buscar sesión activa más reciente
        var sesion = await _db.OtpSesiones
            .Where(o => o.IdSujeto == sujeto.Id && !o.Usada)
            .OrderByDescending(o => o.FechaCreacion)
            .FirstOrDefaultAsync();

        if (sesion == null)
            return (false, "No hay código activo para este correo.");

        if (DateTime.UtcNow > sesion.Expiracion)
            return (false, "El código ha expirado. Solicita uno nuevo.");

        // Verificar el OTP con BCrypt
        if (!BCrypt.Net.BCrypt.Verify(otp, sesion.HashOtp))
        {
            sesion.Intentos++;

            // Tras 3 fallos, bloquear cuenta y anular sesión
            if (sesion.Intentos >= 3)
            {
                sujeto.Estado = EstadoSujetoEnum.Bloqueado;
                sesion.Usada  = true;
                await _db.SaveChangesAsync();
                // N03 — notificar bloqueo al usuario
                try { await _email.EnviarBloqueoOtpAsync(sujeto.Correo!, sujeto.Nombre!); } catch { }
                return (false, "Código incorrecto. Cuenta bloqueada por seguridad.");
            }

            await _db.SaveChangesAsync();
            return (false, "Código incorrecto.");
        }

        // OTP correcto: activar cuenta y marcar sesión como usada
        sesion.Usada  = true;
        sujeto.Estado = EstadoSujetoEnum.Activo;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    // Reenvía un nuevo OTP a la misma sesión (reutiliza la sesión existente).
    // Respeta cooldown de 30s y máx 3 reenvíos.
    public async Task<(bool ok, string? error)> ReenviarAsync(string correo)
    {
        var sujeto = await _db.Sujetos
            .FirstOrDefaultAsync(s => s.Correo == correo);

        if (sujeto == null)
            return (false, "Correo no encontrado.");

        var sesion = await _db.OtpSesiones
            .Where(o => o.IdSujeto == sujeto.Id && !o.Usada)
            .OrderByDescending(o => o.FechaCreacion)
            .FirstOrDefaultAsync();

        if (sesion == null)
            return (false, "No hay sesión activa.");

        // Cooldown de 30 segundos entre reenvíos
        if (sesion.UltimoReenvio.HasValue &&
            (DateTime.UtcNow - sesion.UltimoReenvio.Value).TotalSeconds < 30)
            return (false, "Espera 30 segundos antes de solicitar otro código.");

        // Máximo 3 reenvíos por sesión
        if (sesion.ConteoReenvios >= 3)
            return (false, "Límite de reenvíos alcanzado. Vuelve a registrarte.");

        // Generar nuevo OTP en la misma sesión (extiende expiración)
        var otp  = Random.Shared.Next(100000, 999999).ToString();
        var hash = BCrypt.Net.BCrypt.HashPassword(otp, workFactor: 12);

        sesion.HashOtp        = hash;
        sesion.Expiracion     = DateTime.UtcNow.AddSeconds(90);
        sesion.Intentos       = 0;
        sesion.UltimoReenvio  = DateTime.UtcNow;
        sesion.ConteoReenvios++;

        await _db.SaveChangesAsync();
        await _email.EnviarOtpAsync(sujeto.Correo!, sujeto.Nombre!, otp);

        return (true, null);
    }
}
