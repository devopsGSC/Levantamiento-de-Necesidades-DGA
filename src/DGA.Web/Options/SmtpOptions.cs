namespace DGA.Web.Options;

/// <summary>
/// Configuración SMTP. Host/Puerto/From son públicos (appsettings.json); Usuario y
/// Contraseña son secretos y viven SOLO en User Secrets (dev) o variables de entorno
/// (producción) — nunca en appsettings.json ni en el repositorio.
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string From { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "Levantamiento de Necesidades DGA";
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(User);
}
