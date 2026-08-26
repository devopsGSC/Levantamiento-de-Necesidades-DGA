using DGA.Web.Data;
using DGA.Web.Data.Entities;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DGA.Web.Services;

public class SolicitudExportService(ApplicationDbContext db, FileStorageService archivos)
{
    private static string FormatoMoneda(decimal monto) => monto.ToString("$#,##0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static decimal Subtotal(SolicitudItem item) => !item.TienePresupuesto
        ? 0
        : item.CostoEstimado * (item.TipoCosto == "Total" ? 1 : item.CantidadSolicitada) * (item.CantidadPeriodos ?? 1);

    /// <summary>Texto de la columna Detalle — para ítems de suscripción (Internet,
    /// Telefonía) muestra el Tipo/Cantidad de Períodos en vez de "-" cuando el ítem no
    /// tiene un Detalle propio del catálogo (la suscripción es el Elemento mismo).</summary>
    private static string DetalleTexto(SolicitudItem item)
    {
        if (item.TipoSuscripcion is null)
        {
            return item.Detalle?.Nombre ?? "-";
        }
        var suscripcion = $"{item.TipoSuscripcion} × {item.CantidadPeriodos}";
        return item.Detalle is null ? suscripcion : $"{item.Detalle.Nombre} ({suscripcion})";
    }

    public async Task<Solicitud?> CargarParaExportarAsync(int id)
    {
        return await db.Solicitudes
            .Include(s => s.Usuario)
            .Include(s => s.Aduana).ThenInclude(a => a.TipoAduana)
            .Include(s => s.Cargo)
            .Include(s => s.UnidadEjecutora)
            .Include(s => s.Estado)
            .Include(s => s.Items).ThenInclude(i => i.Componente)
            .Include(s => s.Items).ThenInclude(i => i.Subcomponente)
            .Include(s => s.Items).ThenInclude(i => i.Elemento)
            .Include(s => s.Items).ThenInclude(i => i.Detalle)
            .Include(s => s.Items).ThenInclude(i => i.Prioridad)
            .Include(s => s.Items).ThenInclude(i => i.Fotografias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
    }

    public byte[] GenerarPdf(Solicitud s)
    {
        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(32);
                pagina.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily(Fonts.Calibri));

                pagina.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Column(titulo =>
                    {
                        titulo.Item().AlignCenter().Text("ORDEN DE SOLICITUD FORMAL").FontSize(18).Bold();
                    });

                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Row(fila =>
                    {
                        fila.RelativeItem().Text(t => { t.Span("NO. DE SOLICITUD: ").Bold(); t.Span(s.IdSolicitud); });
                        fila.RelativeItem().AlignRight().Text(t => { t.Span("FECHA DE EMISIÓN: ").Bold(); t.Span(s.FechaRegistro.ASalvador().ToString("dd/MM/yyyy HH:mm:ss")); });
                    });
                    col.Item().LineHorizontal(2).LineColor(Colors.Grey.Darken2);

                    col.Item().Column(seccion =>
                    {
                        seccion.Item().Text("1. INFORMACIÓN GENERAL").Bold().FontSize(11).FontColor(Colors.BlueGrey.Darken3);
                        seccion.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        seccion.Item().PaddingTop(8).Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(2.7f);
                                c.RelativeColumn(1.3f);
                                c.RelativeColumn(2.7f);
                            });

                            static IContainer Celda(IContainer c) => c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(7);

                            Celda(tabla.Cell()).Text("Responsable:").SemiBold();
                            Celda(tabla.Cell()).Text(s.NombreResponsable);
                            Celda(tabla.Cell()).Text("Cargo:").SemiBold();
                            Celda(tabla.Cell()).Text(s.Cargo?.Nombre ?? "-");

                            Celda(tabla.Cell()).Text("Unidad Ejecutora:").SemiBold();
                            Celda(tabla.Cell().ColumnSpan(3)).Text(s.UnidadEjecutora?.Nombre ?? "-");

                            Celda(tabla.Cell()).Text("Aduana:").SemiBold();
                            Celda(tabla.Cell().ColumnSpan(3)).Text($"{s.Aduana.TipoAduana.Nombre} - {s.Aduana.Codigo} - {s.Aduana.Nombre}");
                        });
                    });

                    var itemsOrdenados = s.Items.OrderBy(i => i.NumeroItem).ToList();
                    col.Item().Column(seccion =>
                    {
                        seccion.Item().Text($"2. ÍTEMS SOLICITADOS (TOTAL: {itemsOrdenados.Count})").Bold().FontSize(11).FontColor(Colors.BlueGrey.Darken3);
                        seccion.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        seccion.Item().PaddingTop(8).Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(20);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(1.4f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.3f);
                                c.ConstantColumn(24);
                                c.ConstantColumn(58);
                                c.ConstantColumn(55);
                                c.ConstantColumn(30);
                            });

                            void Encabezado(string texto) =>
                                tabla.Cell().Background(Colors.BlueGrey.Lighten5).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                    .Padding(6).AlignCenter().Text(texto).SemiBold().FontSize(8.5f);

                            Encabezado("N°"); Encabezado("Componente"); Encabezado("Subcomponente");
                            Encabezado("Elemento"); Encabezado("Detalle"); Encabezado("Cant.");
                            Encabezado("Costo Est."); Encabezado("Subtotal"); Encabezado("Cotiz.");

                            foreach (var item in itemsOrdenados)
                            {
                                static IContainer CeldaItem(IContainer c) => c.Padding(6).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

                                CeldaItem(tabla.Cell()).AlignCenter().Text(item.NumeroItem.ToString());
                                CeldaItem(tabla.Cell()).Text(item.Componente.Nombre).FontSize(8.5f);
                                CeldaItem(tabla.Cell()).Text(item.Subcomponente.Nombre).FontSize(8.5f);
                                CeldaItem(tabla.Cell()).Text(item.Elemento?.Nombre ?? item.ElementoLibre ?? "-").FontSize(8.5f);
                                CeldaItem(tabla.Cell()).Text(DetalleTexto(item)).FontSize(8.5f);
                                CeldaItem(tabla.Cell()).AlignCenter().Text(item.CantidadSolicitada.ToString()).SemiBold();
                                CeldaItem(tabla.Cell()).AlignRight().Text(item.TienePresupuesto ? $"{FormatoMoneda(item.CostoEstimado)} ({(item.TipoCosto == "Total" ? "Tot." : "Unit.")})" : "Sin presupuesto").FontSize(7.5f);
                                CeldaItem(tabla.Cell()).AlignRight().Text(FormatoMoneda(Subtotal(item))).FontSize(8f).SemiBold();
                                CeldaItem(tabla.Cell()).AlignCenter().Text(item.CotizacionRuta is null ? "-" : "Sí").FontSize(8.5f);
                            }

                            tabla.Cell().ColumnSpan(5).Background(Colors.BlueGrey.Lighten5).Padding(6).AlignRight().Text("TOTAL DE CANTIDADES:").Bold().FontSize(8.5f);
                            tabla.Cell().Background(Colors.BlueGrey.Lighten5).Padding(6).AlignCenter().Text(itemsOrdenados.Sum(i => i.CantidadSolicitada).ToString()).Bold();
                            tabla.Cell().ColumnSpan(3).Background(Colors.BlueGrey.Lighten5);

                            tabla.Cell().ColumnSpan(7).Background(Colors.BlueGrey.Lighten5).Padding(6).AlignRight().Text("MONTO PRESUPUESTADO TOTAL:").Bold().FontSize(8.5f);
                            tabla.Cell().ColumnSpan(2).Background(Colors.BlueGrey.Lighten5).Padding(6).AlignRight().Text(FormatoMoneda(itemsOrdenados.Sum(Subtotal))).Bold().FontSize(8.5f);
                        });
                    });

                    var itemsConFotos = itemsOrdenados.Where(i => i.Fotografias.Count > 0).ToList();
                    if (itemsConFotos.Count > 0)
                    {
                        col.Item().Column(seccion =>
                        {
                            seccion.Item().Text("3. GALERÍA DE IMÁGENES ADJUNTAS").Bold().FontSize(11).FontColor(Colors.BlueGrey.Darken3);
                            seccion.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            foreach (var item in itemsConFotos)
                            {
                                var etiqueta = item.Elemento?.Nombre ?? item.ElementoLibre ?? "-";
                                seccion.Item().PaddingTop(10).Text($"Imágenes Ítem {item.NumeroItem}: {etiqueta}").SemiBold().FontSize(9.5f).FontColor(Colors.BlueGrey.Darken3);

                                foreach (var fotosFila in item.Fotografias.Chunk(3))
                                {
                                    seccion.Item().PaddingTop(6).Row(fila =>
                                    {
                                        foreach (var foto in fotosFila)
                                        {
                                            var ruta = archivos.RutaFisicaCompleta(foto.RutaArchivo);
                                            if (File.Exists(ruta))
                                            {
                                                fila.ConstantItem(170).PaddingRight(8).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(2).Image(ruta).FitWidth();
                                            }
                                        }
                                    });
                                }
                            }
                        });
                    }

                    col.Item().Column(seccion =>
                    {
                        seccion.Item().Text("4. JUSTIFICACIÓN TÉCNICA Y DESCRIPCIÓN").Bold().FontSize(11).FontColor(Colors.BlueGrey.Darken3);
                        seccion.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        seccion.Item().PaddingTop(8).Background(Colors.Grey.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(jc =>
                        {
                            jc.Item().Text(string.IsNullOrWhiteSpace(s.JustificacionGeneral) ? "Sin justificación detallada." : s.JustificacionGeneral);
                            if (!string.IsNullOrWhiteSpace(s.ObservacionesGenerales))
                            {
                                jc.Item().PaddingTop(6).Text(t => { t.Span("Observaciones: ").SemiBold(); t.Span(s.ObservacionesGenerales); });
                            }
                        });
                    });

                    col.Item().PaddingTop(20).Row(fila =>
                    {
                        void Firma(string? etiqueta = null, string? nombre = null)
                        {
                            fila.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                                if (etiqueta is not null)
                                    c.Item().AlignCenter().Text(etiqueta).SemiBold().FontSize(9);
                                if (nombre is not null)
                                    c.Item().AlignCenter().Text(nombre).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        Firma("Firma del Solicitante", s.NombreResponsable);
                        fila.ConstantItem(16);
                        Firma("Visto Bueno");
                        fila.ConstantItem(16);
                        Firma("Autorización");
                    });

                    col.Item().PaddingTop(14).AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Darken1).Italic());
                        t.Span("Documento generado por el Sistema de Levantamiento de Necesidades | Solicitud: ");
                        t.Span(s.IdSolicitud);
                        t.Span(" | Fecha de Impresión: ");
                        t.Span(DateTime.UtcNow.ASalvador().ToString("dd/MM/yyyy HH:mm:ss"));
                    });
                });
            });
        });

        return documento.GeneratePdf();
    }

    public byte[] GenerarExcel(Solicitud s)
    {
        using var libro = new XLWorkbook();

        var hojaGeneral = libro.Worksheets.Add("Solicitud");
        var filas = new (string, string)[]
        {
            ("ID Solicitud", s.IdSolicitud),
            ("Estado", s.Estado.Nombre),
            ("Responsable", s.NombreResponsable),
            ("Cargo", s.Cargo?.Nombre ?? "-"),
            ("Unidad Ejecutora", s.UnidadEjecutora?.Nombre ?? "-"),
            ("Tipo de Aduana", s.Aduana.TipoAduana.Nombre),
            ("Aduana", $"{s.Aduana.Codigo} - {s.Aduana.Nombre}"),
            ("Fecha de Registro", s.FechaRegistro.ASalvador().ToString("dd/MM/yyyy HH:mm")),
            ("Justificación General", s.JustificacionGeneral),
            ("Observaciones Generales", s.ObservacionesGenerales ?? "-"),
            ("Monto Presupuestado Total", FormatoMoneda(s.Items.Sum(Subtotal))),
        };
        for (var i = 0; i < filas.Length; i++)
        {
            hojaGeneral.Cell(i + 1, 1).Value = filas[i].Item1;
            hojaGeneral.Cell(i + 1, 1).Style.Font.SetBold();
            hojaGeneral.Cell(i + 1, 2).Value = filas[i].Item2;
        }
        hojaGeneral.Column(1).Width = 24;
        hojaGeneral.Column(2).Width = 70;
        hojaGeneral.Columns().Style.Alignment.WrapText = true;

        var hojaItems = libro.Worksheets.Add("Ítems");
        string[] encabezados = ["N°", "Componente", "Subcomponente", "Elemento", "Detalle", "Cantidad", "Costo Estimado", "Tipo de Costo", "Subtotal", "Cotización Adjunta", "Prioridad", "Ubicación Específica", "Justificación del Ítem", "Fotografías"];
        for (var c = 0; c < encabezados.Length; c++)
        {
            hojaItems.Cell(1, c + 1).Value = encabezados[c];
            hojaItems.Cell(1, c + 1).Style.Font.SetBold();
            hojaItems.Cell(1, c + 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EEF4FF"));
        }
        var fila2 = 2;
        foreach (var item in s.Items.OrderBy(i => i.NumeroItem))
        {
            hojaItems.Cell(fila2, 1).Value = item.NumeroItem;
            hojaItems.Cell(fila2, 2).Value = item.Componente.Nombre;
            hojaItems.Cell(fila2, 3).Value = item.Subcomponente.Nombre;
            hojaItems.Cell(fila2, 4).Value = item.Elemento?.Nombre ?? item.ElementoLibre ?? "-";
            hojaItems.Cell(fila2, 5).Value = DetalleTexto(item);
            hojaItems.Cell(fila2, 6).Value = item.CantidadSolicitada;
            if (item.TienePresupuesto)
            {
                hojaItems.Cell(fila2, 7).Value = item.CostoEstimado;
                hojaItems.Cell(fila2, 7).Style.NumberFormat.Format = "$#,##0.00";
                hojaItems.Cell(fila2, 8).Value = item.TipoCosto;
            }
            else
            {
                hojaItems.Cell(fila2, 7).Value = "Sin presupuesto";
                hojaItems.Cell(fila2, 8).Value = "-";
            }
            hojaItems.Cell(fila2, 9).Value = Subtotal(item);
            hojaItems.Cell(fila2, 9).Style.NumberFormat.Format = "$#,##0.00";
            hojaItems.Cell(fila2, 10).Value = item.CotizacionNombreOriginal ?? "-";
            hojaItems.Cell(fila2, 11).Value = item.Prioridad.Nombre;
            hojaItems.Cell(fila2, 12).Value = item.UbicacionEspecifica ?? "-";
            hojaItems.Cell(fila2, 13).Value = item.JustificacionItem ?? "-";
            hojaItems.Cell(fila2, 14).Value = item.Fotografias.Count;
            fila2++;
        }

        hojaItems.Cell(fila2, 8).Value = "TOTAL PRESUPUESTADO:";
        hojaItems.Cell(fila2, 8).Style.Font.SetBold();
        hojaItems.Cell(fila2, 9).Value = s.Items.Sum(Subtotal);
        hojaItems.Cell(fila2, 9).Style.NumberFormat.Format = "$#,##0.00";
        hojaItems.Cell(fila2, 9).Style.Font.SetBold();

        hojaItems.Columns().AdjustToContents();
        hojaItems.Column(13).Width = 50;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
