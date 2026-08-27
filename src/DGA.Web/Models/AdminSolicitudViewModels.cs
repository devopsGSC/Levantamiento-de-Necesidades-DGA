namespace DGA.Web.Models;

public class AdminSolicitudListItemViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public string Componente { get; set; } = string.Empty;
    public string Elemento { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}

public class AdminSolicitudIndexViewModel
{
    public List<AdminSolicitudListItemViewModel> Solicitudes { get; set; } = new();

    public string? Busqueda { get; set; }
    public byte? EstadoFiltro { get; set; }
    public byte? TipoAduanaFiltro { get; set; }
    public int? AduanaFiltro { get; set; }
    public byte? ComponenteFiltro { get; set; }
    public int? SubcomponenteFiltro { get; set; }
    public int? ElementoFiltro { get; set; }
    public byte? PrioridadFiltro { get; set; }
    public byte? UnidadEjecutoraFiltro { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public List<OpcionCatalogo> EstadoOptions { get; set; } = new();
    public List<OpcionCatalogo> TipoAduanaOptions { get; set; } = new();
    public List<OpcionCatalogo> AduanaOptions { get; set; } = new();
    public List<OpcionCatalogo> ComponenteOptions { get; set; } = new();
    public List<OpcionCatalogo> SubcomponenteOptions { get; set; } = new();
    public List<OpcionCatalogo> ElementoOptions { get; set; } = new();
    public List<OpcionCatalogo> PrioridadOptions { get; set; } = new();
    public List<OpcionCatalogo> UnidadEjecutoraOptions { get; set; } = new();

    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalResultados { get; set; }

    public bool TieneFiltrosActivos =>
        !string.IsNullOrWhiteSpace(Busqueda) || EstadoFiltro.HasValue || TipoAduanaFiltro.HasValue ||
        AduanaFiltro.HasValue || ComponenteFiltro.HasValue || SubcomponenteFiltro.HasValue ||
        ElementoFiltro.HasValue || PrioridadFiltro.HasValue || UnidadEjecutoraFiltro.HasValue ||
        FechaDesde.HasValue || FechaHasta.HasValue;
}

public class AdminSolicitudDetailViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public byte EstadoActualId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? UnidadEjecutora { get; set; }
    public byte? UnidadEjecutoraId { get; set; }
    public string Aduana { get; set; } = string.Empty;
    public string TipoAduana { get; set; } = string.Empty;
    public string JustificacionGeneral { get; set; } = string.Empty;
    public string? ObservacionesGenerales { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? FechaRevision { get; set; }
    public byte? Progreso { get; set; }

    public List<SolicitudDetailItemViewModel> Items { get; set; } = new();
    public List<SolicitudHistorialItemViewModel> Historial { get; set; } = new();
    public List<OpcionCatalogo> EstadoOptions { get; set; } = new();
    public List<OpcionCatalogo> UnidadEjecutoraOptions { get; set; } = new();

    /// <summary>Suma de los subtotales de todos los ítems — $0 en los ítems sin costo estimado.</summary>
    public decimal MontoPresupuestadoTotal => Items.Sum(i => i.Subtotal);
}

public class CambiarEstadoViewModel
{
    public int SolicitudId { get; set; }
    public byte NuevoEstadoId { get; set; }
    public byte? UnidadEjecutoraId { get; set; }
    public string? Comentario { get; set; }
}
