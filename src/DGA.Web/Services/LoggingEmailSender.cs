namespace DGA.Web.Services;

/// <summary>
/// Implementación temporal: escribe el correo en el log en vez de enviarlo de verdad.
/// El servidor SMTP institucional todavía no está definido (placeholder configurable,
/// igual que los datos de soporte técnico). Reemplazar por un IEmailSender real
/// (SMTP/SendGrid/etc.) cuando se defina, sin tocar el resto de la aplicación —
/// solo se registra la interfaz en Program.cs.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        logger.LogWarning(
            "Envío de correo no configurado (SMTP pendiente de definir). " +
            "Destinatario: {ToEmail} | Asunto: {Subject} | Contenido:\n{Body}",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
