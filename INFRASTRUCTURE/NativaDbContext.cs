using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;

namespace Nativa.Infrastructure
{
    public class NativaDbContext : DbContext
    {
        public NativaDbContext(DbContextOptions<NativaDbContext> options) : base(options) { }

        public DbSet<Sujeto>         Sujetos          { get; set; }
        public DbSet<Activo>         Activos          { get; set; }
        public DbSet<AdjuntoActivo>  AdjuntosActivos  { get; set; }
        public DbSet<CuentaBancaria> CuentasBancarias { get; set; }
        public DbSet<ParametrosPago> ParametrosPagos  { get; set; }
        public DbSet<PlanPago>       PlanesPago       { get; set; }
        public DbSet<PagoMensual>    PagosMensuales   { get; set; }
        public DbSet<OtpSesion>      OtpSesiones      { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Sujeto ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Sujeto>(entity =>
            {
                entity.ToTable("Sujetos");
                entity.HasKey(e => e.Id);

                // FechaCreacion con valor por defecto al hacer INSERT
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // RowVersion para concurrencia optimista (CU17, CU18)
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Índices únicos: no puede haber dos sujetos con la misma cédula o correo
                entity.HasIndex(e => e.Cedula).IsUnique();
                entity.HasIndex(e => e.Correo).IsUnique();
            });

            // ── Activo ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Activo>(entity =>
            {
                entity.ToTable("Activos");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // RowVersion para concurrencia optimista en "Tomar Finca" (CU18)
                entity.Property(e => e.RowVersion)
                    .IsRowVersion();

                // Índice compuesto para la cola FIFO: filtrar por Estado y ordenar por FechaRegistro
                entity.HasIndex(e => new { e.Estado, e.FechaRegistro })
                    .HasDatabaseName("IX_Activos_Estado_FechaRegistro");

                // FK → Sujeto (Dueño): usar IdDueno como FK explícita (evita columna sombra)
                entity.HasOne(a => a.Dueno)
                    .WithMany()
                    .HasForeignKey(a => a.IdDueno)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK → Sujeto (Ingeniero, nullable): null hasta asignación FIFO
                entity.HasOne(a => a.Ingeniero)
                    .WithMany()
                    .HasForeignKey(a => a.IdIngeniero)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── AdjuntoActivo ────────────────────────────────────────────────────
            modelBuilder.Entity<AdjuntoActivo>(entity =>
            {
                entity.ToTable("AdjuntosActivos");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → Activo: usar IdActivo como FK explícita
                entity.HasOne(a => a.Activo)
                    .WithMany()
                    .HasForeignKey(a => a.IdActivo)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── CuentaBancaria ───────────────────────────────────────────────────
            modelBuilder.Entity<CuentaBancaria>(entity =>
            {
                entity.ToTable("CuentasBancarias");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → Sujeto (Dueño): usar IdDueno como FK explícita
                entity.HasOne(c => c.Dueno)
                    .WithMany()
                    .HasForeignKey(c => c.IdDueno)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── ParametrosPago ───────────────────────────────────────────────────
            modelBuilder.Entity<ParametrosPago>(entity =>
            {
                entity.ToTable("ParametrosPagos");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → Sujeto (Admin creador): usar CreadoPor como FK explícita
                entity.HasOne(p => p.Creador)
                    .WithMany()
                    .HasForeignKey(p => p.CreadoPor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── PlanPago ─────────────────────────────────────────────────────────
            modelBuilder.Entity<PlanPago>(entity =>
            {
                entity.ToTable("PlanesPago");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → Activo: usar IdActivo como FK explícita
                entity.HasOne(p => p.Activo)
                    .WithMany()
                    .HasForeignKey(p => p.IdActivo)
                    .OnDelete(DeleteBehavior.Restrict);

                // FK → Sujeto (Ingeniero que activó el plan): usar IdIngeniero como FK explícita
                entity.HasOne(p => p.Ingeniero)
                    .WithMany()
                    .HasForeignKey(p => p.IdIngeniero)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── PagoMensual ──────────────────────────────────────────────────────
            modelBuilder.Entity<PagoMensual>(entity =>
            {
                entity.ToTable("PagosMensuales");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → PlanPago: usar IdPlan como FK explícita
                entity.HasOne(p => p.Plan)
                    .WithMany()
                    .HasForeignKey(p => p.IdPlan)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── OtpSesion ────────────────────────────────────────────────────────
            modelBuilder.Entity<OtpSesion>(entity =>
            {
                entity.ToTable("OtpSesiones");

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // FK → Sujeto: usar IdSujeto como FK explícita
                entity.HasOne(o => o.Sujeto)
                    .WithMany()
                    .HasForeignKey(o => o.IdSujeto)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
