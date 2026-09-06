using System.ComponentModel.DataAnnotations.Schema;

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

    /// <summary>Si el usuario indicó tener un monto presupuestado para este ítem. Opcional —
    /// en "No" (el valor por defecto) <see cref="CostoEstimado"/>, <see cref="TipoCosto"/> y la
    /// cotización adjunta quedan sin usar (0 / valor por defecto / null).</summary>
    public bool TienePresupuesto { get; set; }

    /// <summary>Monto que ingresa el usuario al armar el ítem — solo aplica si
    /// <see cref="TienePresupuesto"/> es true, y en ese caso es obligatorio y mayor a 0. Su
    /// significado depende de <see cref="TipoCosto"/>: si es "Unitario" es el costo de UNA
    /// unidad (ej. una cerámica) y el subtotal multiplica por CantidadSolicitada; si es
    /// "Total" ya incluye todo (ej. cerámica + mano de obra) y no se vuelve a multiplicar.
    /// El subtotal presupuestado del ítem es
    /// CostoEstimado * (TipoCosto == "Total" ? 1 : CantidadSolicitada) * (CantidadPeriodos ?? 1).</summary>
    [Column(TypeName = "decimal(12,2)")]
    public decimal CostoEstimado { get; set; }

    /// <summary>"Unitario" / "Total" — qué representa <see cref="CostoEstimado"/>.</summary>
    public string TipoCosto { get; set; } = "Unitario";

    /// <summary>Ruta en disco de la cotización adjunta (imagen o PDF) — opcional.</summary>
    public string? CotizacionRuta { get; set; }
    public string? CotizacionNombreOriginal { get; set; }

    /// <summary>"Mensual" / "Anual" — solo se completa en ítems de suscripción recurrente
    /// (ver <see cref="Data.CatalogoSuscripciones"/>); null en el resto de los ítems.</summary>
    public string? TipoSuscripcion { get; set; }

    /// <summary>Cantidad de meses (si Mensual) o años (si Anual) de la suscripción. Null en
    /// ítems que no son suscripción.</summary>
    public int? CantidadPeriodos { get; set; }

    public byte PrioridadId { get; set; }
    public string? UbicacionEspecifica { get; set; }

    /// <summary>Justificación propia del ítem (antes "detalle_cascada" en el original — nombre engañoso, no es un nivel de catálogo).</summary>
    public string? JustificacionItem { get; set; }

    /// <summary>Quién va a tramitar ESTE ítem — no toda la solicitud, porque ítems de una
    /// misma solicitud pueden ir a unidades distintas. La define el administrador recién
    /// con la solicitud Aprobada (ver <see cref="Data.Estados.PermiteGestionItems"/>). Null
    /// hasta que se asigna.</summary>
    public byte? UnidadEjecutoraId { get; set; }

    /// <summary>Si el ítem ya fue completado por la Unidad Ejecutora asignada (o por un
    /// admin). El Progreso (%) de la solicitud se calcula a partir de esto — ver
    /// <see cref="Data.ItemCompletado"/>.</summary>
    public bool Completado { get; set; }
    public DateTime? FechaCompletado { get; set; }
    public int? CompletadoPorUsuarioId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Solicitud Solicitud { get; set; } = null!;
    public Componente Componente { get; set; } = null!;
    public Subcomponente Subcomponente { get; set; } = null!;
    public Elemento? Elemento { get; set; }
    public Detalle? Detalle { get; set; }
    public Prioridad Prioridad { get; set; } = null!;
    public UnidadEjecutora? UnidadEjecutora { get; set; }
    public ApplicationUser? CompletadoPorUsuario { get; set; }

    public ICollection<SolicitudItemFotografia> Fotografias { get; set; } = new List<SolicitudItemFotografia>();
}
