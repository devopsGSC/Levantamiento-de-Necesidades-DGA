using ClosedXML.Excel;

namespace DGA.Web.Services;

public record FilaCargaMasivaUsuario(
    int Fila,
    string Nombre,
    string? Cargo,
    string Email,
    string? Aduana,
    string? Subdireccion,
    string? Departamento);

/// <summary>Genera y lee la plantilla Excel para la carga masiva de usuarios (panel de Administración).</summary>
public class CargaMasivaUsuariosService
{
    private const string HojaUsuarios = "Usuarios";

    public byte[] GenerarPlantilla()
    {
        using var libro = new XLWorkbook();

        var hoja = libro.Worksheets.Add(HojaUsuarios);
        string[] encabezados = ["Nombre Completo", "Cargo", "Correo Institucional", "Aduana", "Subdirección", "Departamento/Unidad"];
        for (var c = 0; c < encabezados.Length; c++)
        {
            hoja.Cell(1, c + 1).Value = encabezados[c];
            hoja.Cell(1, c + 1).Style.Font.SetBold();
            hoja.Cell(1, c + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF4FF"));
        }
        hoja.SheetView.FreezeRows(1);

        hoja.Column(1).Width = 30;
        hoja.Column(2).Width = 24;
        hoja.Column(3).Width = 34;
        hoja.Column(4).Width = 24;
        hoja.Column(5).Width = 24;
        hoja.Column(6).Width = 24;

        var instrucciones = libro.Worksheets.Add("Instrucciones");
        string[] lineas =
        [
            "Cómo completar esta plantilla",
            "",
            "1. Completá una fila por cada usuario a crear en la hoja \"Usuarios\".",
            "2. \"Nombre Completo\" y \"Correo Institucional\" son obligatorios.",
            "3. \"Cargo\", \"Aduana\", \"Subdirección\" y \"Departamento/Unidad\" son opcionales.",
            "4. No modifiques los encabezados de la hoja \"Usuarios\".",
            "5. Todos los usuarios cargados por este medio se crean con rol \"Usuario\".",
            "   Si alguno debe ser Administrador, cambiá el rol después desde el Directorio de Usuarios.",
            "6. Guardá el archivo y subilo con el botón \"Subir Archivo\" del panel de Administración.",
            "7. Cada usuario creado recibirá un correo automático con su contraseña temporal.",
        ];
        for (var i = 0; i < lineas.Length; i++)
        {
            instrucciones.Cell(i + 1, 1).Value = lineas[i];
        }
        instrucciones.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(13);
        instrucciones.Column(1).Width = 90;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    public List<FilaCargaMasivaUsuario> LeerFilas(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var hoja = libro.Worksheets.Contains(HojaUsuarios) ? libro.Worksheet(HojaUsuarios) : libro.Worksheet(1);

        var filas = new List<FilaCargaMasivaUsuario>();
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
        for (var f = 2; f <= ultimaFila; f++)
        {
            var nombre = hoja.Cell(f, 1).GetString().Trim();
            var cargo = hoja.Cell(f, 2).GetString().Trim();
            var email = hoja.Cell(f, 3).GetString().Trim();
            var aduana = hoja.Cell(f, 4).GetString().Trim();
            var subdireccion = hoja.Cell(f, 5).GetString().Trim();
            var departamento = hoja.Cell(f, 6).GetString().Trim();

            if (nombre.Length == 0 && email.Length == 0)
            {
                continue;
            }

            filas.Add(new FilaCargaMasivaUsuario(
                f,
                nombre,
                cargo.Length == 0 ? null : cargo,
                email,
                aduana.Length == 0 ? null : aduana,
                subdireccion.Length == 0 ? null : subdireccion,
                departamento.Length == 0 ? null : departamento));
        }

        return filas;
    }
}
