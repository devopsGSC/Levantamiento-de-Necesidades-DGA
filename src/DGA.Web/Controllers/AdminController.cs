using DGA.Web.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DGA.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
[Route("Admin")]
public class AdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction("Index", "AdminSolicitudes");
}
