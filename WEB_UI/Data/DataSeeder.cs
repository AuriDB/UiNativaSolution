using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;

namespace WEB_UI.Data;

/// <summary>
/// Siembra usuarios de prueba al arrancar la aplicación, solo si la tabla Sujetos está vacía.
/// Usuarios creados:
///   root@nativa.cr   — Admin (super admin, primer usuario del sistema)
///   admin@nativa.cr  — Admin (administrador operativo)
///   ing@nativa.cr    — Ingeniero
///   dueno@nativa.cr  — Dueño
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<NativaDbContext>();

        // Aplicar migraciones pendientes (seguro en Development)
        if (app.Environment.IsDevelopment())
            await db.Database.MigrateAsync();

        // Solo sembrar si no hay usuarios registrados
        if (await db.Sujetos.AnyAsync())
            return;

        var usuarios = new List<Sujeto>
        {
            // ── 1. Root / Super Admin ──────────────────────────────────────
            new()
            {
                Cedula       = "0-0000-0001",
                Nombre       = "Root Admin",
                Correo       = "root@nativa.cr",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Root@Nativa1!", workFactor: 12),
                Rol          = RolEnum.Admin,
                Estado       = EstadoSujetoEnum.Activo
            },

            // ── 2. Admin operativo ─────────────────────────────────────────
            new()
            {
                Cedula       = "0-0000-0002",
                Nombre       = "Admin Nativa",
                Correo       = "admin@nativa.cr",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@Nativa1!", workFactor: 12),
                Rol          = RolEnum.Admin,
                Estado       = EstadoSujetoEnum.Activo
            },

            // ── 3. Ingeniero evaluador ─────────────────────────────────────
            new()
            {
                Cedula       = "1-0001-0001",
                Nombre       = "Ing. Carlos Mora",
                Correo       = "ing@nativa.cr",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Ing@Nativa1!", workFactor: 12),
                Rol          = RolEnum.Ingeniero,
                Estado       = EstadoSujetoEnum.Activo
            },

            // ── 4. Dueño de finca ──────────────────────────────────────────
            // Estado=Activo directo para omitir OTP en pruebas
            new()
            {
                Cedula       = "2-0002-0002",
                Nombre       = "María Rodríguez",
                Correo       = "dueno@nativa.cr",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Dueno@Nativa1!", workFactor: 12),
                Rol          = RolEnum.Dueno,
                Estado       = EstadoSujetoEnum.Activo
            }
        };

        db.Sujetos.AddRange(usuarios);
        await db.SaveChangesAsync();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "DataSeeder: 4 usuarios de prueba sembrados correctamente (DB: {DB}).",
            db.Database.GetDbConnection().Database);
    }
}
