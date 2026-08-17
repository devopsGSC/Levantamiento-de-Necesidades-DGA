namespace DGA.Web.Data.Entities;

public class SolicitudItemFotografia
{
    public int Id { get; set; }
    public int SolicitudItemId { get; set; }

    /// <summary>Ruta en disco/blob storage — el mecanismo de almacenamiento se define en el módulo de archivos.</summary>
    public string RutaArchivo { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int TamanoBytes { get; set; }
    public DateTime SubidoEn { get; set; } = DateTime.UtcNow;

    public SolicitudItem SolicitudItem { get; set; } = null!;
}
