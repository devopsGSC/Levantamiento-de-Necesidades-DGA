namespace DGA.Web.Models;

public class SolicitudListItemViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public int CantidadFotografias { get; set; }
    public bool EsEditable { get; set; }
    public bool PuedeDescartar { get; set; }
}

public class SolicitudIndexViewModel
{
    public List<SolicitudListItemViewModel> Solicitudes { get; set; } = new();
    public string? Busqueda { get; set; }
    public byte? EstadoFiltro { get; set; }
    public List<OpcionCatalogo> EstadoOptions { get; set; } = new();
}

public class SolicitudDetailViewModel
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? UnidadEjecutora { get; set; }
    public string Aduana { get; set; } = string.Empty;
    public string TipoAduana { get; set; } = string.Empty;
    public string JustificacionGeneral { get; set; } = string.Empty;
    public string? ObservacionesGenerales { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool EsEditable { get; set; }
    public bool PuedeDescartar { get; set; }

    /// <summary>El cambio de estado y la bitácora con comentarios se gestionan desde el
    /// Panel de Admin (AdminSolicitudesController), no acá — esta vista es de solo lectura
    /// incluso para un admin viendo su propia solicitud. Con esto la vista puede ofrecer un
    /// atajo directo a esa pantalla en vez de dejar al admin buscarla por su cuenta.</summary>
    public bool EsAdmin { get; set; }

    public List<SolicitudDetailItemViewModel> Items { get; set; } = new();
    public List<SolicitudHistorialItemViewModel> Historial { get; set; } = new();

    /// <summary>Suma de los subtotales de todos los ítems — $0 en los ítems sin costo estimado.</summary>
    public decimal MontoPresupuestadoTotal => Items.Sum(i => i.Subtotal);
}

public class SolicitudDetailItemViewModel
{
    /// <summary>Id real del SolicitudItem — necesario para armar el link de la cotización adjunta.</summary>
    public int Id { get; set; }
    public int NumeroItem { get; set; }
    public string Componente { get; set; } = string.Empty;
    public string Subcomponente { get; set; } = string.Empty;
    public string? Elemento { get; set; }
    public string? Detalle { get; set; }
    public int CantidadSolicitada { get; set; }
    public bool TienePresupuesto { get; set; }
    public decimal CostoEstimado { get; set; }
    public string TipoCosto { get; set; } = "Unitario";
    public string? CotizacionNombreOriginal { get; set; }
    public bool TieneCotizacion => !string.IsNullOrEmpty(CotizacionNombreOriginal);
    public string? TipoSuscripcion { get; set; }
    public int? CantidadPeriodos { get; set; }
    public decimal Subtotal => !TienePresupuesto ? 0 : CostoEstimado * (TipoCosto == "Total" ? 1 : CantidadSolicitada) * (CantidadPeriodos ?? 1);
    public string Prioridad { get; set; } = string.Empty;
    public string? UbicacionEspecifica { get; set; }
    public string? JustificacionItem { get; set; }
    public List<SolicitudFotoViewModel> Fotografias { get; set; } = new();
}

/// <summary>Foto de un ítem para mostrar en pantalla (vistas de detalle) — se sirve
/// autenticada vía SolicitudesController.Foto(Id).</summary>
public class SolicitudFotoViewModel
{
    public int Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
}

public class SolicitudHistorialItemViewModel
{
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Comentario { get; set; }
    public DateTime FechaCambio { get; set; }
}
