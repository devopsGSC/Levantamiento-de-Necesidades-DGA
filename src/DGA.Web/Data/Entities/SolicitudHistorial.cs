namespace DGA.Web.Data.Entities;

/// <summary>
/// Bitácora de cambios de la solicitud. Cubre dos tipos de evento, que se distinguen por
/// qué campos vienen completos:
/// - Cambio de Estado de la solicitud: <see cref="EstadoNuevoId"/> (y opcionalmente
///   <see cref="EstadoAnteriorId"/>) completos, <see cref="SolicitudItemId"/> null.
/// - Ítem marcado/desmarcado como completado: <see cref="SolicitudItemId"/> e
///   <see cref="ItemCompletado"/> completos, <see cref="EstadoNuevoId"/> null.
/// </summary>
public class SolicitudHistorial
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public byte? EstadoAnteriorId { get; set; }
    public byte? EstadoNuevoId { get; set; }

    public int? SolicitudItemId { get; set; }
    /// <summary>true = el ítem se marcó como completado; false = se desmarcó. Null si este
    /// evento no es sobre un ítem.</summary>
    public bool? ItemCompletado { get; set; }

    public int? UsuarioCambioId { get; set; }

    /// <summary>"Comentario Administrativo" visto en la bitácora de cambios.</summary>
    public string? Comentario { get; set; }
    public DateTime FechaCambio { get; set; } = DateTime.UtcNow;

    public Solicitud Solicitud { get; set; } = null!;
    public EstadoSolicitud? EstadoAnterior { get; set; }
    public EstadoSolicitud? EstadoNuevo { get; set; }
    public SolicitudItem? SolicitudItem { get; set; }
    public ApplicationUser? UsuarioCambio { get; set; }
}
