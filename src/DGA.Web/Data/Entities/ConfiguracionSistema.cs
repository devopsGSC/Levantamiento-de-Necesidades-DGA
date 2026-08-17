namespace DGA.Web.Data.Entities;

/// <summary>Configuración global editable por el admin desde Admin/Configuracion — fila
/// única (Id siempre 1). Reemplaza los valores fijos que antes vivían en appsettings.json.</summary>
public class ConfiguracionSistema
{
    public int Id { get; set; }
    public string SoporteTelefono { get; set; } = string.Empty;
    public string SoporteCorreo { get; set; } = string.Empty;
    public string SoporteHorario { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
