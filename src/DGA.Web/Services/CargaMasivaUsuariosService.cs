using ClosedXML.Excel;

namespace DGA.Web.Services;

public record FilaCargaMasivaUsuario(int Fila, string Nombre, string Email, string Rol, string? Departamento);

/// <summary>Genera y lee la plantilla Excel para la carga masiva de usuarios (panel de Administración).</summary>
public class CargaMasivaUsuariosService
{
    private const string HojaUsuarios = "Usuarios";

    public byte[] GenerarPlantilla()
    {
        using var libro = new XLWorkbook();

        var hoja = libro.Worksheets.Add(HojaUsuarios);
        string[] encabezados = ["Nombre Completo", "Correo Institucional", "Rol", "Departamento"];
        for (var c = 0; c < encabezados.Length; c++)
        {
            hoja.Cell(1, c + 1).Value = encabezados[c];
            hoja.Cell(1, c + 1).Style.Font.SetBold();
            hoja.Cell(1, c + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF4FF"));
        }
        hoja.SheetView.FreezeRows(1);

        hoja.Range("C2:C500").CreateDataValidation().List("Usuario,Administrador");

        hoja.Column(1).Width = 30;
        hoja.Column(2).Width = 34;
        hoja.Column(3).Width = 18;
        hoja.Column(4).Width = 24;

        var instrucciones = libro.Worksheets.Add("Instrucciones");
        string[] lineas =
        [
            "Cómo completar esta plantilla",
            "",
            "1. Completá una fila por cada usuario a crear en la hoja \"Usuarios\".",
            "2. \"Nombre Completo\" y \"Correo Institucional\" son obligatorios.",
            "3. \"Rol\" debe ser \"Usuario\" o \"Administrador\" (la columna tiene una lista desplegable).",
            "4. \"Departamento\" es opcional.",
            "5. No modifiques los encabezados de la hoja \"Usuarios\".",
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
            var email = hoja.Cell(f, 2).GetString().Trim();
            var rol = hoja.Cell(f, 3).GetString().Trim();
            var departamento = hoja.Cell(f, 4).GetString().Trim();

            if (nombre.Length == 0 && email.Length == 0)
            {
                continue;
            }

            filas.Add(new FilaCargaMasivaUsuario(f, nombre, email, rol, departamento.Length == 0 ? null : departamento));
        }

        return filas;
    }
}
