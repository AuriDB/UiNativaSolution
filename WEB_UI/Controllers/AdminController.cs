using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

/// <summary>
/// Panel de Administración: gestión de usuarios (CU05-08) y parámetros de pago (CU27-28).
/// También permite activar planes de pago para fincas aprobadas (CU24 desde Admin).
/// </summary>
[Authorize(Roles = "Admin")]
[Route("Admin")]
public class AdminController : Controller
{
    private readonly NativaDbContext       _db;
    private readonly PlanActivationService _planService;
    private readonly EmailService          _email;

    private int UserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public AdminController(NativaDbContext db, PlanActivationService planService, EmailService email)
    {
        _db          = db;
        _planService = planService;
        _email       = email;
    }

    // ── Vistas ────────────────────────────────────────────────────────────────

    // GET /Admin — lista de usuarios
    [HttpGet("")]
    public IActionResult Index() => View();

    // GET /Admin/Parametros — parámetros de pago PSA
    [HttpGet("Parametros")]
    public IActionResult Parametros() => View();

    // GET /Admin/FincasAprobadas — vista para activar planes
    [HttpGet("FincasAprobadas")]
    public IActionResult FincasAprobadas() => View();

    // ── Endpoints AJAX: Usuarios ──────────────────────────────────────────────

    // GET /Admin/UsuariosData — JSON todos los usuarios (excepto el Admin autenticado)
    [HttpGet("UsuariosData")]
    public async Task<IActionResult> UsuariosData()
    {
        var usuarios = await _db.Sujetos
            .OrderBy(s => s.Rol)
            .ThenBy(s => s.Nombre)
            .Select(s => new
            {
                s.Id,
                s.Nombre,
                s.Correo,
                s.Cedula,
                rol         = s.Rol.ToString(),
                estado      = s.Estado.ToString(),
                estadoColor = EstadoColor(s.Estado),
                s.Estado,
                esSelf      = s.Id == UserId,
                fechaCreacion = s.FechaCreacion.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Json(usuarios);
    }

    // POST /Admin/CrearIngeniero — CU05
    [HttpPost("CrearIngeniero")]
    public async Task<IActionResult> CrearIngeniero([FromBody] CrearIngenieroRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre) ||
            string.IsNullOrWhiteSpace(req.Cedula) ||
            string.IsNullOrWhiteSpace(req.Correo) ||
            string.IsNullOrWhiteSpace(req.Contrasena))
            return Json(new { success = false, message = "Todos los campos son obligatorios." });

        if (await _db.Sujetos.AnyAsync(s => s.Cedula == req.Cedula.Trim()))
            return Json(new { success = false, message = "Ya existe un usuario con esa cédula." });

        if (await _db.Sujetos.AnyAsync(s => s.Correo == req.Correo.Trim().ToLower()))
            return Json(new { success = false, message = "Ya existe un usuario con ese correo." });

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Contrasena, workFactor: 12);

        var ingeniero = new Sujeto
        {
            Nombre       = req.Nombre.Trim(),
            Cedula       = req.Cedula.Trim(),
            Correo       = req.Correo.Trim().ToLower(),
            PasswordHash = hash,
            Rol          = RolEnum.Ingeniero,
            Estado       = EstadoSujetoEnum.Activo   // Admin activa directamente
        };

