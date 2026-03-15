using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Data;

/// <summary>
/// Siembra usuarios de prueba en la base de datos si no existen.
/// Ejecutado en el startup. Seguro para correr múltiples veces (idempotente).
/// </summary>
public static class DataSeeder
{
    // Cédula reservada para el superusuario root — NUNCA se puede inactivar.
    public const string RootCedula = "0-0000-0001";

    private static readonly (string Cedula, string Nombre, string Correo, string Pass, RolEnum Rol)[] Usuarios =
    [
        (RootCedula,   "Root Nativa",          "root@nativa.cr",  "Root@1234",  RolEnum.Admin),
        ("9-0001-0001", "Admin Prueba",         "admin@nativa.cr", "Admin@1234", RolEnum.Admin),
        ("9-0002-0001", "Ingeniero Prueba",     "ing@nativa.cr",   "Ing@1234",   RolEnum.Ingeniero),
        ("9-0003-0001", "Dueño Prueba",         "dueno@nativa.cr", "Dueno@1234", RolEnum.Dueno),
    ];

    public static async Task SeedAsync(NativaDbContext db)
    {
        foreach (var (cedula, nombre, correo, pass, rol) in Usuarios)
        {
            if (await db.Sujetos.AnyAsync(s => s.Cedula == cedula))
                continue;

            db.Sujetos.Add(new Sujeto
            {
                Cedula        = cedula,
                Nombre        = nombre,
                Correo        = correo,
                PasswordHash  = BCrypt.Net.BCrypt.HashPassword(pass, workFactor: 12),
                Rol           = rol,
                Estado        = EstadoSujetoEnum.Activo,
                FechaCreacion = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
