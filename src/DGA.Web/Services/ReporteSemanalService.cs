using ClosedXML.Excel;
using DGA.Web.Data;
using DGA.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DGA.Web.Services;

/// <summary>
/// Genera y guarda el reporte semanal de solicitudes (listado detallado de todas las
/// solicitudes registradas en un rango de fechas). Se dispara automáticamente los domingos
/// (ver ReporteSemanalBackgroundService) o manualmente desde Admin/Reportes.
/// Los archivos viven en disco fuera de wwwroot (App_Data/reportes), igual que las
/// fotografías de ítems — se sirven autenticados vía AdminReportesController.
/// </summary>
public class ReporteSemanalService(ApplicationDbContext db, IWebHostEnvironment env)
{
    private string RutaRaiz => Path.Combine(env.ContentRootPath, "App_Data", "reportes");

    public Task<bool> ExisteReporteAutomaticoAsync(DateOnly inicio, DateOnly fin) =>
        db.ReportesSemanales.AnyAsync(r => r.FechaInicio == inicio && r.FechaFin == fin && !r.GeneradoManualmente);

    public async Task<ReporteSemanal> GenerarAsync(DateOnly inicio, DateOnly fin, bool manual, int? usuarioId)
    {
        var desde = inicio.ToDateTime(TimeOnly.MinValue);
        var hasta = fin.ToDateTime(TimeOnly.MaxValue);

        var solicitudes = await db.Solicitudes
            .Where(s => !s.IsDeleted && s.FechaRegistro >= desde && s.FechaRegistro <= hasta)
            .Include(s => s.Aduana)
            .Include(s => s.Estado)
            .Include(s => s.Items).ThenInclude(i => i.Componente)
            .AsSplitQuery()
            .OrderBy(s => s.FechaRegistro)
            .ToListAsync();

        var pdfBytes = GenerarPdf(inicio, fin, solicitudes);
        var excelBytes = GenerarExcel(inicio, fin, solicitudes);

        var nombreCarpeta = $"{inicio:yyyy-MM-dd}_{fin:yyyy-MM-dd}";
        var carpeta = Path.Combine(RutaRaiz, nombreCarpeta);
        Directory.CreateDirectory(carpeta);

        var sufijo = manual ? $"-manual-{DateTime.UtcNow:HHmmss}" : string.Empty;
        var nombrePdf = $"reporte{sufijo}.pdf";
        var nombreExcel = $"reporte{sufijo}.xlsx";
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombrePdf), pdfBytes);
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombreExcel), excelBytes);

        var reporte = new ReporteSemanal
        {
            FechaInicio = inicio,
            FechaFin = fin,
            CantidadSolicitudes = solicitudes.Count,
            RutaPdf = $"{nombreCarpeta}/{nombrePdf}",
            RutaExcel = $"{nombreCarpeta}/{nombreExcel}",
            GeneradoManualmente = manual,
            GeneradoPorUsuarioId = usuarioId,
            GeneradoEn = DateTime.UtcNow,
        };
        db.ReportesSemanales.Add(reporte);
        await db.SaveChangesAsync();
        return reporte;
    }

    public string RutaFisicaCompleta(string rutaRelativa) =>
        Path.Combine(RutaRaiz, rutaRelativa.Replace('/', Path.DirectorySeparatorChar));

    private static byte[] GenerarPdf(DateOnly inicio, DateOnly fin, List<Solicitud> solicitudes)
    {
        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(32);
                pagina.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(Fonts.Calibri));

                pagina.Header().Column(col =>
                {
                    col.Item().Row(fila =>
                    {
                        fila.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Global Customs Solutions").FontSize(14).Bold();
                            c.Item().Text("Reporte Semanal de Solicitudes").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        fila.ConstantItem(220).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"{inicio:dd/MM/yyyy} — {fin:dd/MM/yyyy}").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().AlignRight().Text($"{solicitudes.Count} solicitud(es)").FontSize(10);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                pagina.Content().PaddingTop(14).Column(col =>
                {
                    if (solicitudes.Count == 0)
                    {
                        col.Item().Text("No se registraron solicitudes en este período.").FontColor(Colors.Grey.Darken1);
                        return;
                    }

                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.ConstantColumn(70);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.6f);
                            c.ConstantColumn(40);
                        });

                        void Encabezado(string texto) => tabla.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(texto).SemiBold().FontSize(8.5f);
                        Encabezado("ID Solicitud"); Encabezado("Fecha Registro"); Encabezado("Responsable");
                        Encabezado("Aduana"); Encabezado("Componente"); Encabezado("Estado"); Encabezado("Ítems");

                        foreach (var s in solicitudes)
                        {
                            var componentePrincipal = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Componente.Nombre).FirstOrDefault() ?? "-";
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(s.IdSolicitud);
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(s.FechaRegistro.ToString("dd/MM/yyyy"));
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(s.NombreResponsable).FontSize(8.5f);
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text($"{s.Aduana.Codigo} - {s.Aduana.Nombre}").FontSize(8.5f);
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(componentePrincipal).FontSize(8.5f);
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(s.Estado.Nombre).FontSize(8.5f);
                            tabla.Cell().Padding(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Text(s.Items.Count.ToString());
                        }
                    });
                });

                pagina.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generado el ").FontSize(8);
                    t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                    t.Span(" — Página ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" de ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static byte[] GenerarExcel(DateOnly inicio, DateOnly fin, List<Solicitud> solicitudes)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Solicitudes");

        hoja.Cell(1, 1).Value = $"Reporte Semanal de Solicitudes — {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}";
        hoja.Cell(1, 1).Style.Font.SetBold();
        hoja.Cell(1, 1).Style.Font.FontSize = 13;
        hoja.Range(1, 1, 1, 7).Merge();

        string[] encabezados = ["ID Solicitud", "Fecha Registro", "Responsable", "Aduana", "Componente", "Estado", "Cantidad de Ítems"];
        for (var c = 0; c < encabezados.Length; c++)
        {
            hoja.Cell(3, c + 1).Value = encabezados[c];
            hoja.Cell(3, c + 1).Style.Font.SetBold();
            hoja.Cell(3, c + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF4FF"));
        }

        var fila = 4;
        foreach (var s in solicitudes)
        {
            var componentePrincipal = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Componente.Nombre).FirstOrDefault() ?? "-";
            hoja.Cell(fila, 1).Value = s.IdSolicitud;
            hoja.Cell(fila, 2).Value = s.FechaRegistro.ToString("dd/MM/yyyy HH:mm");
            hoja.Cell(fila, 3).Value = s.NombreResponsable;
            hoja.Cell(fila, 4).Value = $"{s.Aduana.Codigo} - {s.Aduana.Nombre}";
            hoja.Cell(fila, 5).Value = componentePrincipal;
            hoja.Cell(fila, 6).Value = s.Estado.Nombre;
            hoja.Cell(fila, 7).Value = s.Items.Count;
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