        _db.Sujetos.Add(ingeniero);
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = $"Ingeniero {ingeniero.Nombre} creado correctamente." });
    }

    // POST /Admin/EditarUsuario — CU06
    [HttpPost("EditarUsuario")]
    public async Task<IActionResult> EditarUsuario([FromBody] EditarUsuarioRequest req)
    {
        var sujeto = await _db.Sujetos.FindAsync(req.Id);

        if (sujeto == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (sujeto.Id == UserId)
            return Json(new { success = false, message = "No puedes editarte a ti mismo desde aquí." });

        if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Correo))
            return Json(new { success = false, message = "Nombre y correo son obligatorios." });

        var correo = req.Correo.Trim().ToLower();

        // Verificar unicidad del correo (ignorar el propio)
        if (await _db.Sujetos.AnyAsync(s => s.Correo == correo && s.Id != req.Id))
            return Json(new { success = false, message = "Ese correo ya está en uso por otro usuario." });

        sujeto.Nombre = req.Nombre.Trim();
        sujeto.Correo = correo;

        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Usuario actualizado correctamente." });
    }

    // POST /Admin/InactivarUsuario — CU07
    [HttpPost("InactivarUsuario")]
    public async Task<IActionResult> InactivarUsuario([FromBody] InactivarUsuarioRequest req)
    {
        var sujeto = await _db.Sujetos.FindAsync(req.Id);

        if (sujeto == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (sujeto.Id == UserId)
            return Json(new { success = false, message = "No puedes inactivarte a ti mismo." });

        if (sujeto.Estado == EstadoSujetoEnum.Inactivo)
            return Json(new { success = false, message = "El usuario ya está inactivo." });

        sujeto.Estado = EstadoSujetoEnum.Inactivo;
        await _db.SaveChangesAsync();

        // N13 — notificar al usuario que su cuenta fue inactivada
        try { await _email.EnviarCuentaInactivadaAsync(sujeto.Correo!, sujeto.Nombre!); } catch { }

        return Json(new { success = true, message = $"Usuario {sujeto.Nombre} inactivado." });
    }

    // POST /Admin/ReactivarUsuario — reactivar Inactivo → Activo
    [HttpPost("ReactivarUsuario")]
    public async Task<IActionResult> ReactivarUsuario([FromBody] InactivarUsuarioRequest req)
    {
        var sujeto = await _db.Sujetos.FindAsync(req.Id);

        if (sujeto == null)
            return Json(new { success = false, message = "Usuario no encontrado." });

        if (sujeto.Estado == EstadoSujetoEnum.Activo)
            return Json(new { success = false, message = "El usuario ya está activo." });

        sujeto.Estado = EstadoSujetoEnum.Activo;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = $"Usuario {sujeto.Nombre} reactivado." });
    }

    // ── Endpoints AJAX: Parámetros ────────────────────────────────────────────

    // GET /Admin/ParametrosData — JSON historial de parámetros
    [HttpGet("ParametrosData")]
    public async Task<IActionResult> ParametrosData()
    {
        var parametros = await _db.ParametrosPagos
            .Include(p => p.Creador)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new
            {
                p.Id,
                p.PrecioBase,
                p.PctVegetacion,
                p.PctHidrologia,
                p.PctNacional,
                p.PctTopografia,
                p.Tope,
                p.Vigente,
                creadoPor     = p.Creador!.Nombre,
                fechaCreacion = p.FechaCreacion.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(parametros);
    }

    // POST /Admin/CrearParametros — CU27 (Opción A/B: nuevos parámetros = dejar anteriores en Vigente=false)
    [HttpPost("CrearParametros")]
    public async Task<IActionResult> CrearParametros([FromBody] CrearParametrosRequest req)
    {
        if (req.PrecioBase <= 0)
            return Json(new { success = false, message = "El precio base debe ser mayor a 0." });

        if (req.Tope <= 0 || req.Tope > 1)
            return Json(new { success = false, message = "El tope debe estar entre 0 y 1 (ej: 0.50 = 50%)." });

        // Desactivar parámetros vigentes anteriores
        var anteriores = await _db.ParametrosPagos.Where(p => p.Vigente).ToListAsync();
        foreach (var ant in anteriores)
            ant.Vigente = false;

        var nuevos = new ParametrosPago
        {
            PrecioBase    = req.PrecioBase,
            PctVegetacion = req.PctVegetacion,
            PctHidrologia = req.PctHidrologia,
            PctNacional   = req.PctNacional,
            PctTopografia = req.PctTopografia,
            Tope          = req.Tope,
            Vigente       = true,
            CreadoPor     = UserId
        };

        _db.ParametrosPagos.Add(nuevos);
        await _db.SaveChangesAsync();

        // N14 — notificar a dueños con planes activos que los parámetros cambiaron
        var duenosConPlan = await _db.PlanesPago
            .Include(p => p.Activo).ThenInclude(a => a!.Dueno)
            .Select(p => new { p.Activo!.Dueno!.Correo, p.Activo.Dueno.Nombre })
            .Distinct()
            .ToListAsync();

        foreach (var d in duenosConPlan)
        {
            if (d.Correo == null) continue;
            try { await _email.EnviarParametrosActualizadosAsync(d.Correo, d.Nombre!); } catch { }
        }

        return Json(new { success = true,
            message = "Parámetros de pago configurados. El nuevo set es ahora el vigente." });
    }

    // ── Endpoint: Activar Plan ────────────────────────────────────────────────

    // GET /Admin/FincasAprobadasData — fincas Aprobadas sin plan (para activar desde Admin)
    [HttpGet("FincasAprobadasData")]
    public async Task<IActionResult> FincasAprobadasData()
    {
        var planActivoIds = await _db.PlanesPago.Select(p => p.IdActivo).ToListAsync();

        var fincas = await _db.Activos
            .Include(a => a.Dueno)
            .Include(a => a.Ingeniero)
            .Where(a => a.Estado == EstadoActivoEnum.Aprobada && !planActivoIds.Contains(a.Id))
            .OrderBy(a => a.FechaRegistro)
            .Select(a => new
            {
                a.Id,
                a.Hectareas,
                nombreDueno    = a.Dueno!.Nombre,
                nombreIngeniero = a.Ingeniero != null ? a.Ingeniero.Nombre : "—",
                fechaRegistro  = a.FechaRegistro.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Json(fincas);
    }

    // POST /Admin/ActivarPlan — activar plan de pago
    [HttpPost("ActivarPlan")]
    public async Task<IActionResult> ActivarPlan([FromBody] ActivarPlanRequest req)
    {
        var (ok, mensaje, planId) = await _planService.ActivarAsync(req.ActivoId, UserId);
        return Json(new { success = ok, message = mensaje, planId });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static string EstadoColor(EstadoSujetoEnum estado) => estado switch
    {
        EstadoSujetoEnum.Activo    => "success",
        EstadoSujetoEnum.Inactivo  => "secondary",
        EstadoSujetoEnum.Bloqueado => "danger",
        _                          => "light"
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CrearIngenieroRequest(string Nombre, string Cedula, string Correo, string Contrasena);
public record EditarUsuarioRequest(int Id, string Nombre, string Correo);
public record InactivarUsuarioRequest(int Id);
public record CrearParametrosRequest(
    decimal PrecioBase, decimal PctVegetacion, decimal PctHidrologia,
    decimal PctNacional, decimal PctTopografia, decimal Tope);
public record ActivarPlanRequest(int ActivoId);
