namespace DGA.Web.Data.Entities;

public class SolicitudHistorial
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public byte? EstadoAnteriorId { get; set; }
    public byte EstadoNuevoId { get; set; }
    public int? UsuarioCambioId { get; set; }

    /// <summary>"Comentario Administrativo" visto en la bitácora de cambios.</summary>
    public string? Comentario { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    public Solicitud Solicitud { get; set; } = null!;
    public EstadoSolicitud? EstadoAnterior { get; set; }
    public EstadoSolicitud EstadoNuevo { get; set; } = null!;
    public ApplicationUser? UsuarioCambio { get; set; }
}
