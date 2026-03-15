using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using WEB_UI.Data;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Services;

public class AdminService
{
    private readonly NativaDbContext   _db;
    private readonly EmailService      _email;
    private readonly CalculadoraService _calc;

    public AdminService(NativaDbContext db, EmailService email, CalculadoraService calc)
    {
        _db    = db;
        _email = email;
        _calc  = calc;
    }

    // ── CU08 Ver Usuarios ────────────────────────────────────────────────────
    public async Task<List<object>> ListarUsuariosAsync()
    {
        return await _db.Sujetos
            .OrderBy(s => s.Rol).ThenBy(s => s.Nombre)
            .Select(s => (object)new
            {
                s.Id,
                s.Cedula,
                s.Nombre,
                s.Correo,
                Rol    = s.Rol.ToString(),
                Estado = s.Estado.ToString(),
                s.FechaCreacion
            })
            .ToListAsync();
    }

    // ── CU05 Crear Ingeniero ─────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> CrearIngenieroAsync(
        string nombre, string apellido1, string apellido2,
        string cedula, string correo, string contrasena)
    {
        // Normalizar cédula
        var cedulaStripped = cedula.Replace("-", "").Trim();
        if (cedulaStripped.Length != 9 || !cedulaStripped.All(char.IsDigit))
            return (false, "Cédula inválida. Debe tener 9 dígitos.");

        var cedulaFormateada = $"{cedulaStripped[0]}-{cedulaStripped[1..5]}-{cedulaStripped[5..]}";

        if (await _db.Sujetos.AnyAsync(s => s.Cedula == cedulaFormateada))
            return (false, "Ya existe un usuario con esa cédula.");
        if (await _db.Sujetos.AnyAsync(s => s.Correo == correo))
            return (false, "Ya existe un usuario con ese correo.");

        var nombreCompleto = $"{nombre.Trim()} {apellido1.Trim()} {apellido2.Trim()}";
        var hash = BCrypt.Net.BCrypt.HashPassword(contrasena, workFactor: 12);

        _db.Sujetos.Add(new Sujeto
        {
            Cedula        = cedulaFormateada,
            Nombre        = nombreCompleto,
            Correo        = correo.Trim().ToLower(),
            PasswordHash  = hash,
            Rol           = RolEnum.Ingeniero,
            Estado        = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, "Ingeniero creado correctamente.");
    }

    // ── CU06 Editar Usuario ──────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> EditarUsuarioAsync(
        int id, string nombre, string apellido1, string apellido2)
    {
        var sujeto = await _db.Sujetos.FindAsync(id);
        if (sujeto is null) return (false, "Usuario no encontrado.");

        sujeto.Nombre = $"{nombre.Trim()} {apellido1.Trim()} {apellido2.Trim()}";
        await _db.SaveChangesAsync();
        return (true, "Usuario actualizado correctamente.");
    }

    // ── Reactivar Usuario Bloqueado ──────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> ReactivarAsync(int id)
    {
        var sujeto = await _db.Sujetos.FindAsync(id);
        if (sujeto is null) return (false, "Usuario no encontrado.");
        if (sujeto.Estado == EstadoSujetoEnum.Activo)
            return (false, "El usuario ya está activo.");

        sujeto.Estado = EstadoSujetoEnum.Activo;
        await _db.SaveChangesAsync();
        return (true, "Usuario reactivado correctamente.");
    }

    // ── CU07 Inactivar Usuario ────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> InactivarAsync(int id, int adminId)
    {
        if (id == adminId) return (false, "No puedes inactivar tu propia cuenta.");

        var sujeto = await _db.Sujetos.FindAsync(id);
        if (sujeto is null) return (false, "Usuario no encontrado.");
        if (sujeto.Estado == EstadoSujetoEnum.Inactivo)
            return (false, "El usuario ya está inactivo.");

        // Cascade: si es Ingeniero, devolver fincas EnRevision a FIFO
        if (sujeto.Rol == RolEnum.Ingeniero)
        {
            var fincasEnRevision = await _db.Activos
                .Where(a => a.IdIngeniero == id && a.Estado == EstadoActivoEnum.EnRevision)
                .ToListAsync();

            foreach (var finca in fincasEnRevision)
            {
                finca.Estado      = EstadoActivoEnum.Pendiente;
                finca.IdIngeniero = null;
            }
        }

        sujeto.Estado = EstadoSujetoEnum.Inactivo;
        await _db.SaveChangesAsync();

        // N13
        _ = _email.EnviarGenericoAsync(sujeto.Correo,
            "Tu cuenta ha sido inactivada — Sistema Nativa",
            $"<p>Hola <strong>{sujeto.Nombre}</strong>,</p>" +
            $"<p>Tu cuenta en el Sistema Nativa ha sido <strong>inactivada</strong> por un administrador.</p>" +
            $"<p>Si crees que esto es un error, contacta al soporte del sistema.</p>");

        return (true, "Usuario inactivado correctamente.");
    }

    // ── CU28 Ver Parámetros ──────────────────────────────────────────────────
    public async Task<List<object>> ListarParametrosAsync()
    {
        return await _db.ParametrosPago
            .OrderByDescending(p => p.Id)
            .Select(p => (object)new
            {
                p.Id,
                p.PrecioBase,
                p.PctVegetacion,
                p.PctHidrologia,
                p.PctNacional,
                p.PctTopografia,
                p.Tope,
                p.Vigente,
                FechaCreacion = p.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();
    }

    public async Task<ParametrosPago?> GetVigenteAsync()
        => await _db.ParametrosPago
            .Where(p => p.Vigente)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

    // ── CU27 Crear Parámetros ────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> CrearParametrosAsync(
        decimal precioBase, decimal pctVeg, decimal pctHid,
        decimal pctNac, decimal pctTop, decimal tope,
        string opcion, int adminId)
    {
        if (opcion != "A" && opcion != "B")
            return (false, "Opción inválida. Debe ser A o B.");

        // Desactivar vigente actual
        var vigenteActual = await _db.ParametrosPago.Where(p => p.Vigente).ToListAsync();
        foreach (var v in vigenteActual) v.Vigente = false;

        var nuevos = new ParametrosPago
        {
            PrecioBase     = precioBase,
            PctVegetacion  = pctVeg,
            PctHidrologia  = pctHid,
            PctNacional    = pctNac,
            PctTopografia  = pctTop,
            Tope           = tope,
            Vigente        = true,
            FechaCreacion  = DateTime.UtcNow,
            CreadoPor      = adminId
        };
        _db.ParametrosPago.Add(nuevos);
        await _db.SaveChangesAsync();

        // Opción B: recalcular PagosMensuales Pendientes
        if (opcion == "B")
            await RecalcularOpcionBAsync(nuevos);

        return (true, $"Parámetros creados (Opción {opcion}). " +
                       (opcion == "B" ? "Pagos pendientes recalculados." : "Solo afecta fincas nuevas."));
    }

    private async Task RecalcularOpcionBAsync(ParametrosPago nuevos)
    {
        var planesActivos = await _db.PlanesPago
            .Include(pl => pl.Activo)
                .ThenInclude(a => a.Dueno)
            .Include(pl => pl.Pagos)
            .Where(pl => pl.Pagos.Any(p => p.Estado == EstadoPagoEnum.Pendiente))
            .ToListAsync();

        foreach (var plan in planesActivos)
        {
            var nuevoMonto = _calc.Calcular(plan.Activo, nuevos);
            var pagosPendientes = plan.Pagos.Where(p => p.Estado == EstadoPagoEnum.Pendiente).ToList();

            foreach (var pago in pagosPendientes)
                pago.Monto = nuevoMonto;

            plan.MontoMensual = nuevoMonto;

            // N14
            _ = _email.EnviarGenericoAsync(plan.Activo.Dueno.Correo,
                "Actualización en tu plan de pagos — Sistema Nativa",
                $"<p>Hola <strong>{plan.Activo.Dueno.Nombre}</strong>,</p>" +
                $"<p>Los parámetros de pago del programa han sido actualizados.</p>" +
                $"<p>Tu plan de pagos para la finca ID #{plan.IdActivo} fue recalculado.</p>" +
                $"<p><strong>Nuevo monto mensual: ₡{nuevoMonto:N2}</strong></p>" +
                $"<p>Los pagos ya ejecutados no fueron modificados.</p>");
        }

        await _db.SaveChangesAsync();
    }
}
