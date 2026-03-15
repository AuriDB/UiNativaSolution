using Microsoft.EntityFrameworkCore;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Data;

public class NativaDbContext : DbContext
{
    public NativaDbContext(DbContextOptions<NativaDbContext> options) : base(options) { }

    public DbSet<Sujeto> Sujetos => Set<Sujeto>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<AdjuntoActivo> AdjuntosActivos => Set<AdjuntoActivo>();
    public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
    public DbSet<ParametrosPago> ParametrosPago => Set<ParametrosPago>();
    public DbSet<PlanPago> PlanesPago => Set<PlanPago>();
    public DbSet<PagoMensual> PagosMensuales => Set<PagoMensual>();
    public DbSet<OtpSesion> OtpSesiones => Set<OtpSesion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Sujeto ────────────────────────────────────────────────
        modelBuilder.Entity<Sujeto>(e =>
        {
            e.HasIndex(s => s.Cedula).IsUnique();
            e.HasIndex(s => s.Correo).IsUnique();
            e.Property(s => s.RowVersion).IsRowVersion();
            e.Property(s => s.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── Activo ────────────────────────────────────────────────
        modelBuilder.Entity<Activo>(e =>
        {
            e.HasIndex(a => new { a.Estado, a.FechaRegistro });

            e.HasOne(a => a.Dueno)
             .WithMany(s => s.Fincas)
             .HasForeignKey(a => a.IdDueno)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Ingeniero)
             .WithMany()
             .HasForeignKey(a => a.IdIngeniero)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);

            e.Property(a => a.RowVersion).IsRowVersion();
            e.Property(a => a.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── AdjuntoActivo ─────────────────────────────────────────
        modelBuilder.Entity<AdjuntoActivo>(e =>
        {
            e.HasOne(a => a.Activo)
             .WithMany(ac => ac.Adjuntos)
             .HasForeignKey(a => a.IdActivo)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(a => a.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── CuentaBancaria ────────────────────────────────────────
        modelBuilder.Entity<CuentaBancaria>(e =>
        {
            e.HasOne(c => c.Dueno)
             .WithMany(s => s.CuentasBancarias)
             .HasForeignKey(c => c.IdDueno)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(c => c.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── ParametrosPago ────────────────────────────────────────
        modelBuilder.Entity<ParametrosPago>(e =>
        {
            e.HasOne(p => p.Admin)
             .WithMany()
             .HasForeignKey(p => p.CreadoPor)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(p => p.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── PlanPago ──────────────────────────────────────────────
        modelBuilder.Entity<PlanPago>(e =>
        {
            e.HasOne(p => p.Activo)
             .WithMany(a => a.Planes)
             .HasForeignKey(p => p.IdActivo)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(p => p.Ingeniero)
             .WithMany()
             .HasForeignKey(p => p.IdIngeniero)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(p => p.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── PagoMensual ───────────────────────────────────────────
        modelBuilder.Entity<PagoMensual>(e =>
        {
            e.HasOne(p => p.Plan)
             .WithMany(pl => pl.Pagos)
             .HasForeignKey(p => p.IdPlan)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(p => p.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });

        // ── OtpSesion ─────────────────────────────────────────────
        modelBuilder.Entity<OtpSesion>(e =>
        {
            e.HasOne(o => o.Sujeto)
             .WithMany(s => s.OtpSesiones)
             .HasForeignKey(o => o.IdSujeto)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(o => o.FechaCreacion)
             .HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
