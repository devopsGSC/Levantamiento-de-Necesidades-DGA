using Microsoft.AspNetCore.Identity;

namespace DGA.Web.Data.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string Nombre { get; set; } = string.Empty;
    public string? Departamento { get; set; }
    public bool PasswordTemporal { get; set; }
    public bool PrimerInicioSesion { get; set; } = true;
    public DateTime? CredencialesReenviadasEn { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Único admin habilitado para cambiar el rol de otro Administrador. Se asigna
    /// al primer admin sembrado (ver DbSeeder); no se puede otorgar desde la UI.</summary>
    public bool EsAdminPrincipal { get; set; }

    public ICollection<Solicitud> SolicitudesCreadas { get; set; } = new List<Solicitud>();
    public ICollection<Solicitud> SolicitudesRevisadas { get; set; } = new List<Solicitud>();
}
