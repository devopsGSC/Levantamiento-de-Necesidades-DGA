namespace DGA.Web.Data.Entities;

public class SolicitudItem
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }

    /// <summary>Orden del ítem dentro de la solicitud (1, 2, 3…).</summary>
    public short NumeroItem { get; set; }

    public byte ComponenteId { get; set; }
    public int SubcomponenteId { get; set; }
    public int? ElementoId { get; set; }

    /// <summary>Texto libre cuando el Subcomponente no tiene catálogo de Elementos.</summary>
    public string? ElementoLibre { get; set; }

    /// <summary>Solo aplica si Elemento.TieneDetalle = true.</summary>
    public int? DetalleId { get; set; }

    public int CantidadSolicitada { get; set; }
    public byte PrioridadId { get; set; }
    public string? UbicacionEspecifica { get; set; }

    /// <summary>Justificación propia del ítem (antes "detalle_cascada" en el original — nombre engañoso, no es un nivel de catálogo).</summary>
    public string? JustificacionItem { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Solicitud Solicitud { get; set; } = null!;
    public Componente Componente { get; set; } = null!;
    public Subcomponente Subcomponente { get; set; } = null!;
    public Elemento? Elemento { get; set; }
    public Detalle? Detalle { get; set; }
    public Prioridad Prioridad { get; set; } = null!;

    public ICollection<SolicitudItemFotografia> Fotografias { get; set; } = new List<SolicitudItemFotografia>();
}
