using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
[Route("Admin/Configuracion/Aduanas")]
public class AdminCatalogoAduanasController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(byte? tipo)
    {
        var query = db.Aduanas.AsQueryable();
        if (tipo.HasValue)
        {
            query = query.Where(a => a.TipoAduanaId == tipo.Value);
        }

        var vm = new AdminCatalogoAduanasViewModel
        {
            TipoFiltro = tipo,
            TiposAduanaOptions = await db.TiposAduana.OrderBy(t => t.Orden)
                .Select(t => new OpcionCatalogo(t.Id, t.Nombre)).ToListAsync(),
            Aduanas = await query.OrderBy(a => a.TipoAduanaId).ThenBy(a => a.Orden)
                .Select(a => new AdminAduanaItemViewModel { Id = a.Id, TipoAduanaId = a.TipoAduanaId, Codigo = a.Codigo, Nombre = a.Nombre, Activo = a.Activo })
                .ToListAsync(),
        };
        return View(vm);
    }

    [HttpPost("Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(byte tipoAduanaId, string codigo, string nombre)
    {
        var siguienteId = (await db.Aduanas.MaxAsync(a => (int?)a.Id)) is { } max ? max + 1 : 1;
        var siguienteOrden = (short)((await db.Aduanas.Where(a => a.TipoAduanaId == tipoAduanaId).MaxAsync(a => (short?)a.Orden)) is { } maxOrden ? maxOrden + 1 : 1);
        db.Aduanas.Add(new Aduana { Id = siguienteId, TipoAduanaId = tipoAduanaId, Codigo = codigo, Nombre = nombre, Orden = siguienteOrden, Activo = true });
        await db.SaveChangesAsync();
        TempData["Mensaje"] = $"Aduana \"{codigo} - {nombre}\" agregada.";
        return RedirectToAction(nameof(Index), new { tipo = tipoAduanaId });
    }

    [HttpPost("{id:int}/Editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, string codigo, string nombre)
    {
        var aduana = await db.Aduanas.FindAsync(id);
        if (aduana is null) return NotFound();
        aduana.Codigo = codigo;
        aduana.Nombre = nombre;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = "Aduana actualizada.";
        return RedirectToAction(nameof(Index), new { tipo = aduana.TipoAduanaId });
    }

    [HttpPost("{id:int}/Activo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id, bool activo)
    {
        var aduana = await db.Aduanas.FindAsync(id);
        if (aduana is null) return NotFound();
        aduana.Activo = activo;
        await db.SaveChangesAsync();
        TempData["Mensaje"] = activo ? "Aduana reactivada." : "Aduana desactivada — deja de ofrecerse en solicitudes nuevas.";
        return RedirectToAction(nameof(Index), new { tipo = aduana.TipoAduanaId });
    }
}
