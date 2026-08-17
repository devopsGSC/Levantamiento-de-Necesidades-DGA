using DGA.Web.Data;
using DGA.Web.Data.Entities;
using DGA.Web.Options;
using DGA.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

// Licencia Community de QuestPDF: gratuita para organizaciones con ingresos anuales
// menores a 1M USD (aplica a DGA como entidad de gobierno sin fines de lucro).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Requiere autenticación por defecto en toda la app ("acceso restringido para personal
// autorizado"); las acciones públicas (login, recuperar contraseña) llevan [AllowAnonymous].
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});
if (builder.Environment.IsDevelopment())
{
    // Recompila las vistas Razor al vuelo en dev — evita rebuild+restart por cada cambio de .cshtml.
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.Configure<ArchivosOptions>(builder.Configuration.GetSection(ArchivosOptions.SectionName));
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<SolicitudIdGenerator>();
builder.Services.AddScoped<SolicitudExportService>();
builder.Services.AddScoped<ReporteSemanalService>();
builder.Services.AddScoped<CargaMasivaUsuariosService>();
builder.Services.AddHostedService<ReporteSemanalBackgroundService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
var smtpConfigurado = builder.Configuration.GetSection(SmtpOptions.SectionName)["User"];
if (!string.IsNullOrWhiteSpace(smtpConfigurado))
{
    // SMTP real configurado (User Secrets / variables de entorno) — se usa para enviar
    // los correos de recuperación de contraseña de verdad.
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    // Sin SMTP configurado: cae a solo loguear el correo (útil en dev sin credenciales).
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
}

// El esquema ya existe en SQL Server (database/01_schema_dga.sql) — este DbContext solo
// lo describe. No se usan migraciones de EF Core en este proyecto.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Política de contraseña del Manual de Uso: mínimo 12 caracteres, mayúsculas,
        // minúsculas, números y carácter especial.
        options.Password.RequiredLength = 12;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // No hay auto-registro (regla de negocio): los usuarios los crea el admin ya
        // confirmados, no hace falta flujo de confirmación de correo.
        options.SignIn.RequireConfirmedAccount = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    await DbSeeder.SeedAdminAsync(scope.ServiceProvider, logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
