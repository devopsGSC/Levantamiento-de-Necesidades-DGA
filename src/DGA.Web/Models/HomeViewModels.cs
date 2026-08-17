namespace DGA.Web.Models;

public class HomeIndexViewModel
{
    public bool EsAdmin { get; set; }
    public int TotalSolicitudes { get; set; }
    public int EnBorrador { get; set; }
    public int EnTramite { get; set; }
    public int Finalizadas { get; set; }
    public List<HomeRecienteItem> Recientes { get; set; } = new();
}

public class HomeRecienteItem
{
    public string IdSolicitud { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? ComponentePrincipal { get; set; }
    public string Aduana { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
