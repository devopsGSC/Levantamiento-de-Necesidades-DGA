using DGA.Web.Data;
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
}
