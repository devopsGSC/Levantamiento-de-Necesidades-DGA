namespace DGA.Web.Models;

public class AdminReportesIndexViewModel
{
    public List<ReporteSemanalListItemViewModel> Reportes { get; set; } = new();
}

public class ReporteSemanalListItemViewModel
{
    public int Id { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int CantidadSolicitudes { get; set; }
    public bool GeneradoManualmente { get; set; }
    public DateTime GeneradoEn { get; set; }
    public string? GeneradoPor { get; set; }
}
