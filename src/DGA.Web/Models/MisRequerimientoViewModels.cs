namespace DGA.Web.Models;

/// <summary>Lo que ve un usuario de un rol delegado (Compras DGA, Mantenimiento DGA, Otro):
/// solo las solicitudes que el admin le asignó a su Unidad Ejecutora al aprobarlas.</summary>
public class MisRequerimientoListItemViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public byte EstadoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public DateTime? FechaRevision { get; set; }
    public byte? Progreso { get; set; }

    /// <summary>Cuántos de los ítems de la solicitud están asignados a la Unidad Ejecutora de
    /// este usuario — no el total de ítems de la solicitud, porque los de otras unidades ni
    /// siquiera se le muestran.</summary>
    public int CantidadItemsAsignados { get; set; }
}

public class MisRequerimientoIndexViewModel
{
    public List<MisRequerimientoListItemViewModel> Solicitudes { get; set; } = new();
    public string? Busqueda { get; set; }
    public byte? EstadoFiltro { get; set; }
    public List<OpcionCatalogo> EstadoOptions { get; set; } = new();

    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalResultados { get; set; }
}

public class MisRequerimientoDetailViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public byte EstadoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string Aduana { get; set; } = string.Empty;
    public string TipoAduana { get; set; } = string.Empty;
    public string JustificacionGeneral { get; set; } = string.Empty;
    public string? ObservacionesGenerales { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaRevision { get; set; }
    public byte? Progreso { get; set; }

    /// <summary>Si todavía se pueden marcar ítems como completados (Aprobada o En Proceso).</summary>
    public bool PermiteGestionItems { get; set; }
    public int ItemsCompletados => Items.Count(i => i.Completado);

    /// <summary>Nombre del único estado al que este rol puede avanzar la solicitud
    /// (Aprobado -> En Proceso -> Finalizado). Null cuando ya no hay a dónde avanzar
    /// (por ejemplo, ya está Finalizado).</summary>
    public string? SiguienteEstadoNombre { get; set; }
    public bool PuedeAvanzar => SiguienteEstadoNombre is not null;

    public List<SolicitudDetailItemViewModel> Items { get; set; } = new();
    public List<SolicitudHistorialItemViewModel> Historial { get; set; } = new();

    /// <summary>Suma de los subtotales de todos los ítems — $0 en los ítems sin costo estimado.</summary>
    public decimal MontoPresupuestadoTotal => Items.Sum(i => i.Subtotal);
}
