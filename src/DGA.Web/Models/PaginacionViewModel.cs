namespace DGA.Web.Models;

/// <summary>Datos para <c>_Paginacion.cshtml</c> — el partial arma los links leyendo
/// el query string de la request actual y pisando solo "pagina", así preserva
/// cualquier filtro sin que la vista tenga que pasárselos a mano.</summary>
public class PaginacionViewModel
{
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int TotalResultados { get; set; }
}
