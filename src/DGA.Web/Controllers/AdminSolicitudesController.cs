using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using DGA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
[Route("Admin/Solicitudes")]
public class AdminSolicitudesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    SolicitudExportService exportService) : Controller
{
    private const int PorPagina = 10;

    private int UsuarioIdActual => int.Parse(userManager.GetUserId(User)!);

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? busqueda, byte? estado, byte? tipoAduanaId, int? aduanaId,
        byte? componenteId, int? subcomponenteId, int? elementoId,
        byte? prioridadId, byte? unidadEjecutoraId,
        DateTime? fechaDesde, DateTime? fechaHasta, int pagina = 1)
    {
        var query = db.Solicitudes.Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(s => s.IdSolicitud.Contains(busqueda) || s.NombreResponsable.Contains(busqueda));
        }
        if (estado.HasValue)
        {
            query = query.Where(s => s.EstadoId == estado.Value);
        }
        if (tipoAduanaId.HasValue)
        {
            query = query.Where(s => s.Aduana.TipoAduanaId == tipoAduanaId.Value);
        }
        if (aduanaId.HasValue)
        {
            query = query.Where(s => s.AduanaId == aduanaId.Value);
        }
        if (unidadEjecutoraId.HasValue)
        {
            query = query.Where(s => s.UnidadEjecutoraId == unidadEjecutoraId.Value);
        }
        if (componenteId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.ComponenteId == componenteId.Value));
        }
        if (subcomponenteId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.SubcomponenteId == subcomponenteId.Value));
        }
        if (elementoId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.ElementoId == elementoId.Value));
        }
        if (prioridadId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.PrioridadId == prioridadId.Value));
        }
        if (fechaDesde.HasValue)
        {
            query = query.Where(s => s.FechaRegistro >= fechaDesde.Value.Date);
        }
        if (fechaHasta.HasValue)
        {
            var hastaExclusive = fechaHasta.Value.Date.AddDays(1);
            query = query.Where(s => s.FechaRegistro < hastaExclusive);
        }

        var total = await query.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var solicitudes = await query
            .OrderByDescending(s => s.FechaRegistro)
            .Skip((pagina - 1) * PorPagina)
            .Take(PorPagina)
            .Select(s => new AdminSolicitudListItemViewModel
            {
                Id = s.Id,
                IdSolicitud = s.IdSolicitud,
                Estado = s.Estado.Nombre,
                Aduana = s.Aduana.Codigo + " - " + s.Aduana.Nombre,
                FechaRegistro = s.FechaRegistro,
                Componente = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Componente.Nombre).FirstOrDefault() ?? "-",
                Elemento = s.Items.OrderBy(i => i.NumeroItem)
                    .Select(i => i.Elemento != null ? i.Elemento.Nombre : i.ElementoLibre)
                    .FirstOrDefault() ?? "-",
                Detalle = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Detalle != null ? i.Detalle.Nombre : null).FirstOrDefault() ?? "-",
            })
            .ToListAsync();

        var vm = new AdminSolicitudIndexViewModel
        {
            Solicitudes = solicitudes,
            Busqueda = busqueda,
            EstadoFiltro = estado,
            TipoAduanaFiltro = tipoAduanaId,
            AduanaFiltro = aduanaId,
            ComponenteFiltro = componenteId,
            SubcomponenteFiltro = subcomponenteId,
            ElementoFiltro = elementoId,
            PrioridadFiltro = prioridadId,
            UnidadEjecutoraFiltro = unidadEjecutoraId,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalResultados = total,
            EstadoOptions = await db.EstadosSolicitud.OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
            TipoAduanaOptions = await db.TiposAduana.OrderBy(t => t.Orden).Select(t => new OpcionCatalogo(t.Id, t.Nombre)).ToListAsync(),
            AduanaOptions = await db.Aduanas
                .Where(a => !tipoAduanaId.HasValue || a.TipoAduanaId == tipoAduanaId.Value)
                .OrderBy(a => a.Orden).Select(a => new OpcionCatalogo(a.Id, a.Codigo + " - " + a.Nombre)).ToListAsync(),
            ComponenteOptions = await db.Componentes.OrderBy(c => c.Orden).Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync(),
            SubcomponenteOptions = componenteId.HasValue
                ? await db.Subcomponentes.Where(s => s.ComponenteId == componenteId.Value).OrderBy(s => s.Orden).Select(s => new OpcionCatalogo(s.Id, s.Nombre)).ToListAsync()
                : new(),
            ElementoOptions = subcomponenteId.HasValue
                ? await db.Elementos.Where(e => e.SubcomponenteId == subcomponenteId.Value).OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync()
                : new(),
            PrioridadOptions = await db.Prioridades.OrderBy(p => p.Orden).Select(p => new OpcionCatalogo(p.Id, p.Nombre)).ToListAsync(),
            UnidadEjecutoraOptions = await db.UnidadesEjecutoras.OrderBy(u => u.Orden).Select(u => new OpcionCatalogo(u.Id, u.Nombre)).ToListAsync(),
        };
        return View(vm);
    }

    [HttpGet("{id:int}")]
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

        if (solicitud is null)
        {
            return NotFound();
        }

        var vm = new AdminSolicitudDetailViewModel
        {
            Id = solicitud.Id,
            IdSolicitud = solicitud.IdSolicitud,
            EstadoActualId = solicitud.EstadoId,
            Estado = solicitud.Estado.Nombre,
            NombreResponsable = solicitud.NombreResponsable,
            Cargo = solicitud.Cargo?.Nombre,
            UnidadEjecutora = solicitud.UnidadEjecutora?.Nombre,
            UnidadEjecutoraId = solicitud.UnidadEjecutoraId,
            Aduana = $"{solicitud.Aduana.Codigo} - {solicitud.Aduana.Nombre}",
            TipoAduana = solicitud.Aduana.TipoAduana.Nombre,
            JustificacionGeneral = solicitud.JustificacionGeneral,
            ObservacionesGenerales = solicitud.ObservacionesGenerales,
            FechaRegistro = solicitud.FechaRegistro,
            FechaRevision = solicitud.FechaRevision,
            Progreso = solicitud.Progreso,
            EstadoOptions = await db.EstadosSolicitud
                .Where(e => e.Id == solicitud.EstadoId || (e.Id == Estados.Aprobado || e.Id == Estados.Denegado || e.Id == Estados.EnProceso || e.Id == Estados.Finalizado))
                .OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
            UnidadEjecutoraOptions = await db.UnidadesEjecutoras.Where(u => u.Activo || u.Id == solicitud.UnidadEjecutoraId)
                .OrderBy(u => u.Orden).Select(u => new OpcionCatalogo(u.Id, u.Nombre)).ToListAsync(),
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

    [HttpPost("CambiarEstado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(CambiarEstadoViewModel model)
    {
        var solicitud = await db.Solicitudes.FirstOrDefaultAsync(s => s.Id == model.SolicitudId && !s.IsDeleted);
        if (solicitud is null)
        {
            return NotFound();
        }

        if (!Estados.PuedeEstablecerAdmin(model.NuevoEstadoId))
        {
            TempData["Error"] = "Ese estado no se puede asignar manualmente.";
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }
        if (model.NuevoEstadoId == solicitud.EstadoId)
        {
            TempData["Error"] = "La solicitud ya está en ese estado.";
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        var unidadEjecutoraFinal = model.UnidadEjecutoraId ?? solicitud.UnidadEjecutoraId;
        if (Estados.RequiereUnidadEjecutora(model.NuevoEstadoId) && !unidadEjecutoraFinal.HasValue)
        {
            TempData["Error"] = "Indicá la Unidad Ejecutora antes de aprobar la solicitud.";
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        var estadoAnterior = solicitud.EstadoId;
        solicitud.EstadoId = model.NuevoEstadoId;
        solicitud.Progreso = Estados.ProgresoParaEstado(model.NuevoEstadoId);
        solicitud.UnidadEjecutoraId = unidadEjecutoraFinal;
        solicitud.AdminRevisorId = UsuarioIdActual;
        solicitud.FechaRevision = DateTime.UtcNow;
        solicitud.UpdatedAt = DateTime.UtcNow;

        db.SolicitudHistorial.Add(new SolicitudHistorial
        {
            SolicitudId = solicitud.Id,
            EstadoAnteriorId = estadoAnterior,
            EstadoNuevoId = model.NuevoEstadoId,
            UsuarioCambioId = UsuarioIdActual,
            Comentario = model.Comentario,
            FechaCambio = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Estado de {solicitud.IdSolicitud} actualizado.";
        return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
    }

    [HttpGet("{id:int}/Pdf")]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var solicitud = await exportService.CargarParaExportarAsync(id);
        if (solicitud is null)
        {
            return NotFound();
        }

        var pdf = exportService.GenerarPdf(solicitud);
        return File(pdf, "application/pdf", $"{solicitud.IdSolicitud}.pdf");
    }

    [HttpGet("{id:int}/Excel")]
    public async Task<IActionResult> DescargarExcel(int id)
    {
        var solicitud = await exportService.CargarParaExportarAsync(id);
        if (solicitud is null)
        {
            return NotFound();
        }

        var excel = exportService.GenerarExcel(solicitud);
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{solicitud.IdSolicitud}.xlsx");
    }

    [HttpPost("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var solicitud = await db.Solicitudes.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (solicitud is null)
        {
            return NotFound();
        }

        solicitud.IsDeleted = true;
        solicitud.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Mensaje"] = $"Solicitud {solicitud.IdSolicitud} eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
