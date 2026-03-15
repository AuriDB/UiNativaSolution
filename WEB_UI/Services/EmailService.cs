using MailKit.Net.Smtp;
using MimeKit;

namespace WEB_UI.Services;

public class EmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration cfg, ILogger<EmailService> logger)
    {
        _cfg    = cfg;
        _logger = logger;
    }

    public async Task EnviarOtpAsync(string destino, string nombre, string otp)
    {
        var asunto = "Código de verificación — Sistema Nativa";
        var cuerpo = $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Tu código de verificación es:</p>
            <h2 style="letter-spacing:6px;color:#78c2ad">{otp}</h2>
            <p>Caduca en <strong>90 segundos</strong>. No lo compartas con nadie.</p>
            <hr/>
            <small>Sistema Nativa — Pago por Servicios Ambientales, Costa Rica</small>
            """;
        await EnviarAsync(destino, asunto, cuerpo);
    }

    public async Task EnviarResetPasswordAsync(string destino, string nombre, string resetUrl)
    {
        var asunto = "Restablecer contraseña — Sistema Nativa";
        var cuerpo = $"""
            <p>Hola <strong>{nombre}</strong>,</p>
            <p>Recibimos una solicitud para restablecer tu contraseña.</p>
            <p><a href="{resetUrl}" style="background:#78c2ad;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;">
               Restablecer contraseña
            </a></p>
            <p>Este enlace expira en <strong>15 minutos</strong> y solo puede usarse una vez.</p>
            <p>Si no solicitaste esto, ignora este correo.</p>
            <hr/>
            <small>Sistema Nativa — Pago por Servicios Ambientales, Costa Rica</small>
            """;
        await EnviarAsync(destino, asunto, cuerpo);
    }

    public virtual async Task EnviarGenericoAsync(string destino, string asunto, string cuerpoHtml)
        => await EnviarAsync(destino, asunto, cuerpoHtml);

    private async Task EnviarAsync(string destino, string asunto, string cuerpoHtml)
    {
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(
                _cfg["Email:DisplayName"] ?? "Sistema Nativa",
                _cfg["Email:From"]        ?? "nativa@noreply.cr"));
            msg.To.Add(MailboxAddress.Parse(destino));
            msg.Subject = asunto;
            msg.Body    = new TextPart("html") { Text = cuerpoHtml };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _cfg["Email:Host"] ?? "smtp.mailtrap.io",
                int.Parse(_cfg["Email:Port"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _cfg["Email:User"] ?? "",
                _cfg["Email:Pass"] ?? "");
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email a {Destino}", destino);
        }
    }
}
