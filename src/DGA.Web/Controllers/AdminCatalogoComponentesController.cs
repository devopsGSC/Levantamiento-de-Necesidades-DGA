using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

/// <summary>Gestión de la cascada de 4 niveles Componente → Subcomponente → Elemento →
/// Detalle. Se muestra como un árbol completo en una sola página (son pocas decenas de
/// filas en total) en vez de cargar cada nivel por AJAX — más simple y ya alcanza.</summary>
[Authorize(Roles = Roles.Administrador)]
[Route("Admin/Configuracion/Componentes")]
public class AdminCatalogoComponentesController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var componentes = await db.Componentes
            .Include(c => c.Subcomponentes.OrderBy(s => s.Orden)).ThenInclude(s => s.Elementos.OrderBy(e => e.Orden)).ThenInclude(e => e.Detalles.OrderBy(d => d.Orden))
            .AsSplitQuery()
            .OrderBy(c => c.Orden)
            .Select(c => new AdminComponenteNodoViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Activo = c.Activo,
                Subcomponentes = c.Subcomponentes.Select(s => new AdminSubcomponenteNodoViewModel
                {
                    Id = s.Id,
                    Nombre = s.Nombre,
                    Activo = s.Activo,
                    Elementos = s.Elementos.Select(e => new AdminElementoNodoViewModel
                    {
                        Id = e.Id,
                        Nombre = e.Nombre,
                        TieneDetalle = e.TieneDetalle,
                        Activo = e.Activo,
                        Detalles = e.Detalles.Select(d => new AdminDetalleNodoViewModel { Id = d.Id, Nombre = d.Nombre, Activo = d.Activo }).ToList(),
                    }).ToList(),
                }).ToList(),
            })
            .ToListAsync();

        return View(componentes);
    }

    // ------------------------------------------------------------------
    // Componente
    // ------------------------------------------------------------------

    [HttpPost("Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearComponente(string nombre)
    {
        var siguienteId = (byte)((await db.Componentes.MaxAsync(c => (byte?)c.Id)) is { } max ? max + 1 : 1);
        var siguienteOrden = (short)((await db.Componentes.MaxAsync(c => (short?)c.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Componentes.Add(new Componente { Id = siguienteId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Componente \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarComponente(byte id, string nombre)
    {
        var c = await db.Componentes.FindAsync(id);
        if (c is null) return NotFound();
        c.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Componente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarComponente(byte id, bool activo)
    {
        var c = await db.Componentes.FindAsync(id);
        if (c is null) return NotFound();
        c.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Componente reactivado." : "Componente desactivado.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Subcomponente
    // ------------------------------------------------------------------

    [HttpPost("Subcomponentes/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearSubcomponente(byte componenteId, string nombre)
    {
        var siguienteId = (await db.Subcomponentes.MaxAsync(s => (int?)s.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Subcomponentes.Where(s => s.ComponenteId == componenteId).MaxAsync(s => (short?)s.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Subcomponentes.Add(new Subcomponente { Id = siguienteId, ComponenteId = componenteId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Subcomponente \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Subcomponentes/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarSubcomponente(int id, string nombre)
    {
        var s = await db.Subcomponentes.FindAsync(id);
        if (s is null) return NotFound();
        s.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Subcomponente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Subcomponentes/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarSubcomponente(int id, bool activo)
    {
        var s = await db.Subcomponentes.FindAsync(id);
        if (s is null) return NotFound();
        s.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Subcomponente reactivado." : "Subcomponente desactivado.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Elemento
    // ------------------------------------------------------------------

    [HttpPost("Elementos/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearElemento(int subcomponenteId, string nombre, bool tieneDetalle)
    {
        var siguienteId = (await db.Elementos.MaxAsync(e => (int?)e.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Elementos.Where(e => e.SubcomponenteId == subcomponenteId).MaxAsync(e => (short?)e.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Elementos.Add(new Elemento { Id = siguienteId, SubcomponenteId = subcomponenteId, Nombre = nombre, TieneDetalle = tieneDetalle, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Elemento \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Elementos/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarElemento(int id, string nombre, bool tieneDetalle)
    {
        var e = await db.Elementos.FindAsync(id);
        if (e is null) return NotFound();
        e.Nombre = nombre;
        e.TieneDetalle = tieneDetalle;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Elemento actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Elementos/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarElemento(int id, bool activo)
    {
        var e = await db.Elementos.FindAsync(id);
        if (e is null) return NotFound();
        e.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Elemento reactivado." : "Elemento desactivado.";
        return RedirectToAction(nameof(Index));
    }

    // ------------------------------------------------------------------
    // Detalle
    // ------------------------------------------------------------------

    [HttpPost("Detalles/Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDetalle(int elementoId, string nombre)
    {
        var siguienteId = (await db.Detalles.MaxAsync(d => (int?)d.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Detalles.Where(d => d.ElementoId == elementoId).MaxAsync(d => (short?)d.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Detalles.Add(new Detalle { Id = siguienteId, ElementoId = elementoId, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Detalle \"{nombre}\" agregado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Detalles/{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarDetalle(int id, string nombre)
    {
        var d = await db.Detalles.FindAsync(id);
        if (d is null) return NotFound();
        d.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Detalle actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Detalles/{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivarDetalle(int id, bool activo)
    {
        var d = await db.Detalles.FindAsync(id);
        if (d is null) return NotFound();
        d.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Detalle reactivado." : "Detalle desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
