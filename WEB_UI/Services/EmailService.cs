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
    public async Task EnviarOtpAsync(string correo, string nombre, string otp)
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
    public async Task EnviarResetPasswordAsync(string correo, string nombre, string resetUrl)
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
