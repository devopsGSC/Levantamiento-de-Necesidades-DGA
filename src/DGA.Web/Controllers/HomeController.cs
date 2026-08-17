using System.Diagnostics;
using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Controllers;

public class HomeController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    private const byte EstadoGuardadoBorrador = 1;
    private const byte EstadoFinalizado = 12;

    public async Task<IActionResult> Index()
    {
        var esAdmin = User.IsInRole(Roles.Administrador);
        var usuarioId = int.Parse(userManager.GetUserId(User)!);

        var query = db.Solicitudes.Where(s => !s.IsDeleted);
        if (!esAdmin)
        {
            query = query.Where(s => s.UsuarioId == usuarioId);
        }

        var total = await query.CountAsync();
        var borrador = await query.CountAsync(s => s.EstadoId == EstadoGuardadoBorrador);
        var finalizadas = await query.CountAsync(s => s.EstadoId == EstadoFinalizado);

        var recientes = await query
            .OrderByDescending(s => s.FechaRegistro)
            .Take(5)
            .Select(s => new HomeRecienteItem
            {
                IdSolicitud = s.IdSolicitud,
                Estado = s.Estado.Nombre,
                Aduana = s.Aduana.Nombre,
                FechaRegistro = s.FechaRegistro,
                ComponentePrincipal = s.Items
                    .OrderBy(i => i.NumeroItem)
                    .Select(i => i.Componente.Nombre)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var vm = new HomeIndexViewModel
        {
            EsAdmin = esAdmin,
            TotalSolicitudes = total,
            EnBorrador = borrador,
            Finalizadas = finalizadas,
            EnTramite = total - borrador - finalizadas,
            Recientes = recientes,
        };

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
