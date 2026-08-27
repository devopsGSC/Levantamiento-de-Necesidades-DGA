using System.ComponentModel.DataAnnotations;
using DGA.Web.Data.Entities;
using DGA.Web.Models;
using DGA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DGA.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
[Route("Admin/Usuarios")]
public class AdminUsuariosController(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    CargaMasivaUsuariosService cargaMasivaService,
    ILogger<AdminUsuariosController> logger) : Controller
{
    private const int MaxFilasCargaMasiva = 500;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? busqueda)
    {
        var actual = await userManager.GetUserAsync(User);

        var query = userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(u => u.Nombre.Contains(busqueda) || u.Email!.Contains(busqueda));
        }

        var usuarios = new List<AdminUsuarioListItemViewModel>();
        foreach (var usuario in query.OrderBy(u => u.Nombre).ToList())
        {
            var roles = await userManager.GetRolesAsync(usuario);
            var esAdminActualmente = roles.Contains(Roles.Administrador);
            usuarios.Add(new AdminUsuarioListItemViewModel
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email ?? string.Empty,
                Rol = roles.FirstOrDefault() ?? "-",
                Cargo = usuario.Cargo,
                Aduana = usuario.Aduana,
                Subdireccion = usuario.Subdireccion,
                Departamento = usuario.Departamento,
                Activo = usuario.Activo,
                CreatedAt = usuario.CreatedAt,
                EsAdminPrincipal = usuario.EsAdminPrincipal,
                EsFilaPropia = usuario.Id == actual!.Id,
                RolEditable = usuario.Id != actual.Id && (!esAdminActualmente || actual.EsAdminPrincipal),
            });
        }

        return View(new AdminUsuarioIndexViewModel { Usuarios = usuarios, Busqueda = busqueda });
    }

    [HttpGet("Crear")]
    public IActionResult Crear() => View(new CrearUsuarioViewModel { Rol = Roles.Usuario });

    [HttpPost("Crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel model)
    {
        if (model.Rol != Roles.Administrador && model.Rol != Roles.Usuario)
        {
            ModelState.AddModelError(nameof(model.Rol), "Rol inválido.");
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese correo.");
            return View(model);
        }

        var contrasenaTemporal = PasswordGenerator.Generar();
        var usuario = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            Nombre = model.Nombre,
            Cargo = model.Cargo,
            Aduana = model.Aduana,
            Subdireccion = model.Subdireccion,
            Departamento = model.Departamento,
            Activo = true,
            PrimerInicioSesion = true,
            PasswordTemporal = true,
        };

        var resultado = await userManager.CreateAsync(usuario, contrasenaTemporal);
        if (!resultado.Succeeded)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await userManager.AddToRoleAsync(usuario, model.Rol);

        await emailSender.SendAsync(
            model.Email,
            "Credenciales de acceso — Levantamiento de Necesidades",
            ConstruirCuerpoCredenciales(model.Email, contrasenaTemporal, esNuevaCuenta: true));

        logger.LogInformation("Usuario creado por admin: {Email}", model.Email);
        TempData["Mensaje"] = $"Usuario {model.Email} creado. Se envió un correo con las credenciales.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("CambiarContrasena")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(int id, string nuevaContrasena, string confirmarContrasena)
    {
        var usuario = await userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena != confirmarContrasena)
        {
            TempData["Error"] = "Las contraseñas no coinciden.";
            return RedirectToAction(nameof(Index));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await userManager.ResetPasswordAsync(usuario, token, nuevaContrasena);
        if (!resultado.Succeeded)
        {
            TempData["Error"] = string.Join(" ", resultado.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        usuario.CredencialesReenviadasEn = DateTime.UtcNow;
        await userManager.UpdateAsync(usuario);

        await emailSender.SendAsync(
            usuario.Email!,
            "Credenciales de acceso — Levantamiento de Necesidades",
            ConstruirCuerpoCredenciales(usuario.Email!, nuevaContrasena, esNuevaCuenta: false));

        logger.LogInformation("Contraseña de {Email} cambiada manualmente por un admin.", usuario.Email);
        TempData["Mensaje"] = $"Contraseña de {usuario.Email} actualizada. Se envió un correo con las nuevas credenciales.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("CambiarRol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, string nuevoRol)
    {
        if (nuevoRol != Roles.Administrador && nuevoRol != Roles.Usuario)
        {
            TempData["Error"] = "Rol inválido.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
        {
            return NotFound();
        }

        var actual = await userManager.GetUserAsync(User);
        if (usuario.Id == actual!.Id)
        {
            TempData["Error"] = "No podés cambiar tu propio rol.";
            return RedirectToAction(nameof(Index));
        }

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        var esAdminActualmente = rolesActuales.Contains(Roles.Administrador);

        if (esAdminActualmente && !actual.EsAdminPrincipal)
        {
            TempData["Error"] = "Solo el administrador principal puede cambiar el rol de otro administrador.";
            return RedirectToAction(nameof(Index));
        }

        if (esAdminActualmente && nuevoRol != Roles.Administrador)
        {
            var totalAdmins = (await userManager.GetUsersInRoleAsync(Roles.Administrador)).Count;
            if (totalAdmins <= 1)
            {
                TempData["Error"] = "No podés quitarle el rol de Administrador al único admin del sistema.";
                return RedirectToAction(nameof(Index));
            }
        }

        if (rolesActuales.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
        }
        await userManager.AddToRoleAsync(usuario, nuevoRol);

        logger.LogInformation("Rol de {Email} cambiado a {Rol} por {Admin}", usuario.Email, nuevoRol, actual.Email);
        TempData["Mensaje"] = $"Rol de {usuario.Email} actualizado a {nuevoRol}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("CargaMasiva")]
    public IActionResult CargaMasiva() => View();

    [HttpGet("CargaMasiva/Plantilla")]
    public IActionResult DescargarPlantillaCargaMasiva()
    {
        var contenido = cargaMasivaService.GenerarPlantilla();
        return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "plantilla-carga-usuarios.xlsx");
    }

    [HttpPost("CargaMasiva")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CargaMasiva(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccioná un archivo Excel para subir.";
            return RedirectToAction(nameof(CargaMasiva));
        }

        List<FilaCargaMasivaUsuario> filas;
        try
        {
            using var stream = archivo.OpenReadStream();
            filas = cargaMasivaService.LeerFilas(stream);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo leer el archivo de carga masiva de usuarios.");
            TempData["Error"] = "No se pudo leer el archivo. Verificá que sea un Excel (.xlsx) con el formato de la plantilla.";
            return RedirectToAction(nameof(CargaMasiva));
        }

        if (filas.Count == 0)
        {
            TempData["Error"] = "El archivo no tiene filas con datos.";
            return RedirectToAction(nameof(CargaMasiva));
        }

        if (filas.Count > MaxFilasCargaMasiva)
        {
            TempData["Error"] = $"El archivo tiene {filas.Count} filas; el máximo permitido por carga es {MaxFilasCargaMasiva}.";
            return RedirectToAction(nameof(CargaMasiva));
        }

        var resultado = new CargaMasivaResultadoViewModel();
        var correosEnArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fila in filas)
        {
            var motivoError = ValidarFila(fila, correosEnArchivo);
            if (motivoError is not null)
            {
                resultado.Errores.Add(new CargaMasivaErrorViewModel { Fila = fila.Fila, Email = fila.Email, Motivo = motivoError });
                continue;
            }

            correosEnArchivo.Add(fila.Email);

            if (await userManager.FindByEmailAsync(fila.Email) is not null)
            {
                resultado.Errores.Add(new CargaMasivaErrorViewModel { Fila = fila.Fila, Email = fila.Email, Motivo = "Ya existe un usuario con ese correo." });
                continue;
            }

            var contrasenaTemporal = PasswordGenerator.Generar();
            var usuario = new ApplicationUser
            {
                UserName = fila.Email,
                Email = fila.Email,
                EmailConfirmed = true,
                Nombre = fila.Nombre,
                Cargo = fila.Cargo,
                Aduana = fila.Aduana,
                Subdireccion = fila.Subdireccion,
                Departamento = fila.Departamento,
                Activo = true,
                PrimerInicioSesion = true,
                PasswordTemporal = true,
            };

            var creado = await userManager.CreateAsync(usuario, contrasenaTemporal);
            if (!creado.Succeeded)
            {
                resultado.Errores.Add(new CargaMasivaErrorViewModel
                {
                    Fila = fila.Fila,
                    Email = fila.Email,
                    Motivo = string.Join(" ", creado.Errors.Select(e => e.Description)),
                });
                continue;
            }

            await userManager.AddToRoleAsync(usuario, Roles.Usuario);

            await emailSender.SendAsync(
                fila.Email,
                "Credenciales de acceso — Levantamiento de Necesidades",
                ConstruirCuerpoCredenciales(fila.Email, contrasenaTemporal, esNuevaCuenta: true));

            resultado.Creados.Add(fila.Email);
        }

        logger.LogInformation(
            "Carga masiva de usuarios por {Admin}: {Creados} creados, {Errores} con error.",
            (await userManager.GetUserAsync(User))?.Email, resultado.Creados.Count, resultado.Errores.Count);

        return View("CargaMasivaResultado", resultado);
    }

    private const string EnlaceSistema = "https://centrodesolicitudes.gcslatam.com/";

    private static string ConstruirCuerpoCredenciales(string email, string contrasena, bool esNuevaCuenta)
    {
        var intro = esNuevaCuenta
            ? "Se ha creado una cuenta a su nombre en el Sistema de Levantamiento de Necesidades."
            : "Se ha actualizado la contraseña de su cuenta en el Sistema de Levantamiento de Necesidades.";
        var etiquetaContrasena = esNuevaCuenta ? "Contraseña temporal" : "Contraseña nueva";

        return $"""
            <div style="background:#f4f5f7;padding:32px 16px;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:0 auto;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;">
                <tr>
                  <td style="background:#2563EB;padding:20px 32px;">
                    <span style="color:#ffffff;font-size:16px;font-weight:600;">Global Customs Solutions</span>
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;color:#1f2937;font-size:14px;line-height:1.6;">
                    <p style="margin:0 0 16px;">{intro}</p>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#DBEAFE;border-radius:6px;margin:0 0 20px;">
                      <tr>
                        <td style="padding:16px 20px;">
                          <p style="margin:0 0 6px;color:#1D4ED8;font-size:12px;text-transform:uppercase;letter-spacing:.04em;">Correo</p>
                          <p style="margin:0 0 14px;font-size:15px;font-weight:600;">{email}</p>
                          <p style="margin:0 0 6px;color:#1D4ED8;font-size:12px;text-transform:uppercase;letter-spacing:.04em;">{etiquetaContrasena}</p>
                          <p style="margin:0;font-size:15px;font-weight:600;font-family:Consolas,Menlo,monospace;">{contrasena}</p>
                        </td>
                      </tr>
                    </table>
                    <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
                      <tr>
                        <td style="border-radius:6px;background:#2563EB;">
                          <a href="{EnlaceSistema}" style="display:inline-block;padding:12px 24px;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;">Ingresar al sistema</a>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:0 0 8px;color:#4b5563;">Por seguridad, le recomendamos iniciar sesión y cambiar esta contraseña lo antes posible.</p>
                    <p style="margin:0;color:#6b7280;font-size:12.5px;">Si no esperaba este correo, comuníquese con un administrador del sistema.</p>
                  </td>
                </tr>
              </table>
            </div>
            """;
    }

    private static string? ValidarFila(FilaCargaMasivaUsuario fila, HashSet<string> correosEnArchivo)
    {
        if (string.IsNullOrWhiteSpace(fila.Nombre))
        {
            return "Falta el nombre completo.";
        }
        if (string.IsNullOrWhiteSpace(fila.Email) || !new EmailAddressAttribute().IsValid(fila.Email))
        {
            return "El correo es obligatorio y debe tener un formato válido.";
        }
        if (correosEnArchivo.Contains(fila.Email))
        {
            return "Correo duplicado dentro del mismo archivo.";
        }
        return null;
    }

    [HttpPost("CambiarActivo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarActivo(int id, bool activo)
    {
        var usuario = await userManager.FindByIdAsync(id.ToString());
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Activo = activo;
        await userManager.UpdateAsync(usuario);

        TempData["Mensaje"] = activo ? $"Usuario {usuario.Email} reactivado." : $"Usuario {usuario.Email} desactivado.";
        return RedirectToAction(nameof(Index));
    }
}
