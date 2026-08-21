using System.ComponentModel.DataAnnotations;

namespace DGA.Web.Models;

public class SolicitudFormViewModel
{
    public int Id { get; set; }
    public string? IdSolicitud { get; set; }

    /// <summary>Solo para mostrar en pantalla en una solicitud nueva, antes de guardar —
    /// próximo correlativo probable (no reservado). Nunca se usa como el IdSolicitud real;
    /// ese se genera en Guardar() de forma atómica. Ver SolicitudIdGenerator.PrevisualizarProximoIdAsync.</summary>
    public string? IdSolicitudPrevisualizado { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre del responsable.")]
    [Display(Name = "Nombre Solicitante")]
    public string NombreResponsable { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccioná el cargo.")]
    [Display(Name = "Cargo")]
    public byte? CargoId { get; set; }

    [Required(ErrorMessage = "Seleccioná el tipo de aduana.")]
    [Display(Name = "Tipo de Aduana")]
    public byte? TipoAduanaId { get; set; }

    [Required(ErrorMessage = "Seleccioná la aduana.")]
    [Display(Name = "Nombre de Aduana")]
    public int? AduanaId { get; set; }

    [Required(ErrorMessage = "Ingresá la justificación general.")]
    [Display(Name = "Justificación General")]
    public string JustificacionGeneral { get; set; } = string.Empty;

    [Display(Name = "Observaciones Generales")]
    public string? ObservacionesGenerales { get; set; }

    /// <summary>JSON serializado de List&lt;SolicitudItemFormViewModel&gt; armado en el cliente.</summary>
    public string ItemsJson { get; set; } = "[]";

    public string Accion { get; set; } = "borrador"; // "borrador" | "finalizar"

    // -- Opciones de catálogo para poblar los combos (las llena el controlador) --
    public List<OpcionCatalogo> CargoOptions { get; set; } = new();
    public List<OpcionCatalogo> TipoAduanaOptions { get; set; } = new();
    public List<OpcionCatalogo> ComponenteOptions { get; set; } = new();
    public List<OpcionCatalogo> PrioridadOptions { get; set; } = new();

    public string ItemsExistentesJson { get; set; } = "[]";

    /// <summary>IDs de Elemento/Detalle que son suscripción recurrente (ver
    /// DGA.Web.Data.CatalogoSuscripciones) — el JS del formulario los usa para mostrar los
    /// campos de Tipo de Suscripción / Cantidad de Períodos cuando corresponde.</summary>
    public int[] ElementoIdsSuscripcion { get; set; } = DGA.Web.Data.CatalogoSuscripciones.ElementoIds;
    public int[] DetalleIdsSuscripcion { get; set; } = DGA.Web.Data.CatalogoSuscripciones.DetalleIds;

    public bool EsEdicion => Id > 0;
    public bool PuedeDescartar { get; set; }
}

public record OpcionCatalogo(int Id, string Nombre);

/// <summary>Forma de cada ítem tal como lo arma/lee el JavaScript del formulario (JSON).</summary>
public class SolicitudItemFormViewModel
{
    /// <summary>Id real del SolicitudItem cuando ya existe (edición) — 0 en un ítem nuevo.
    /// Se usa solo para poder previsualizar la cotización ya guardada antes de re-guardar.</summary>
    public int Id { get; set; }
    public int NumeroItem { get; set; }
    public byte ComponenteId { get; set; }
    public string ComponenteNombre { get; set; } = string.Empty;
    public int SubcomponenteId { get; set; }
    public string SubcomponenteNombre { get; set; } = string.Empty;
    public int? ElementoId { get; set; }
    public string? ElementoNombre { get; set; }
    public string? ElementoLibre { get; set; }
    public int? DetalleId { get; set; }
    public string? DetalleNombre { get; set; }
    public int CantidadSolicitada { get; set; }
    public decimal CostoEstimado { get; set; }
    public string TipoCosto { get; set; } = "Unitario";
    public string? CotizacionTokenNuevo { get; set; }
    public string? CotizacionNombreOriginalNuevo { get; set; }
    public string? CotizacionRutaExistente { get; set; }
    public string? CotizacionNombreExistente { get; set; }
    public string? TipoSuscripcion { get; set; }
    public int? CantidadPeriodos { get; set; }
    public byte PrioridadId { get; set; }
    public string? UbicacionEspecifica { get; set; }
    public string? JustificacionItem { get; set; }

    /// <summary>Tokens de fotos recién subidas (carpeta temporal) pendientes de confirmar.</summary>
    public List<string> FotografiasNuevas { get; set; } = new();

    /// <summary>Fotos ya guardadas de una edición existente (ruta relativa ya confirmada).</summary>
    public List<SolicitudFotoExistenteViewModel> FotografiasExistentes { get; set; } = new();
}

/// <summary>Foto ya confirmada de un ítem existente — incluye el Id para poder previsualizarla
/// (autenticado, vía SolicitudesController.Foto) mientras se edita, antes de volver a guardar.</summary>
public class SolicitudFotoExistenteViewModel
{
    public int Id { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
}
