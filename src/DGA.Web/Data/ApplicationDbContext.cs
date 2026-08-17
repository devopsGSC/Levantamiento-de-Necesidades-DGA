using DGA.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Data;

/// <summary>
/// Mapea contra el esquema SQL Server creado manualmente en database/01_schema_dga.sql.
/// No se usan migraciones de EF Core: el esquema es dueño de la base de datos, este
/// DbContext solo lo describe. No llamar Database.Migrate() ni Database.EnsureCreated().
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<TipoAduana> TiposAduana => Set<TipoAduana>();
    public DbSet<Aduana> Aduanas => Set<Aduana>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Componente> Componentes => Set<Componente>();
    public DbSet<Subcomponente> Subcomponentes => Set<Subcomponente>();
    public DbSet<Elemento> Elementos => Set<Elemento>();
    public DbSet<Detalle> Detalles => Set<Detalle>();
    public DbSet<Prioridad> Prioridades => Set<Prioridad>();
    public DbSet<EstadoSolicitud> EstadosSolicitud => Set<EstadoSolicitud>();
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();
    public DbSet<SolicitudItem> SolicitudItems => Set<SolicitudItem>();
    public DbSet<SolicitudItemFotografia> SolicitudItemFotografias => Set<SolicitudItemFotografia>();
    public DbSet<SolicitudHistorial> SolicitudHistorial => Set<SolicitudHistorial>();
    public DbSet<ReporteSemanal> ReportesSemanales => Set<ReporteSemanal>();
    public DbSet<ConfiguracionSistema> ConfiguracionSistema => Set<ConfiguracionSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity ya mapea AspNetUsers/AspNetRoles/etc. por convención con esos mismos
        // nombres de tabla, que coinciden exactamente con database/01_schema_dga.sql.

        modelBuilder.Entity<Aduana>()
            .HasOne(a => a.TipoAduana)
            .WithMany(t => t.Aduanas)
            .HasForeignKey(a => a.TipoAduanaId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Subcomponente>()
            .HasOne(s => s.Componente)
            .WithMany(c => c.Subcomponentes)
            .HasForeignKey(s => s.ComponenteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Elemento>()
            .HasOne(e => e.Subcomponente)
            .WithMany(s => s.Elementos)
            .HasForeignKey(e => e.SubcomponenteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Detalle>()
            .HasOne(d => d.Elemento)
            .WithMany(e => e.Detalles)
            .HasForeignKey(d => d.ElementoId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Solicitud>(entity =>
        {
            entity.HasIndex(s => s.IdSolicitud).IsUnique();

            entity.HasOne(s => s.Usuario)
                .WithMany(u => u.SolicitudesCreadas)
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.AdminRevisor)
                .WithMany(u => u.SolicitudesRevisadas)
                .HasForeignKey(s => s.AdminRevisorId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.Cargo)
                .WithMany()
                .HasForeignKey(s => s.CargoId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.Aduana)
                .WithMany()
                .HasForeignKey(s => s.AduanaId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(s => s.Estado)
                .WithMany()
                .HasForeignKey(s => s.EstadoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Sin filtro global de IsDeleted a propósito: choca con las relaciones
            // requeridas de SolicitudItem/SolicitudHistorial (EF Core warning 10622).
            // El filtrado de solicitudes descartadas se hace explícito con
            // .Where(s => !s.IsDeleted) en las consultas de los controladores/servicios.
        });

        modelBuilder.Entity<SolicitudItem>(entity =>
        {
            entity.HasOne(i => i.Solicitud)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Componente)
                .WithMany()
                .HasForeignKey(i => i.ComponenteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(i => i.Subcomponente)
                .WithMany()
                .HasForeignKey(i => i.SubcomponenteId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(i => i.Elemento)
                .WithMany()
                .HasForeignKey(i => i.ElementoId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(i => i.Detalle)
                .WithMany()
                .HasForeignKey(i => i.DetalleId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(i => i.Prioridad)
                .WithMany()
                .HasForeignKey(i => i.PrioridadId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SolicitudItemFotografia>()
            .HasOne(f => f.SolicitudItem)
            .WithMany(i => i.Fotografias)
            .HasForeignKey(f => f.SolicitudItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SolicitudHistorial>(entity =>
        {
            entity.HasOne(h => h.Solicitud)
                .WithMany(s => s.Historial)
                .HasForeignKey(h => h.SolicitudId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.EstadoAnterior)
                .WithMany()
                .HasForeignKey(h => h.EstadoAnteriorId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(h => h.EstadoNuevo)
                .WithMany()
                .HasForeignKey(h => h.EstadoNuevoId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(h => h.UsuarioCambio)
                .WithMany()
                .HasForeignKey(h => h.UsuarioCambioId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ReporteSemanal>()
            .HasOne(r => r.GeneradoPorUsuario)
            .WithMany()
            .HasForeignKey(r => r.GeneradoPorUsuarioId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Aduana>().Ignore(a => a.NombreCompleto);
    }
}
