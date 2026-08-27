using DGA.Web.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DGA.Web.Services;

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromDisplayName, smtp.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (htmlBody.Contains($"cid:{CredencialesEmailTemplate.LogoContentId}"))
        {
            var logo = builder.LinkedResources.Add(CredencialesEmailTemplate.LogoContentId + ".png", CredencialesEmailTemplate.LogoBytes);
            logo.ContentId = CredencialesEmailTemplate.LogoContentId;
        }
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
            await client.AuthenticateAsync(smtp.User, smtp.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logger.LogInformation("Correo enviado a {ToEmail}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar correo a {ToEmail}", toEmail);
            throw;
        }
    }
}
