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
    public async Task<IActionResult> Index(string? busqueda, byte? estado, int pagina = 1)
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
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalResultados = total,
            EstadoOptions = await db.EstadosSolicitud.OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
        };
        return View(vm);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var solicitud = await db.Solicitudes
            .Include(s => s.Aduana).ThenInclude(a => a.TipoAduana)
            .Include(s => s.Cargo)
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
            Aduana = $"{solicitud.Aduana.Codigo} - {solicitud.Aduana.Nombre}",
            TipoAduana = solicitud.Aduana.TipoAduana.Nombre,
            JustificacionGeneral = solicitud.JustificacionGeneral,
            ObservacionesGenerales = solicitud.ObservacionesGenerales,
            FechaRegistro = solicitud.FechaRegistro,
            FechaRevision = solicitud.FechaRevision,
            Progreso = solicitud.Progreso,
            EstadoOptions = await db.EstadosSolicitud.OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
            Items = solicitud.Items.OrderBy(i => i.NumeroItem).Select(i => new SolicitudDetailItemViewModel
            {
                NumeroItem = i.NumeroItem,
                Componente = i.Componente.Nombre,
                Subcomponente = i.Subcomponente.Nombre,
                Elemento = i.Elemento?.Nombre ?? i.ElementoLibre,
                Detalle = i.Detalle?.Nombre,
                CantidadSolicitada = i.CantidadSolicitada,
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

        var estadoAnterior = solicitud.EstadoId;
        solicitud.EstadoId = model.NuevoEstadoId;
        solicitud.Progreso = model.Progreso;
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
