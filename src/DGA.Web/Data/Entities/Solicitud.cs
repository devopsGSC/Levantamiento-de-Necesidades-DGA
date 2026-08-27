namespace DGA.Web.Data.Entities;

public class Solicitud
{
    public int Id { get; set; }

    /// <summary>"SOL-00001" — generado server-side con dbo.SolicitudIdSequence, nunca en el cliente.</summary>
    public string IdSolicitud { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
    public string NombreResponsable { get; set; } = string.Empty;
    public byte CargoId { get; set; }

    /// <summary>Quién va a tramitar la solicitud — no la elige el usuario al armarla, la
    /// define el administrador recién al aprobarla (ver <see cref="Data.Estados.RequiereUnidadEjecutora"/>).
    /// Null mientras la solicitud no llegó a Aprobado.</summary>
    public byte? UnidadEjecutoraId { get; set; }

    public int AduanaId { get; set; }
    public string JustificacionGeneral { get; set; } = string.Empty;
    public string? ObservacionesGenerales { get; set; }
    public byte EstadoId { get; set; }
    public int? AdminRevisorId { get; set; }

    /// <summary>Progreso 0-100 (antes "medicion" en el original).</summary>
    public byte? Progreso { get; set; }

    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? FechaRevision { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ApplicationUser Usuario { get; set; } = null!;
    public ApplicationUser? AdminRevisor { get; set; }
    public Cargo Cargo { get; set; } = null!;
    public UnidadEjecutora? UnidadEjecutora { get; set; }
    public Aduana Aduana { get; set; } = null!;
    public EstadoSolicitud Estado { get; set; } = null!;

    public ICollection<SolicitudItem> Items { get; set; } = new List<SolicitudItem>();
    public ICollection<SolicitudHistorial> Historial { get; set; } = new List<SolicitudHistorial>();
}
