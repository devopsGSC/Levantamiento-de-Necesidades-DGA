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
[Route("Admin/Reportes")]
public class AdminReportesController(
    ApplicationDbContext db,
    ReporteSemanalService reporteService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var reportes = await db.ReportesSemanales
            .Include(r => r.GeneradoPorUsuario)
            .OrderByDescending(r => r.FechaFin)
            .ThenByDescending(r => r.GeneradoEn)
            .Select(r => new ReporteSemanalListItemViewModel
            {
                Id = r.Id,
                FechaInicio = r.FechaInicio,
                FechaFin = r.FechaFin,
                CantidadSolicitudes = r.CantidadSolicitudes,
                GeneradoManualmente = r.GeneradoManualmente,
                GeneradoEn = r.GeneradoEn,
                GeneradoPor = r.GeneradoPorUsuario != null ? r.GeneradoPorUsuario.Nombre : null,
            })
            .ToListAsync();

        return View(new AdminReportesIndexViewModel { Reportes = reportes });
    }

    [HttpPost("Generar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generar()
    {
        // Corte manual: desde el lunes de la semana actual hasta hoy (no espera a que
        // termine la semana) — pensado para pedir un corte fuera del ciclo automático
        // de los domingos.
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var diasDesdeElLunes = ((int)hoy.DayOfWeek + 6) % 7; // DayOfWeek: Lunes = 1 ... Domingo = 0
        var inicioSemana = hoy.AddDays(-diasDesdeElLunes);

        var usuarioId = int.Parse(userManager.GetUserId(User)!);
        var reporte = await reporteService.GenerarAsync(inicioSemana, hoy, manual: true, usuarioId);

        TempData["Mensaje"] = $"Reporte generado: {inicioSemana:dd/MM/yyyy} al {hoy:dd/MM/yyyy} ({reporte.CantidadSolicitudes} solicitudes).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/Pdf")]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        var reporte = await db.ReportesSemanales.FindAsync(id);
        if (reporte is null)
        {
            return NotFound();
        }

        var ruta = reporteService.RutaFisicaCompleta(reporte.RutaPdf);
        if (!System.IO.File.Exists(ruta))
        {
            return NotFound();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(ruta);
        return File(bytes, "application/pdf", $"Reporte_{reporte.FechaInicio:yyyyMMdd}_{reporte.FechaFin:yyyyMMdd}.pdf");
    }

    [HttpGet("{id:int}/Excel")]
    public async Task<IActionResult> DescargarExcel(int id)
    {
        var reporte = await db.ReportesSemanales.FindAsync(id);
        if (reporte is null)
        {
            return NotFound();
        }

        var ruta = reporteService.RutaFisicaCompleta(reporte.RutaExcel);
        if (!System.IO.File.Exists(ruta))
        {
            return NotFound();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(ruta);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte_{reporte.FechaInicio:yyyyMMdd}_{reporte.FechaFin:yyyyMMdd}.xlsx");
    }
}
