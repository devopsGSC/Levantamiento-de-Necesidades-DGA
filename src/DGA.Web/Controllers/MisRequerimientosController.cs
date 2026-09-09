using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

/// <summary>
/// Pantalla de los roles delegados (Compras DGA, Mantenimiento DGA, Otro): solo ven las
/// solicitudes que el Administrador les asignó a su Unidad Ejecutora al aprobarlas, y solo
/// pueden avanzarlas un paso a la vez (Aprobado -> En Proceso -> Finalizado). No pueden
/// aprobar, denegar ni reasignar la Unidad Ejecutora — eso lo sigue haciendo el admin.
/// </summary>
[Authorize(Roles = $"{Roles.ComprasDGA},{Roles.MantenimientoDGA},{Roles.Otro}")]
[Route("MisRequerimientos")]
public class MisRequerimientosController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private const int PorPagina = 10;

    private int UsuarioIdActual => int.Parse(userManager.GetUserId(User)!);

    // La pertenencia a exactamente uno de los 3 roles delegados ya la exige el [Authorize]
    // de arriba, así que acá siempre hay una Unidad Ejecutora resuelta.
    private byte UnidadEjecutoraIdActual => Roles.UnidadEjecutoraDelRolDelegado(User)!.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, byte? estado, int pagina = 1)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var query = db.Solicitudes.Where(s => !s.IsDeleted && s.Items.Any(i => i.UnidadEjecutoraId == unidadId)
            && (s.EstadoId == Estados.Aprobado || s.EstadoId == Estados.EnProceso || s.EstadoId == Estados.Finalizado));

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
            .OrderByDescending(s => s.FechaRevision)
            .Skip((pagina - 1) * PorPagina)
            .Take(PorPagina)
            .Select(s => new MisRequerimientoListItemViewModel
            {
                Id = s.Id,
                IdSolicitud = s.IdSolicitud,
                EstadoId = s.EstadoId,
                Estado = s.Estado.Nombre,
                NombreResponsable = s.NombreResponsable,
                Aduana = s.Aduana.Codigo + " - " + s.Aduana.Nombre,
                FechaRevision = s.FechaRevision,
                Progreso = s.Progreso,
                CantidadItemsAsignados = s.Items.Count(i => i.UnidadEjecutoraId == unidadId),
            })
            .ToListAsync();

        var vm = new MisRequerimientoIndexViewModel
        {
            Solicitudes = solicitudes,
            Busqueda = busqueda,
            EstadoFiltro = estado,
            EstadoOptions = await db.EstadosSolicitud
                .Where(e => e.Id == Estados.Aprobado || e.Id == Estados.EnProceso || e.Id == Estados.Finalizado)
                .OrderBy(e => e.Orden).Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TotalResultados = total,
        };
        return View(vm);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var solicitud = await db.Solicitudes
            .Include(s => s.Aduana).ThenInclude(a => a.TipoAduana)
            .Include(s => s.Cargo)
            .Include(s => s.Estado)
            .Include(s => s.Items).ThenInclude(i => i.Componente)
            .Include(s => s.Items).ThenInclude(i => i.Subcomponente)
            .Include(s => s.Items).ThenInclude(i => i.Elemento)
            .Include(s => s.Items).ThenInclude(i => i.Detalle)
            .Include(s => s.Items).ThenInclude(i => i.Fotografias)
            .Include(s => s.Items).ThenInclude(i => i.CompletadoPorUsuario)
            .Include(s => s.Items).ThenInclude(i => i.DenegadoPorUsuario)
            .Include(s => s.Historial).ThenInclude(h => h.EstadoAnterior)
            .Include(s => s.Historial).ThenInclude(h => h.EstadoNuevo)
            .Include(s => s.Historial).ThenInclude(h => h.SolicitudItem)
            .Include(s => s.Historial).ThenInclude(h => h.UsuarioCambio)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.Items.Any(i => i.UnidadEjecutoraId == unidadId)
                && (s.EstadoId == Estados.Aprobado || s.EstadoId == Estados.EnProceso || s.EstadoId == Estados.Finalizado));

        if (solicitud is null)
        {
            return NotFound();
        }

        // Ítems de otras Unidades Ejecutoras no se muestran acá — este rol solo gestiona
        // los suyos, aunque la solicitud completa traiga ítems de varias unidades.
        var itemsPropios = solicitud.Items.Where(i => i.UnidadEjecutoraId == unidadId).OrderBy(i => i.NumeroItem).ToList();
        var historialPropio = solicitud.Historial.Where(h => h.SolicitudItemId is null || itemsPropios.Any(i => i.Id == h.SolicitudItemId));

        var siguiente = Estados.SiguienteEstadoDelegado(solicitud.EstadoId);
        var vm = new MisRequerimientoDetailViewModel
        {
            Id = solicitud.Id,
            IdSolicitud = solicitud.IdSolicitud,
            EstadoId = solicitud.EstadoId,
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
            PermiteGestionItems = Estados.PermiteGestionItems(solicitud.EstadoId),
            SiguienteEstadoNombre = siguiente.HasValue
                ? await db.EstadosSolicitud.Where(e => e.Id == siguiente.Value).Select(e => e.Nombre).FirstOrDefaultAsync()
                : null,
            Items = itemsPropios.Select(i => new SolicitudDetailItemViewModel
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
                Completado = i.Completado,
                FechaCompletado = i.FechaCompletado,
                CompletadoPor = i.CompletadoPorUsuario?.Nombre,
                Denegado = i.Denegado,
                MotivoDenegacion = i.MotivoDenegacion,
                FechaDenegado = i.FechaDenegado,
                DenegadoPor = i.DenegadoPorUsuario?.Nombre,
            }).ToList(),
            Historial = historialPropio.OrderByDescending(h => h.FechaCambio).Select(h => new SolicitudHistorialItemViewModel
            {
                EstadoAnterior = h.EstadoAnterior?.Nombre,
                EstadoNuevo = h.EstadoNuevo?.Nombre,
                NumeroItem = h.SolicitudItem?.NumeroItem,
                ItemCompletado = h.ItemCompletado,
                ItemDenegado = h.ItemDenegado,
                UsuarioCambio = h.UsuarioCambio != null ? h.UsuarioCambio.Nombre : null,
                Comentario = h.Comentario,
                FechaCambio = h.FechaCambio,
            }).ToList(),
        };

        return View(vm);
    }

    [HttpPost("Items/MarcarCompletado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarItemCompletado(int itemId, bool completado, string? comentario)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var item = await db.SolicitudItems
            .Include(i => i.Solicitud).ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(i => i.Id == itemId && !i.Solicitud.IsDeleted && i.UnidadEjecutoraId == unidadId);
        if (item is null)
        {
            return NotFound();
        }

        var (ok, error, historial) = ItemCompletado.Marcar(item, completado, comentario, UsuarioIdActual);
        if (!ok)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id = item.SolicitudId });
        }

        db.SolicitudHistorial.Add(historial!);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Ítem #{item.NumeroItem} {(completado ? "marcado" : "desmarcado")} como completado.";
        return RedirectToAction(nameof(Details), new { id = item.SolicitudId });
    }

    [HttpPost("Items/MarcarDenegado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarItemDenegado(int itemId, bool denegado, string? comentario)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var item = await db.SolicitudItems
            .Include(i => i.Solicitud).ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(i => i.Id == itemId && !i.Solicitud.IsDeleted && i.UnidadEjecutoraId == unidadId);
        if (item is null)
        {
            return NotFound();
        }

        var (ok, error, historial) = ItemDenegado.Marcar(item, denegado, comentario, UsuarioIdActual);
        if (!ok)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id = item.SolicitudId });
        }

        db.SolicitudHistorial.Add(historial!);
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Ítem #{item.NumeroItem} {(denegado ? "denegado (no aplica)" : "vuelto a habilitar")}.";
        return RedirectToAction(nameof(Details), new { id = item.SolicitudId });
    }

    [HttpPost("AvanzarEstado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvanzarEstado(int solicitudId, string? comentario)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var solicitud = await db.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == solicitudId && !s.IsDeleted && s.Items.Any(i => i.UnidadEjecutoraId == unidadId));

        if (solicitud is null)
        {
            return NotFound();
        }

        var siguiente = Estados.SiguienteEstadoDelegado(solicitud.EstadoId);
        if (siguiente is null)
        {
            TempData["Error"] = "Esta solicitud no tiene un siguiente estado disponible.";
            return RedirectToAction(nameof(Details), new { id = solicitudId });
        }

        var estadoAnterior = solicitud.EstadoId;
        solicitud.EstadoId = siguiente.Value;
        solicitud.UpdatedAt = DateTime.UtcNow;
        if (siguiente.Value == Estados.Finalizado)
        {
            solicitud.FechaFinalizacion = DateTime.UtcNow;
        }

        db.SolicitudHistorial.Add(new SolicitudHistorial
        {
            SolicitudId = solicitud.Id,
            EstadoAnteriorId = estadoAnterior,
            EstadoNuevoId = siguiente.Value,
            UsuarioCambioId = UsuarioIdActual,
            Comentario = comentario,
            FechaCambio = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Estado de {solicitud.IdSolicitud} actualizado.";
        return RedirectToAction(nameof(Details), new { id = solicitudId });
    }
}
