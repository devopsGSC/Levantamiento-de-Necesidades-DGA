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
    private int UsuarioIdActual => int.Parse(userManager.GetUserId(User)!);

    // La pertenencia a exactamente uno de los 3 roles delegados ya la exige el [Authorize]
    // de arriba, así que acá siempre hay una Unidad Ejecutora resuelta.
    private byte UnidadEjecutoraIdActual => Roles.UnidadEjecutoraDelRolDelegado(User)!.Value;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda, byte? estado)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var query = db.Solicitudes.Where(s => !s.IsDeleted && s.UnidadEjecutoraId == unidadId
            && (s.EstadoId == Estados.Aprobado || s.EstadoId == Estados.EnProceso || s.EstadoId == Estados.Finalizado));

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(s => s.IdSolicitud.Contains(busqueda) || s.NombreResponsable.Contains(busqueda));
        }
        if (estado.HasValue)
        {
            query = query.Where(s => s.EstadoId == estado.Value);
        }

        var solicitudes = await query
            .OrderByDescending(s => s.FechaRevision)
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
            .Include(s => s.Historial).ThenInclude(h => h.EstadoAnterior)
            .Include(s => s.Historial).ThenInclude(h => h.EstadoNuevo)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.UnidadEjecutoraId == unidadId
                && (s.EstadoId == Estados.Aprobado || s.EstadoId == Estados.EnProceso || s.EstadoId == Estados.Finalizado));

        if (solicitud is null)
        {
            return NotFound();
        }

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
            SiguienteEstadoNombre = siguiente.HasValue
                ? await db.EstadosSolicitud.Where(e => e.Id == siguiente.Value).Select(e => e.Nombre).FirstOrDefaultAsync()
                : null,
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

    [HttpPost("AvanzarEstado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvanzarEstado(int solicitudId, string? comentario)
    {
        var unidadId = UnidadEjecutoraIdActual;
        var solicitud = await db.Solicitudes
            .FirstOrDefaultAsync(s => s.Id == solicitudId && !s.IsDeleted && s.UnidadEjecutoraId == unidadId);

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
        solicitud.Progreso = Estados.ProgresoParaEstado(siguiente.Value);
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
