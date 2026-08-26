namespace DGA.Web.Models;

public class AdminSolicitudListItemViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public string Componente { get; set; } = string.Empty;
    public string Elemento { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class AdminSolicitudIndexViewModel
{
    public List<AdminSolicitudListItemViewModel> Solicitudes { get; set; } = new();
    public string? Busqueda { get; set; }
    public byte? EstadoFiltro { get; set; }
    public List<OpcionCatalogo> EstadoOptions { get; set; } = new();
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalResultados { get; set; }
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

    /// <summary>Suma de los subtotales de todos los ítems — $0 en los ítems sin costo estimado.</summary>
    public decimal MontoPresupuestadoTotal => Items.Sum(i => i.Subtotal);
}

public class CambiarEstadoViewModel
{
    public int SolicitudId { get; set; }
    public byte NuevoEstadoId { get; set; }
    public string? Comentario { get; set; }
}
