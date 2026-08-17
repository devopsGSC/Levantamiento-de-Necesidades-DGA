namespace DGA.Web.Data.Entities;

/// <summary>Un reporte semanal ya generado (automático los domingos, o manual). Los
/// archivos en sí viven en disco fuera de wwwroot; acá solo se guarda la ruta y metadata.</summary>
public class ReporteSemanal
{
    public int Id { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int CantidadSolicitudes { get; set; }
    public string RutaPdf { get; set; } = string.Empty;
    public string RutaExcel { get; set; } = string.Empty;
    public bool GeneradoManualmente { get; set; }
    public int? GeneradoPorUsuarioId { get; set; }
    public DateTime GeneradoEn { get; set; } = DateTime.UtcNow;

    public ApplicationUser? GeneradoPorUsuario { get; set; }
}
