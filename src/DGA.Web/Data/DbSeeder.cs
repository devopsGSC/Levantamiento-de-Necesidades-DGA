using DGA.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Data;

/// <summary>
/// Crea el primer usuario Administrador si todavía no existe ninguno. Las credenciales
/// NUNCA se hardcodean ni viven en appsettings.json — se leen de configuración (User
/// Secrets en Development, variables de entorno en producción). Si no están configuradas,
/// el seed simplemente no hace nada (no rompe el arranque de la app).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services, ILogger logger)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        if (!await roleManager.RoleExistsAsync(Roles.Administrador))
        {
            // Los roles ya se siembran en database/01_schema_dga.sql; esto es solo un
            // resguardo por si el seed se corre contra una base que no pasó por ese script.
            await roleManager.CreateAsync(new ApplicationRole(Roles.Administrador));
        }
        if (!await roleManager.RoleExistsAsync(Roles.Usuario))
        {
            await roleManager.CreateAsync(new ApplicationRole(Roles.Usuario));
        }

        var yaHayAdmin = await userManager.GetUsersInRoleAsync(Roles.Administrador);
        if (yaHayAdmin.Count > 0)
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No hay ningún Administrador en la base y SeedAdmin:Email / SeedAdmin:Password " +
                "no están configurados (User Secrets o variables de entorno). No se creó ningún " +
                "usuario administrador — la aplicación no tendrá a nadie con acceso al panel Admin.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = "Administrador",
            Activo = true,
            PrimerInicioSesion = true,
            PasswordTemporal = false,
            EsAdminPrincipal = true,
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errores = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("No se pudo crear el usuario administrador semilla: {Errores}", errores);
            return;
        }

        await userManager.AddToRoleAsync(admin, Roles.Administrador);
        logger.LogInformation("Usuario administrador semilla creado: {Email}", email);
    }
}
