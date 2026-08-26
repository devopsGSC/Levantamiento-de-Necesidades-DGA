namespace DGA.Web.Models;

public class DashboardViewModel
{
    public DateTime? UltimaSolicitudFecha { get; set; }

    public int? FiltroAduanaId { get; set; }
    public byte? FiltroComponenteId { get; set; }
    public List<OpcionCatalogo> AduanaOptions { get; set; } = new();
    public List<OpcionCatalogo> ComponenteOptions { get; set; } = new();

    public int Total { get; set; }
    public int Finalizadas { get; set; }
    public int EnProceso { get; set; }
    public int Pendientes { get; set; }
    public int Denegadas { get; set; }
    public int Borradores { get; set; }
    public int PrioridadAlta { get; set; }
    public double ProgresoPromedio { get; set; }

    public double Pct(int valor) => Total == 0 ? 0 : Math.Round(valor * 100.0 / Total, 0);

    public List<DashboardBucket> DistribucionProgreso { get; set; } = new();
    public List<DashboardBucket> PorPrioridad { get; set; } = new();
    public List<DashboardBucket> PorEstado { get; set; } = new();
    public List<DashboardAduanaRendimiento> TopAduanas { get; set; } = new();

    public List<string> TendenciaLabels { get; set; } = new();
    public List<int> TendenciaValores { get; set; } = new();

    public List<string> PorAduanaLabels { get; set; } = new();
    public List<int> PorAduanaValores { get; set; } = new();

    public List<string> PorComponenteLabels { get; set; } = new();
    public List<int> PorComponenteValores { get; set; } = new();

    public List<DashboardSolicitudReciente> SolicitudesRecientes { get; set; } = new();
}

public class DashboardBucket
{
    public string Etiqueta { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public double Porcentaje { get; set; }
}

public class DashboardAduanaRendimiento
{
    public string Aduana { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Finalizadas { get; set; }
    public double PorcentajeFinalizadas { get; set; }
}

public class DashboardSolicitudReciente
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public string Componente { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public byte Progreso { get; set; }
    public DateTime FechaRegistro { get; set; }
}
