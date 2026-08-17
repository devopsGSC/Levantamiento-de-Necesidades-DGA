using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

/// <summary>Panel de Configuración: datos de contacto y los catálogos "simples" (Cargos,
/// Prioridades, Tipos de Aduana) que no tienen otra tabla relacionada. Los catálogos con
/// jerarquía (Aduanas, Componentes→Detalles) tienen sus propios controladores —
/// ver AdminCatalogoAduanasController y AdminCatalogoComponentesController.</summary>
[Authorize(Roles = Roles.Administrador)]
[Route("Admin/Configuracion")]
public class AdminConfiguracionController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var config = await db.ConfiguracionSistema.FindAsync(1);

        var vm = new AdminConfiguracionIndexViewModel
        {
            SoporteTelefono = config?.SoporteTelefono ?? string.Empty,
            SoporteCorreo = config?.SoporteCorreo ?? string.Empty,
            SoporteHorario = config?.SoporteHorario ?? string.Empty,
            Cargos = await db.Cargos.OrderBy(c => c.Orden)
                .Select(c => new CatalogoSimpleItemViewModel { Id = c.Id, Nombre = c.Nombre, Orden = c.Orden, Activo = c.Activo })
                .ToListAsync(),
            Prioridades = await db.Prioridades.OrderBy(p => p.Orden)
                .Select(p => new CatalogoSimpleItemViewModel { Id = p.Id, Nombre = p.Nombre, Orden = p.Orden, Activo = p.Activo })
                .ToListAsync(),
            TiposAduana = await db.TiposAduana.OrderBy(t => t.Orden)
                .Select(t => new CatalogoSimpleItemViewModel { Id = t.Id, Nombre = t.Nombre, Orden = t.Orden, Activo = t.Activo })
                .ToListAsync(),
        };
        return View(vm);
    }

    [HttpPost("Contacto")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarContacto(string soporteTelefono, string soporteCorreo, string soporteHorario)
    {
        var config = await db.ConfiguracionSistema.FindAsync(1);
        if (config is null)
        {
            config = new ConfiguracionSistema { Id = 1 };
            db.ConfiguracionSistema.Add(config);
        }

        config.SoporteTelefono = soporteTelefono;
        config.SoporteCorreo = soporteCorreo;
        config.SoporteHorario = soporteHorario;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["Mensaje"] = "Datos de contacto actualizados.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Cargos
    // ------------------------------------------------------------------

    [HttpPost("Cargos/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCargo(string nombre)
    {
        var siguienteId = (byte)((await db.Cargos.MaxAsync(c => (byte?)c.Id)) is { } max ? max + 1 : 1);
        var siguienteOrden = (short)((await db.Cargos.MaxAsync(c => (short?)c.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Cargos.Add(new Cargo { Id = siguienteId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Cargo \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cargos/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCargo(byte id, string nombre)
    {
        var cargo = await db.Cargos.FindAsync(id);
        if (cargo is null) return NotFound();
        cargo.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Cargo actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Cargos/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarCargo(byte id, bool activo)
    {
        var cargo = await db.Cargos.FindAsync(id);
        if (cargo is null) return NotFound();
        cargo.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Cargo reactivado." : "Cargo desactivado — deja de ofrecerse en solicitudes nuevas.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Prioridades
    // ------------------------------------------------------------------

    [HttpPost("Prioridades/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPrioridad(string nombre)
    {
        var siguienteId = (byte)((await db.Prioridades.MaxAsync(p => (byte?)p.Id)) is { } max ? max + 1 : 1);
        var siguienteOrden = (short)((await db.Prioridades.MaxAsync(p => (short?)p.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Prioridades.Add(new Prioridad { Id = siguienteId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Prioridad \"{nombre}\" agregada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Prioridades/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarPrioridad(byte id, string nombre)
    {
        var prioridad = await db.Prioridades.FindAsync(id);
        if (prioridad is null) return NotFound();
        prioridad.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Prioridad actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Prioridades/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarPrioridad(byte id, bool activo)
    {
        var prioridad = await db.Prioridades.FindAsync(id);
        if (prioridad is null) return NotFound();
        prioridad.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Prioridad reactivada." : "Prioridad desactivada — deja de ofrecerse en solicitudes nuevas.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Tipos de Aduana
    // ------------------------------------------------------------------

    [HttpPost("TiposAduana/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTipoAduana(string nombre)
    {
        var siguienteId = (byte)((await db.TiposAduana.MaxAsync(t => (byte?)t.Id)) is { } max ? max + 1 : 1);
        var siguienteOrden = (short)((await db.TiposAduana.MaxAsync(t => (short?)t.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.TiposAduana.Add(new TipoAduana { Id = siguienteId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Tipo de Aduana \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("TiposAduana/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTipoAduana(byte id, string nombre)
    {
        var tipo = await db.TiposAduana.FindAsync(id);
        if (tipo is null) return NotFound();
        tipo.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Tipo de Aduana actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("TiposAduana/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarTipoAduana(byte id, bool activo)
    {
        var tipo = await db.TiposAduana.FindAsync(id);
        if (tipo is null) return NotFound();
        tipo.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Tipo de Aduana reactivado." : "Tipo de Aduana desactivado — deja de ofrecerse en solicitudes nuevas (sus aduanas también quedan fuera de los combos).";
        return RedirectToAction(nameof(Index));
    }
}
