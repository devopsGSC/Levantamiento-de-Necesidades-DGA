using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

/// <summary>Fila liviana con lo mínimo para calcular todas las métricas del dashboard sin repetir joins.</summary>
file class SolicitudResumen
{
    public int Id { get; set; }
    public string IdSolicitud { get; set; } = string.Empty;
    public byte EstadoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int AduanaId { get; set; }
    public string Aduana { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public byte Progreso { get; set; }
    public byte? PrimerComponenteId { get; set; }
    public string PrimerComponente { get; set; } = "Sin componente";
    public byte? PrimeraPrioridadId { get; set; }
    public string PrimeraPrioridad { get; set; } = "-";
}

[Authorize(Roles = Roles.Administrador)]
[Route("Dashboard")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        int? aduanaId, byte? componenteId, byte? estadoId, byte? prioridadId,
        byte? unidadEjecutoraId, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = db.Solicitudes.Where(s => !s.IsDeleted);
        if (aduanaId.HasValue)
        {
            query = query.Where(s => s.AduanaId == aduanaId.Value);
        }
        if (componenteId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.ComponenteId == componenteId.Value));
        }
        if (estadoId.HasValue)
        {
            query = query.Where(s => s.EstadoId == estadoId.Value);
        }
        if (prioridadId.HasValue)
        {
            query = query.Where(s => s.Items.Any(i => i.PrioridadId == prioridadId.Value));
        }
        if (unidadEjecutoraId.HasValue)
        {
            query = query.Where(s => s.UnidadEjecutoraId == unidadEjecutoraId.Value);
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

        var filas = await query
            .Select(s => new SolicitudResumen
            {
                Id = s.Id,
                IdSolicitud = s.IdSolicitud,
                EstadoId = s.EstadoId,
                Estado = s.Estado.Nombre,
                AduanaId = s.AduanaId,
                Aduana = s.Aduana.Codigo + " - " + s.Aduana.Nombre,
                FechaRegistro = s.FechaRegistro,
                Progreso = s.Progreso ?? 0,
                PrimerComponenteId = s.Items.OrderBy(i => i.NumeroItem).Select(i => (byte?)i.ComponenteId).FirstOrDefault(),
                PrimerComponente = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Componente.Nombre).FirstOrDefault() ?? "Sin componente",
                PrimeraPrioridadId = s.Items.OrderBy(i => i.NumeroItem).Select(i => (byte?)i.PrioridadId).FirstOrDefault(),
                PrimeraPrioridad = s.Items.OrderBy(i => i.NumeroItem).Select(i => i.Prioridad.Nombre).FirstOrDefault() ?? "-",
            })
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            FiltroAduanaId = aduanaId,
            FiltroComponenteId = componenteId,
            FiltroEstadoId = estadoId,
            FiltroPrioridadId = prioridadId,
            FiltroUnidadEjecutoraId = unidadEjecutoraId,
            FiltroFechaDesde = fechaDesde,
            FiltroFechaHasta = fechaHasta,
            AduanaOptions = await db.Aduanas.OrderBy(a => a.Orden)
                .Select(a => new OpcionCatalogo(a.Id, a.Codigo + " - " + a.Nombre)).ToListAsync(),
            ComponenteOptions = await db.Componentes.OrderBy(c => c.Orden)
                .Select(c => new OpcionCatalogo(c.Id, c.Nombre)).ToListAsync(),
            EstadoOptions = await db.EstadosSolicitud.OrderBy(e => e.Orden)
                .Select(e => new OpcionCatalogo(e.Id, e.Nombre)).ToListAsync(),
            PrioridadOptions = await db.Prioridades.OrderBy(p => p.Orden)
                .Select(p => new OpcionCatalogo(p.Id, p.Nombre)).ToListAsync(),
            UnidadEjecutoraOptions = await db.UnidadesEjecutoras.OrderBy(u => u.Orden)
                .Select(u => new OpcionCatalogo(u.Id, u.Nombre)).ToListAsync(),
            UltimaSolicitudFecha = filas.Count > 0 ? filas.Max(f => f.FechaRegistro) : null,
            Total = filas.Count,
        };

        vm.Finalizadas = filas.Count(f => f.EstadoId == Estados.Finalizado);
        vm.Denegadas = filas.Count(f => f.EstadoId == Estados.Denegado);
        vm.Borradores = filas.Count(f => f.EstadoId == Estados.GuardadoBorrador);
        vm.EnProceso = vm.Total - vm.Finalizadas - vm.Denegadas - vm.Borradores;
        vm.Pendientes = filas.Count(f => f.EstadoId == Estados.Solicitado);
        vm.PrioridadAlta = filas.Count(f => f.PrimeraPrioridadId == 1);
        vm.ProgresoPromedio = vm.Total == 0 ? 0 : Math.Round(filas.Average(f => f.Progreso), 0);

        vm.DistribucionProgreso = new List<DashboardBucket>
        {
            NuevoBucket("0-25%", filas.Count(f => f.Progreso <= 25), vm.Total),
            NuevoBucket("26-50%", filas.Count(f => f.Progreso is > 25 and <= 50), vm.Total),
            NuevoBucket("51-75%", filas.Count(f => f.Progreso is > 50 and <= 75), vm.Total),
            NuevoBucket("76-100%", filas.Count(f => f.Progreso > 75), vm.Total),
        };

        vm.PorPrioridad = filas
            .GroupBy(f => f.PrimeraPrioridad)
            .Select(g => NuevoBucket(g.Key, g.Count(), vm.Total))
            .OrderByDescending(b => b.Cantidad)
            .ToList();

        vm.PorEstado = filas
            .GroupBy(f => f.Estado)
            .Select(g => NuevoBucket(g.Key, g.Count(), vm.Total))
            .OrderByDescending(b => b.Cantidad)
            .ToList();

        vm.TopAduanas = filas
            .GroupBy(f => f.Aduana)
            .Select(g => new DashboardAduanaRendimiento
            {
                Aduana = g.Key,
                Total = g.Count(),
                Finalizadas = g.Count(f => f.EstadoId == Estados.Finalizado),
                PorcentajeFinalizadas = g.Count() == 0 ? 0 : Math.Round(g.Count(f => f.EstadoId == Estados.Finalizado) * 100.0 / g.Count(), 0),
            })
            .OrderByDescending(a => a.Total)
            .Take(5)
            .ToList();

        var porAduana = filas.GroupBy(f => f.Aduana).OrderByDescending(g => g.Count()).Take(10).ToList();
        vm.PorAduanaLabels = porAduana.Select(g => g.Key).ToList();
        vm.PorAduanaValores = porAduana.Select(g => g.Count()).ToList();

        var porComponente = filas.GroupBy(f => f.PrimerComponente).OrderByDescending(g => g.Count()).ToList();
        vm.PorComponenteLabels = porComponente.Select(g => g.Key).ToList();
        vm.PorComponenteValores = porComponente.Select(g => g.Count()).ToList();

        var tendenciaHasta = fechaHasta?.Date ?? DateTime.UtcNow.Date;
        var tendenciaDesde = fechaDesde?.Date ?? tendenciaHasta.AddDays(-13);
        for (var dia = tendenciaDesde; dia <= tendenciaHasta; dia = dia.AddDays(1))
        {
            vm.TendenciaLabels.Add(dia.ToString("dd MMM"));
            vm.TendenciaValores.Add(filas.Count(f => f.FechaRegistro.Date == dia));
        }

        vm.SolicitudesRecientes = filas
            .OrderByDescending(f => f.FechaRegistro)
            .Take(10)
            .Select(f => new DashboardSolicitudReciente
            {
                Id = f.Id,
                IdSolicitud = f.IdSolicitud,
                Componente = f.PrimerComponente,
                Aduana = f.Aduana,
                Prioridad = f.PrimeraPrioridad,
                Estado = f.Estado,
                Progreso = f.Progreso,
                FechaRegistro = f.FechaRegistro,
            })
            .ToList();

        return View(vm);
    }

    private static DashboardBucket NuevoBucket(string etiqueta, int cantidad, int total) => new()
    {
        Etiqueta = etiqueta,
        Cantidad = cantidad,
        Porcentaje = total == 0 ? 0 : Math.Round(cantidad * 100.0 / total, 0),
    };
}
