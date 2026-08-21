using DGA.Web.Data;
using DGA.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

/// <summary>Endpoints JSON que alimentan los combos en cascada del formulario de solicitud.
/// Solo devuelven catálogos activos, salvo que se pida incluir puntualmente un id inactivo
/// (parámetro "incluir") — necesario al editar un ítem viejo cuyo valor ya fue desactivado
/// por un admin, para que no desaparezca de su propio combo.</summary>
public class CatalogosController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Aduanas(byte tipoAduanaId, int? incluir = null)
    {
        var datos = await db.Aduanas
            .Where(a => a.TipoAduanaId == tipoAduanaId && (a.Activo || a.Id == incluir))
            .OrderBy(a => a.Orden)
            .Select(a => new { id = a.Id, nombre = a.Codigo + " - " + a.Nombre })
            .ToListAsync();
        return Json(datos);
    }

    [HttpGet]
    public async Task<IActionResult> Subcomponentes(byte componenteId, int? incluir = null)
    {
        var datos = await db.Subcomponentes
            .Where(s => s.ComponenteId == componenteId && (s.Activo || s.Id == incluir))
            .OrderBy(s => s.Orden)
            .Select(s => new { id = s.Id, nombre = s.Nombre })
            .ToListAsync();
        return Json(datos);
    }

    [HttpGet]
    public async Task<IActionResult> Elementos(int subcomponenteId, int? incluir = null)
    {
        var datos = await db.Elementos
            .Where(e => e.SubcomponenteId == subcomponenteId && (e.Activo || e.Id == incluir))
            .OrderBy(e => e.Orden)
            .Select(e => new { id = e.Id, nombre = e.Nombre, tieneDetalle = e.TieneDetalle })
            .ToListAsync();
        return Json(datos);
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int elementoId, int? incluir = null)
    {
        var datos = await db.Detalles
            .Where(d => d.ElementoId == elementoId && (d.Activo || d.Id == incluir))
            .OrderBy(d => d.Orden)
            .Select(d => new { id = d.Id, nombre = d.Nombre })
            .ToListAsync();
        return Json(datos);
    }

    /// <summary>Cualquier usuario logueado puede sumar un Elemento que no encuentra en la
    /// lista mientras completa una solicitud — no es una acción exclusiva de Admin, pero
    /// queda disponible para todas las solicitudes futuras igual que si lo hubiera cargado
    /// un administrador desde el catálogo.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearElemento(int subcomponenteId, string nombre)
    {
        nombre = nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            return BadRequest(new { error = "Ingresá un nombre." });
        }
        if (!await db.Subcomponentes.AnyAsync(s => s.Id == subcomponenteId))
        {
            return NotFound();
        }
        var existente = await db.Elementos.FirstOrDefaultAsync(e =>
            e.SubcomponenteId == subcomponenteId && e.Nombre.ToLower() == nombre.ToLower());
        if (existente is not null)
        {
            return Json(new { id = existente.Id, nombre = existente.Nombre, tieneDetalle = existente.TieneDetalle });
        }

        var siguienteId = (await db.Elementos.MaxAsync(e => (int?)e.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Elementos.Where(e => e.SubcomponenteId == subcomponenteId).MaxAsync(e => (short?)e.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        var elemento = new Elemento { Id = siguienteId, SubcomponenteId = subcomponenteId, Nombre = nombre, TieneDetalle = false, Orden = siguienteOrden, Activo = true };
        db.Elementos.Add(elemento);
        await db.SaveChangesAsync();

        return Json(new { id = elemento.Id, nombre = elemento.Nombre, tieneDetalle = elemento.TieneDetalle });
    }

    /// <summary>Igual que <see cref="CrearElemento"/> pero para el 4º nivel (Detalle), dentro
    /// de un Elemento que ya requiere ese nivel de precisión.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearDetalle(int elementoId, string nombre)
    {
        nombre = nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0)
        {
            return BadRequest(new { error = "Ingresá un nombre." });
        }
        if (!await db.Elementos.AnyAsync(e => e.Id == elementoId))
        {
            return NotFound();
        }
        var existente = await db.Detalles.FirstOrDefaultAsync(d =>
            d.ElementoId == elementoId && d.Nombre.ToLower() == nombre.ToLower());
        if (existente is not null)
        {
            return Json(new { id = existente.Id, nombre = existente.Nombre });
        }

        var siguienteId = (await db.Detalles.MaxAsync(d => (int?)d.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Detalles.Where(d => d.ElementoId == elementoId).MaxAsync(d => (short?)d.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        var detalle = new Detalle { Id = siguienteId, ElementoId = elementoId, Nombre = nombre, Orden = siguienteOrden, Activo = true };
        db.Detalles.Add(detalle);
        await db.SaveChangesAsync();

        return Json(new { id = detalle.Id, nombre = detalle.Nombre });
    }
}
