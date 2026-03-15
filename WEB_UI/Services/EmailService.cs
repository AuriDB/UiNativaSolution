using System.Net;
using System.Net.Mail;

namespace WEB_UI.Services;

/// <summary>
/// Envía correos transaccionales del sistema (OTP y reset de contraseña).
/// En desarrollo usa Mailtrap; en producción configurar variables reales.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // Envía el código OTP al correo del sujeto
    public virtual async Task EnviarOtpAsync(string correo, string nombre, string otp)
    {
        var asunto = "Tu código de verificación — Sistema Nativa";
        var cuerpo = $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu código de verificación es:</p>
            <h2 style="letter-spacing:8px;font-family:monospace;color:#78c2ad">{otp}</h2>
            <p>Este código expira en <strong>90 segundos</strong>.</p>
            <p>Si no solicitaste este código, ignora este mensaje.</p>
            <hr/>
            <small style="color:#888">Sistema Nativa — Pago por Servicios Ambientales, Costa Rica</small>
            """;

        await EnviarAsync(correo, asunto, cuerpo);
    }

    // Envía el enlace de restablecimiento de contraseña
    public virtual async Task EnviarResetPasswordAsync(string correo, string nombre, string resetUrl)
    {
        var asunto = "Restablecer contraseña — Sistema Nativa";
        var cuerpo = $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Haz clic en el siguiente enlace para restablecer tu contraseña:</p>
            <p><a href="{resetUrl}" style="color:#78c2ad">{resetUrl}</a></p>
            <p>Este enlace expira en <strong>15 minutos</strong> y es de un solo uso.</p>
            <p>Si no solicitaste esto, ignora este mensaje y tu contraseña no cambiará.</p>
            <hr/>
            <small style="color:#888">Sistema Nativa — Pago por Servicios Ambientales, Costa Rica</small>
            """;

        await EnviarAsync(correo, asunto, cuerpo);
    }

    // ── Notificaciones del sistema (N03–N14) ────────────────────────────────

    // N03 — Bloqueo por 3 intentos OTP fallidos
    public virtual async Task EnviarBloqueoOtpAsync(string correo, string nombre)
    {
        await EnviarAsync(correo, "Cuenta bloqueada — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu cuenta ha sido <strong>bloqueada</strong> por exceder el número de intentos de verificación OTP.</p>
            <p>Contacta al administrador del sistema para reactivarla.</p>
            {_pie}
            """);
    }

    // N05 — Finca asignada a ingeniero (pasa a EnRevision)
    public virtual async Task EnviarFincaEnRevisionAsync(string correo, string nombre, int idFinca)
    {
        await EnviarAsync(correo, $"Tu finca #{idFinca} está en revisión — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu finca <strong>#{idFinca}</strong> ha sido tomada por un ingeniero y se encuentra
               actualmente <strong>En Revisión</strong>.</p>
            <p>Recibirás otro correo cuando el ingeniero emita su dictamen.</p>
            {_pie}
            """);
    }

    // N07 — Dictamen: Aprobada
    public virtual async Task EnviarDictamenAprobadaAsync(string correo, string nombre, int idFinca)
    {
        await EnviarAsync(correo, $"🎉 Tu finca #{idFinca} fue aprobada — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>¡Buenas noticias! Tu finca <strong>#{idFinca}</strong> ha sido
               <span style="color:#28a745;font-weight:bold">APROBADA</span> por el ingeniero evaluador.</p>
            <p>Pronto recibirás información sobre tu plan de pago PSA.</p>
            {_pie}
            """);
    }

    // N08 — Dictamen: Rechazada
    public virtual async Task EnviarDictamenRechazadaAsync(string correo, string nombre, int idFinca, string observaciones)
    {
        await EnviarAsync(correo, $"Tu finca #{idFinca} fue rechazada — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu finca <strong>#{idFinca}</strong> ha sido
               <span style="color:#dc3545;font-weight:bold">RECHAZADA</span>.</p>
            <p><strong>Motivo:</strong> {observaciones}</p>
            <p>Si tienes dudas, contacta al equipo de Nativa.</p>
            {_pie}
            """);
    }

    // N09 — Dictamen: Devuelta con observaciones
    public virtual async Task EnviarDictamenDevueltaAsync(string correo, string nombre, int idFinca, string observaciones)
    {
        await EnviarAsync(correo, $"Tu finca #{idFinca} fue devuelta — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu finca <strong>#{idFinca}</strong> ha sido
               <span style="color:#fd7e14;font-weight:bold">DEVUELTA</span> para correcciones.</p>
            <p><strong>Observaciones del ingeniero:</strong></p>
            <blockquote style="border-left:4px solid #fd7e14;padding-left:1rem;color:#555">
                {observaciones}
            </blockquote>
            <p>Por favor, actualiza los datos y reenvíala desde tu portal.</p>
            {_pie}
            """);
    }

    // N10 — Pago mensual ejecutado
    public virtual async Task EnviarPagoEjecutadoAsync(
        string correo, string nombre, int numeroPago, decimal monto, DateTime fechaPago)
    {
        await EnviarAsync(correo, $"Pago PSA #{numeroPago} ejecutado — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu pago mensual PSA número <strong>{numeroPago} de 12</strong> ha sido ejecutado.</p>
            <table style="border-collapse:collapse;width:100%;max-width:400px">
                <tr><td style="padding:6px;color:#888">Monto:</td>
                    <td style="padding:6px;font-weight:bold">₡{monto:N2}</td></tr>
                <tr><td style="padding:6px;color:#888">Fecha de ejecución:</td>
                    <td style="padding:6px">{fechaPago:dd/MM/yyyy HH:mm} UTC</td></tr>
                <tr><td style="padding:6px;color:#888">Cuota:</td>
                    <td style="padding:6px">{numeroPago} / 12</td></tr>
            </table>
            <p>Puedes consultar tu historial completo en el portal.</p>
            {_pie}
            """);
    }

    // N12 — Contrato PSA vencido (pago #12 ejecutado)
    public virtual async Task EnviarContratoVencidoAsync(string correo, string nombre, int idFinca)
    {
        await EnviarAsync(correo, $"Tu contrato PSA ha finalizado — Finca #{idFinca}", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu contrato de Pago por Servicios Ambientales para la finca <strong>#{idFinca}</strong>
               ha <strong>finalizado</strong>. Los 12 pagos mensuales han sido completados exitosamente.</p>
            <p>Si deseas participar nuevamente, puedes registrar una nueva solicitud en el portal.</p>
            {_pie}
            """);
    }

    // N13 — Cuenta inactivada por el administrador
    public virtual async Task EnviarCuentaInactivadaAsync(string correo, string nombre)
    {
        await EnviarAsync(correo, "Tu cuenta ha sido inactivada — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu cuenta en Sistema Nativa ha sido <strong>inactivada</strong> por un administrador.</p>
            <p>Si crees que esto es un error, contacta al equipo de soporte.</p>
            {_pie}
            """);
    }

    // N14 — Nuevos parámetros de pago configurados (Opción B)
    public virtual async Task EnviarParametrosActualizadosAsync(string correo, string nombre)
    {
        await EnviarAsync(correo, "Actualización de parámetros PSA — Sistema Nativa", $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>El administrador ha configurado nuevos parámetros de pago PSA en el sistema.</p>
            <p>Tus pagos existentes <strong>no se ven afectados</strong> (se calcularon con los
               parámetros vigentes al activar tu plan). Los nuevos contratos usarán la configuración actualizada.</p>
            {_pie}
            """);
    }

    // ── Método base ─────────────────────────────────────────────────────────────

    private static readonly string _pie = """
        <hr/>
        <small style="color:#888">Sistema Nativa — Pago por Servicios Ambientales, Costa Rica</small>
        """;

    // Método base: construye y envía el correo vía SMTP
    private async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        var from        = _config["Email:From"]!;
        var displayName = _config["Email:DisplayName"] ?? "Sistema Nativa";
        var host        = _config["Email:Host"]!;
        var port        = _config.GetValue<int>("Email:Port");
        var user        = _config["Email:User"]!;
        var pass        = _config["Email:Pass"]!;

        using var smtp = new SmtpClient(host, port)
        {
            Credentials    = new NetworkCredential(user, pass),
            EnableSsl      = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using var msg = new MailMessage
        {
            From       = new MailAddress(from, displayName),
            Subject    = asunto,
            Body       = cuerpo,
            IsBodyHtml = true
        };
        msg.To.Add(destinatario);

        await smtp.SendMailAsync(msg);
    }
}
