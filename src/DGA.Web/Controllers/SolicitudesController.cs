using System.Text.Json;
using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using DGA.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

public class SolicitudesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    SolicitudIdGenerator idGenerator,
    FileStorageService archivos,
    SolicitudExportService exportService,
    ILogger<SolicitudesController> logger) : Controller
{
    private static readonly JsonSerializerOptions JsonOpciones = new() { PropertyNameCaseInsensitive = true };

    // El JS del formulario trabaja siempre en camelCase (numeroItem, componenteNombre, etc.) —
    // hay que serializar con esta misma convención al mandarle datos ya existentes (Editar).
    private static readonly JsonSerializerOptions JsonOpcionesCamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private int UsuarioIdActual => int.Parse(userManager.GetUserId(User)!);
    private bool EsAdmin => User.IsInRole(Roles.Administrador);

    // ------------------------------------------------------------------
    // Listado — "Mis Solicitudes" (siempre las propias, sin importar el rol)
    // ------------------------------------------------------------------

    public async Task<IActionResult> Index(string? busqueda, byte? estado)
    {
        var query = db.Solicitudes
            .Where(s => !s.IsDeleted && s.UsuarioId == UsuarioIdActual);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(s => s.IdSolicitud.Contains(busqueda) || s.NombreResponsable.Contains(busqueda));
        }
        if (estado.HasValue)
        {
            query = query.Where(s => s.EstadoId == estado.Value);
        }

        var solicitudes = await query
            .OrderByDescending(s => s.FechaRegistro)
            .Select(s => new SolicitudListItemViewModel
            {
                Id = s.Id,
                IdSolicitud = s.IdSolicitud,
                Estado = s.Estado.Nombre,
                NombreResponsable = s.NombreResponsable,
                FechaRegistro = s.FechaRegistro,
                CantidadFotografias = s.Items.SelectMany(i => i.Fotografias).Count(),
                EsEditable = Estados.EsEditablePorDueno(s.EstadoId),
                PuedeDescartar = Estados.PuedeDescartar(s.EstadoId),
            })
            .ToListAsync();

        var vm = new SolicitudIndexViewModel
        {
            Solicitudes = solicitudes,
            Busqueda = busqueda,
            EstadoFiltro = estado,
            EstadoOptions = await db.EstadosSolicitud.OrderBy(e => e.Orden)
                .Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
        };
        return View(vm);
    }

    // ------------------------------------------------------------------
    // Crear
    // ------------------------------------------------------------------

    public async Task<IActionResult> Create()
    {
        // El correlativo SOL-##### real se asigna en Guardar(), no acá — NEXT VALUE FOR
        // consume la secuencia siempre que se llama, así que generarlo con solo abrir el
        // formulario deja huecos por cada visita que nunca termina en un guardado. Lo que
        // se muestra en pantalla es una previsualización que no reserva nada.
        var model = new SolicitudFormViewModel
        {
            IdSolicitudPrevisualizado = await idGenerator.PrevisualizarProximoIdAsync(),
        };
        await CargarOpcionesAsync(model);
        return View("Form", model);
    }

    // ------------------------------------------------------------------
    // Editar (solo dueño, y solo mientras EsEditablePorDueno)
    // ------------------------------------------------------------------

    public async Task<IActionResult> Edit(int id)
    {
        var solicitud = await db.Solicitudes
            .Include(s => s.Aduana)
            .Include(s => s.Items).ThenInclude(i => i.Componente)
            .Include(s => s.Items).ThenInclude(i => i.Subcomponente)
            .Include(s => s.Items).ThenInclude(i => i.Elemento)
            .Include(s => s.Items).ThenInclude(i => i.Detalle)
            .Include(s => s.Items).ThenInclude(i => i.Fotografias)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (solicitud is null || solicitud.UsuarioId != UsuarioIdActual)
        {
            return NotFound();
        }
        if (!Estados.EsEditablePorDueno(solicitud.EstadoId))
        {
            TempData["Error"] = "Esta solicitud ya no se puede editar: el administrador cambió su estado.";
            return RedirectToAction(nameof(Index));
        }

        var model = new SolicitudFormViewModel
        {
            Id = solicitud.Id,
            IdSolicitud = solicitud.IdSolicitud,
            NombreResponsable = solicitud.NombreResponsable,
            CargoId = solicitud.CargoId,
            UnidadEjecutoraId = solicitud.UnidadEjecutoraId,
            TipoAduanaId = solicitud.Aduana.TipoAduanaId,
            AduanaId = solicitud.AduanaId,
            JustificacionGeneral = solicitud.JustificacionGeneral,
            ObservacionesGenerales = solicitud.ObservacionesGenerales,
            PuedeDescartar = Estados.PuedeDescartar(solicitud.EstadoId),
        };

        var itemsExistentes = solicitud.Items.OrderBy(i => i.NumeroItem).Select(i => new SolicitudItemFormViewModel
        {
            Id = i.Id,
            NumeroItem = i.NumeroItem,
            ComponenteId = i.ComponenteId,
            ComponenteNombre = i.Componente.Nombre,
            SubcomponenteId = i.SubcomponenteId,
            SubcomponenteNombre = i.Subcomponente.Nombre,
            ElementoId = i.ElementoId,
            ElementoNombre = i.Elemento?.Nombre,
            ElementoLibre = i.ElementoLibre,
            DetalleId = i.DetalleId,
            DetalleNombre = i.Detalle?.Nombre,
            CantidadSolicitada = i.CantidadSolicitada,
            TienePresupuesto = i.TienePresupuesto,
            CostoEstimado = i.CostoEstimado,
            TipoCosto = i.TipoCosto,
            CotizacionRutaExistente = i.CotizacionRuta,
            CotizacionNombreExistente = i.CotizacionNombreOriginal,
            TipoSuscripcion = i.TipoSuscripcion,
            CantidadPeriodos = i.CantidadPeriodos,
            PrioridadId = i.PrioridadId,
            UbicacionEspecifica = i.UbicacionEspecifica,
            JustificacionItem = i.JustificacionItem,
            FotografiasExistentes = i.Fotografias.Select(f => new SolicitudFotoExistenteViewModel
            {
                Id = f.Id,
                Ruta = f.RutaArchivo,
                NombreOriginal = f.NombreOriginal,
            }).ToList(),
        }).ToList();
        model.ItemsExistentesJson = JsonSerializer.Serialize(itemsExistentes, JsonOpcionesCamelCase);

        await CargarOpcionesAsync(model,
            componentesEnUso: solicitud.Items.Select(i => i.ComponenteId),
            prioridadesEnUso: solicitud.Items.Select(i => i.PrioridadId));

        return View("Form", model);
    }

    // ------------------------------------------------------------------
    // Guardar (crea o actualiza según model.Id) — Borrador o Finalizar
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(SolicitudFormViewModel model)
    {
        List<SolicitudItemFormViewModel> items;
        try
        {
            items = JsonSerializer.Deserialize<List<SolicitudItemFormViewModel>>(model.ItemsJson, JsonOpciones) ?? new();
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "No se pudo leer la lista de ítems. Volvé a intentar.");
            items = new();
        }

        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Agregá al menos un ítem antes de guardar.");
        }
        foreach (var item in items)
        {
            if (item.TienePresupuesto)
            {
                if (item.CostoEstimado <= 0)
                {
                    ModelState.AddModelError(string.Empty, $"Ítem {item.NumeroItem}: ingresá el costo estimado.");
                }
                if (item.TipoCosto != "Unitario" && item.TipoCosto != "Total")
                {
                    ModelState.AddModelError(string.Empty, $"Ítem {item.NumeroItem}: el tipo de costo debe ser Unitario o Total.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            if (model.Id == 0)
            {
                model.IdSolicitudPrevisualizado = await idGenerator.PrevisualizarProximoIdAsync();
            }
            await CargarOpcionesAsync(model);
            model.ItemsExistentesJson = model.ItemsJson;
            return View("Form", model);
        }

        var esNueva = model.Id == 0;
        Solicitud solicitud;

        if (esNueva)
        {
            solicitud = new Solicitud
            {
                IdSolicitud = string.IsNullOrEmpty(model.IdSolicitud) ? await idGenerator.NuevoIdAsync() : model.IdSolicitud,
                UsuarioId = UsuarioIdActual,
                EstadoId = Estados.GuardadoBorrador,
                FechaRegistro = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };
            db.Solicitudes.Add(solicitud);
        }
        else
        {
            var existente = await db.Solicitudes.Include(s => s.Items).ThenInclude(i => i.Fotografias)
                .FirstOrDefaultAsync(s => s.Id == model.Id && !s.IsDeleted);
            if (existente is null || existente.UsuarioId != UsuarioIdActual || !Estados.EsEditablePorDueno(existente.EstadoId))
            {
                return NotFound();
            }
            solicitud = existente;
            // El set completo de ítems se re-envía siempre desde el cliente: lo más simple
            // y correcto es reemplazar los ítems existentes en vez de intentar diffearlos.
            db.SolicitudItems.RemoveRange(solicitud.Items);
        }

        solicitud.NombreResponsable = model.NombreResponsable;
        solicitud.CargoId = model.CargoId!.Value;
        solicitud.UnidadEjecutoraId = model.UnidadEjecutoraId!.Value;
        solicitud.AduanaId = model.AduanaId!.Value;
        solicitud.JustificacionGeneral = model.JustificacionGeneral;
        solicitud.ObservacionesGenerales = model.ObservacionesGenerales;
        solicitud.UpdatedAt = DateTime.UtcNow;

        var estadoAnterior = solicitud.EstadoId;
        var finalizando = model.Accion == "finalizar";
        solicitud.EstadoId = finalizando ? Estados.Solicitado : Estados.GuardadoBorrador;
        solicitud.Progreso = Estados.ProgresoParaEstado(solicitud.EstadoId);

        foreach (var item in items)
        {
            var nuevoItem = new SolicitudItem
            {
                NumeroItem = (short)item.NumeroItem,
                ComponenteId = item.ComponenteId,
                SubcomponenteId = item.SubcomponenteId,
                ElementoId = item.ElementoId,
                ElementoLibre = item.ElementoLibre,
                DetalleId = item.DetalleId,
                CantidadSolicitada = item.CantidadSolicitada,
                TienePresupuesto = item.TienePresupuesto,
                CostoEstimado = item.CostoEstimado,
                TipoCosto = item.TipoCosto,
                TipoSuscripcion = item.TipoSuscripcion,
                CantidadPeriodos = item.CantidadPeriodos,
                PrioridadId = item.PrioridadId,
                UbicacionEspecifica = item.UbicacionEspecifica,
                JustificacionItem = item.JustificacionItem,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            if (item.TienePresupuesto && !string.IsNullOrEmpty(item.CotizacionTokenNuevo))
            {
                try
                {
                    nuevoItem.CotizacionRuta = archivos.ConfirmarArchivo(item.CotizacionTokenNuevo, solicitud.IdSolicitud, item.NumeroItem);
                    nuevoItem.CotizacionNombreOriginal = item.CotizacionNombreOriginalNuevo ?? Path.GetFileName(nuevoItem.CotizacionRuta);
                }
                catch (ArchivoInvalidoException ex)
                {
                    logger.LogWarning(ex, "No se pudo confirmar la cotización temporal {Token}", item.CotizacionTokenNuevo);
                }
            }
            else if (item.TienePresupuesto && !string.IsNullOrEmpty(item.CotizacionRutaExistente))
            {
                nuevoItem.CotizacionRuta = item.CotizacionRutaExistente;
                nuevoItem.CotizacionNombreOriginal = item.CotizacionNombreExistente;
            }

            foreach (var existente in item.FotografiasExistentes)
            {
                var rutaFisica = archivos.RutaFisicaCompleta(existente.Ruta);
                if (!System.IO.File.Exists(rutaFisica))
                {
                    logger.LogWarning("Foto existente {Ruta} ya no está en disco; se omite al volver a guardar.", existente.Ruta);
                    continue;
                }
                nuevoItem.Fotografias.Add(new SolicitudItemFotografia
                {
                    RutaArchivo = existente.Ruta,
                    NombreOriginal = string.IsNullOrEmpty(existente.NombreOriginal) ? Path.GetFileName(existente.Ruta) : existente.NombreOriginal,
                    ContentType = "image/*",
                    TamanoBytes = (int)new FileInfo(rutaFisica).Length,
                });
            }

            foreach (var tokenTemp in item.FotografiasNuevas)
            {
                try
                {
                    var rutaFinal = archivos.ConfirmarArchivo(tokenTemp, solicitud.IdSolicitud, item.NumeroItem);
                    nuevoItem.Fotografias.Add(new SolicitudItemFotografia
                    {
                        RutaArchivo = rutaFinal,
                        NombreOriginal = Path.GetFileName(rutaFinal),
                        ContentType = "image/*",
                        TamanoBytes = (int)new FileInfo(archivos.RutaFisicaCompleta(rutaFinal)).Length,
                    });
                }
                catch (ArchivoInvalidoException ex)
                {
                    logger.LogWarning(ex, "No se pudo confirmar la foto temporal {Token}", tokenTemp);
                }
            }

            solicitud.Items.Add(nuevoItem);
        }

        db.SolicitudHistorial.Add(new SolicitudHistorial
        {
            Solicitud = solicitud,
            EstadoAnteriorId = esNueva ? null : estadoAnterior,
            EstadoNuevoId = solicitud.EstadoId,
            UsuarioCambioId = UsuarioIdActual,
            Comentario = finalizando ? "Solicitud finalizada por el usuario" : "Guardado como borrador por el usuario",
            FechaCambio = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        TempData["Mensaje"] = finalizando
            ? $"Solicitud {solicitud.IdSolicitud} enviada correctamente."
            : $"Solicitud {solicitud.IdSolicitud} guardada como borrador.";

        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Detalle
    // ------------------------------------------------------------------

    public async Task<IActionResult> Details(int id)
    {
        var solicitud = await db.Solicitudes
            .Include(s => s.Aduana).ThenInclude(a => a.TipoAduana)
            .Include(s => s.Cargo)
            .Include(s => s.UnidadEjecutora)
            .Include(s => s.Estado)
            .Include(s => s.Items).ThenInclude(i => i.Componente)
            .Include(s => s.Items).ThenInclude(i => i.Subcomponente)
            .Include(s => s.Items).ThenInclude(i => i.Elemento)
            .Include(s => s.Items).ThenInclude(i => i.Detalle)
            .Include(s => s.Items).ThenInclude(i => i.Fotografias)
            .Include(s => s.Historial).ThenInclude(h => h.EstadoAnterior)
            .Include(s => s.Historial).ThenInclude(h => h.EstadoNuevo)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (solicitud is null || (solicitud.UsuarioId != UsuarioIdActual && !EsAdmin))
        {
            return NotFound();
        }

        var vm = new SolicitudDetailViewModel
        {
            Id = solicitud.Id,
            IdSolicitud = solicitud.IdSolicitud,
            Estado = solicitud.Estado.Nombre,
            NombreResponsable = solicitud.NombreResponsable,
            Cargo = solicitud.Cargo?.Nombre,
            UnidadEjecutora = solicitud.UnidadEjecutora?.Nombre,
            Aduana = $"{solicitud.Aduana.Codigo} - {solicitud.Aduana.Nombre}",
            TipoAduana = solicitud.Aduana.TipoAduana.Nombre,
            JustificacionGeneral = solicitud.JustificacionGeneral,
            ObservacionesGenerales = solicitud.ObservacionesGenerales,
            FechaRegistro = solicitud.FechaRegistro,
            EsEditable = solicitud.UsuarioId == UsuarioIdActual && Estados.EsEditablePorDueno(solicitud.EstadoId),
            PuedeDescartar = solicitud.UsuarioId == UsuarioIdActual && Estados.PuedeDescartar(solicitud.EstadoId),
            EsAdmin = EsAdmin,
            Items = solicitud.Items.OrderBy(i => i.NumeroItem).Select(i => new SolicitudDetailItemViewModel
            {
                Id = i.Id,
                NumeroItem = i.NumeroItem,
                Componente = i.Componente.Nombre,
                Subcomponente = i.Subcomponente.Nombre,
                Elemento = i.Elemento?.Nombre ?? i.ElementoLibre,
                Detalle = i.Detalle?.Nombre,
                CantidadSolicitada = i.CantidadSolicitada,
                TienePresupuesto = i.TienePresupuesto,
                CostoEstimado = i.CostoEstimado,
                TipoCosto = i.TipoCosto,
                CotizacionNombreOriginal = i.CotizacionNombreOriginal,
                TipoSuscripcion = i.TipoSuscripcion,
                CantidadPeriodos = i.CantidadPeriodos,
                Prioridad = i.PrioridadId switch { 1 => "Alta", 2 => "Media", _ => "Baja" },
                UbicacionEspecifica = i.UbicacionEspecifica,
                JustificacionItem = i.JustificacionItem,
                Fotografias = i.Fotografias.Select(f => new SolicitudFotoViewModel { Id = f.Id, NombreOriginal = f.NombreOriginal }).ToList(),
            }).ToList(),
            Historial = solicitud.Historial.OrderByDescending(h => h.FechaCambio).Select(h => new SolicitudHistorialItemViewModel
            {
                EstadoAnterior = h.EstadoAnterior?.Nombre,
                EstadoNuevo = h.EstadoNuevo.Nombre,
                Comentario = h.Comentario,
                FechaCambio = h.FechaCambio,
            }).ToList(),
        };

        return View(vm);
    }

    // ------------------------------------------------------------------
    // Exportar (dueño o admin)
    // ------------------------------------------------------------------

    public async Task<IActionResult> DescargarPdf(int id)
    {
        var solicitud = await exportService.CargarParaExportarAsync(id);
        if (solicitud is null || (solicitud.UsuarioId != UsuarioIdActual && !EsAdmin))
        {
            return NotFound();
        }

        var pdf = exportService.GenerarPdf(solicitud);
        return File(pdf, "application/pdf", $"{solicitud.IdSolicitud}.pdf");
    }

    public async Task<IActionResult> DescargarExcel(int id)
    {
        var solicitud = await exportService.CargarParaExportarAsync(id);
        if (solicitud is null || (solicitud.UsuarioId != UsuarioIdActual && !EsAdmin))
        {
            return NotFound();
        }

        var excel = exportService.GenerarExcel(solicitud);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{solicitud.IdSolicitud}.xlsx");
    }

    // ------------------------------------------------------------------
    // Descartar (solo dueño, solo en Borrador)
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Descartar(int id)
    {
        var solicitud = await db.Solicitudes.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (solicitud is null || solicitud.UsuarioId != UsuarioIdActual)
        {
            return NotFound();
        }
        if (!Estados.PuedeDescartar(solicitud.EstadoId))
        {
            TempData["Error"] = "Solo se puede descartar una solicitud mientras está en Borrador.";
            return RedirectToAction(nameof(Index));
        }

        solicitud.IsDeleted = true;
        solicitud.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Mensaje"] = $"Solicitud {solicitud.IdSolicitud} descartada.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Fotografías (subida temporal AJAX, previsualización y descarga autenticadas)
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFotoTemp(IFormFile archivo)
    {
        try
        {
            var token = await archivos.GuardarTemporalAsync(archivo);
            return Json(new { ok = true, token, nombre = archivo.FileName });
        }
        catch (ArchivoInvalidoException ex)
        {
            return BadRequest(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarFotoTemp(string token)
    {
        archivos.EliminarTemporal(token);
        return Json(new { ok = true });
    }

    public IActionResult FotoTemp(string token)
    {
        var ruta = Path.Combine("_temp", Path.GetFileName(token));
        var rutaCompleta = archivos.RutaFisicaCompleta(ruta);
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound();
        }
        return PhysicalFile(rutaCompleta, "application/octet-stream");
    }

    public async Task<IActionResult> Foto(int solicitudItemFotografiaId)
    {
        var foto = await db.SolicitudItemFotografias
            .Include(f => f.SolicitudItem).ThenInclude(i => i.Solicitud)
            .FirstOrDefaultAsync(f => f.Id == solicitudItemFotografiaId);

        if (foto is null)
        {
            return NotFound();
        }
        if (foto.SolicitudItem.Solicitud.UsuarioId != UsuarioIdActual && !EsAdmin)
        {
            return Forbid();
        }

        var rutaCompleta = archivos.RutaFisicaCompleta(foto.RutaArchivo);
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound();
        }
        return PhysicalFile(rutaCompleta, "application/octet-stream");
    }

    // ------------------------------------------------------------------
    // Cotización adjunta (imagen o PDF) — misma lógica de subida temporal que las
    // fotos, pero un solo archivo por ítem y acepta PDF además de imagen.
    // ------------------------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirCotizacionTemp(IFormFile archivo)
    {
        try
        {
            var token = await archivos.GuardarTemporalCotizacionAsync(archivo);
            return Json(new { ok = true, token, nombre = archivo.FileName });
        }
        catch (ArchivoInvalidoException ex)
        {
            return BadRequest(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarCotizacionTemp(string token)
    {
        archivos.EliminarTemporal(token);
        return Json(new { ok = true });
    }

    public IActionResult CotizacionTemp(string token)
    {
        var ruta = Path.Combine("_temp", Path.GetFileName(token));
        var rutaCompleta = archivos.RutaFisicaCompleta(ruta);
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound();
        }
        return PhysicalFile(rutaCompleta, FileStorageService.ContentTypePorExtension(rutaCompleta));
    }

    public async Task<IActionResult> Cotizacion(int solicitudItemId)
    {
        var item = await db.SolicitudItems
            .Include(i => i.Solicitud)
            .FirstOrDefaultAsync(i => i.Id == solicitudItemId);

        if (item is null || string.IsNullOrEmpty(item.CotizacionRuta))
        {
            return NotFound();
        }
        if (item.Solicitud.UsuarioId != UsuarioIdActual && !EsAdmin)
        {
            return Forbid();
        }

        var rutaCompleta = archivos.RutaFisicaCompleta(item.CotizacionRuta);
        if (!System.IO.File.Exists(rutaCompleta))
        {
            return NotFound();
        }
        return PhysicalFile(rutaCompleta, FileStorageService.ContentTypePorExtension(rutaCompleta));
    }

    // ------------------------------------------------------------------

    /// <summary>Llena los combos con las opciones activas. En Edit también hay que pasar los
    /// ids que la solicitud ya tiene guardados (<paramref name="componentesEnUso"/>/<paramref name="prioridadesEnUso"/>)
    /// para que sigan apareciendo aunque un admin las haya desactivado después — si no,
    /// el combo mostraría un valor "fantasma" que no está en la lista.</summary>
    private async Task CargarOpcionesAsync(SolicitudFormViewModel model, IEnumerable<byte>? componentesEnUso = null, IEnumerable<byte>? prioridadesEnUso = null)
    {
        model.CargoOptions = await db.Cargos.Where(c => c.Activo || c.Id == model.CargoId)
            .OrderBy(c => c.Orden).Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync();
        model.UnidadEjecutoraOptions = await db.UnidadesEjecutoras.Where(u => u.Activo || u.Id == model.UnidadEjecutoraId)
            .OrderBy(u => u.Orden).Select(u => new OpcionCatalogo(u.Id, u.Nombre)).ToListAsync();
        model.TipoAduanaOptions = await db.TiposAduana.Where(t => t.Activo || t.Id == model.TipoAduanaId)
            .OrderBy(t => t.Orden).Select(t => new OpcionCatalogo(t.Id, t.Nombre)).ToListAsync();

        var componentesSet = (componentesEnUso ?? []).ToHashSet();
        model.ComponenteOptions = await db.Componentes.Where(c => c.Activo || componentesSet.Contains(c.Id))
            .OrderBy(c => c.Orden).Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync();

        var prioridadesSet = (prioridadesEnUso ?? []).ToHashSet();
        model.PrioridadOptions = await db.Prioridades.Where(p => p.Activo || prioridadesSet.Contains(p.Id))
            .OrderBy(p => p.Orden).Select(p => new OpcionCatalogo(p.Id, p.Nombre)).ToListAsync();
    }
}
