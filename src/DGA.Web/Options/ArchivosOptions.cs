namespace DGA.Web.Options;

/// <summary>Configuración del almacenamiento de fotografías adjuntas a los ítems de solicitud.</summary>
public class ArchivosOptions
{
    public const string SectionName = "Archivos";

    /// <summary>
    /// Carpeta RAÍZ donde se guardan las fotos. Debe estar FUERA de wwwroot: los archivos
    /// estáticos se sirven sin pasar por el filtro de autorización de MVC, así que si
    /// vivieran en wwwroot cualquiera con la URL podría verlos sin haber iniciado sesión.
    /// Se sirven autenticados a través de SolicitudesController.Foto.
    /// </summary>
    public string CarpetaRaiz { get; set; } = "App_Data/fotografias";

    public int MaxArchivosPorItem { get; set; } = 10;
    public long MaxBytesPorArchivo { get; set; } = 10 * 1024 * 1024;
    public string[] TiposPermitidos { get; set; } = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}
