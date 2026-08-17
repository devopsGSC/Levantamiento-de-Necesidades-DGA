using DGA.Web.Options;
using Microsoft.Extensions.Options;

namespace DGA.Web.Services;

public class ArchivoInvalidoException(string mensaje) : Exception(mensaje);

/// <summary>
/// Guarda fotos de ítems en disco, FUERA de wwwroot (ver comentario en ArchivosOptions).
/// Flujo: al agregar una foto en el formulario de ítem, se sube de inmediato a una
/// carpeta temporal (el ítem todavía no existe en la BD — se está armando en el cliente).
/// Al guardar la solicitud (Borrador o Finalizar), se "confirman" los archivos temporales
/// moviéndolos a su carpeta definitiva por solicitud/ítem.
/// </summary>
public class FileStorageService(IOptions<ArchivosOptions> options, IWebHostEnvironment env)
{
    private readonly ArchivosOptions _opciones = options.Value;

    private string RutaRaizAbsoluta => Path.Combine(env.ContentRootPath, _opciones.CarpetaRaiz);
    private string RutaTemporalAbsoluta => Path.Combine(RutaRaizAbsoluta, "_temp");

    public async Task<string> GuardarTemporalAsync(IFormFile archivo)
    {
        if (!_opciones.TiposPermitidos.Contains(archivo.ContentType))
        {
            throw new ArchivoInvalidoException($"Formato no permitido: {archivo.ContentType}. Solo JPG, PNG, GIF o WebP.");
        }
        if (archivo.Length > _opciones.MaxBytesPorArchivo)
        {
            var maxMb = _opciones.MaxBytesPorArchivo / (1024 * 1024);
            throw new ArchivoInvalidoException($"El archivo supera el tamaño máximo de {maxMb}MB.");
        }

        Directory.CreateDirectory(RutaTemporalAbsoluta);

        var extension = Path.GetExtension(archivo.FileName);
        var token = $"{Guid.NewGuid():N}{extension}";
        var rutaDestino = Path.Combine(RutaTemporalAbsoluta, token);

        await using (var destino = File.Create(rutaDestino))
        {
            await archivo.CopyToAsync(destino);
        }

        return token;
    }

    public void EliminarTemporal(string token)
    {
        var ruta = Path.Combine(RutaTemporalAbsoluta, SoloNombreArchivo(token));
        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }
    }

    /// <summary>Mueve un archivo temporal a su ubicación definitiva y devuelve la ruta relativa a guardar en la BD.</summary>
    public string ConfirmarArchivo(string tokenTemporal, string idSolicitud, int numeroItem)
    {
        var origen = Path.Combine(RutaTemporalAbsoluta, SoloNombreArchivo(tokenTemporal));
        if (!File.Exists(origen))
        {
            throw new ArchivoInvalidoException("El archivo temporal ya no existe (puede haber expirado). Volvé a adjuntarlo.");
        }

        var carpetaDestino = Path.Combine(RutaRaizAbsoluta, idSolicitud, numeroItem.ToString());
        Directory.CreateDirectory(carpetaDestino);

        var nombreArchivo = SoloNombreArchivo(tokenTemporal);
        var rutaDestino = Path.Combine(carpetaDestino, nombreArchivo);
        File.Move(origen, rutaDestino, overwrite: true);

        return Path.Combine(idSolicitud, numeroItem.ToString(), nombreArchivo).Replace('\\', '/');
    }

    public string RutaFisicaCompleta(string rutaRelativa) => Path.Combine(RutaRaizAbsoluta, rutaRelativa.Replace('/', Path.DirectorySeparatorChar));

    public void EliminarPermanente(string rutaRelativa)
    {
        var ruta = RutaFisicaCompleta(rutaRelativa);
        if (File.Exists(ruta))
        {
            File.Delete(ruta);
        }
    }

    /// <summary>Evita path traversal: nos quedamos solo con el nombre de archivo, nunca con segmentos de carpeta.</summary>
    private static string SoloNombreArchivo(string valor) => Path.GetFileName(valor);
}
