using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

/// <summary>
/// Gestiona la cola FIFO de fincas pendientes y el dictamen del Ingeniero.
/// Concurrencia optimista con RowVersion para la acción "Tomar Finca" (CU18).
/// CU24: ActivarPlan — el ingeniero activa el plan de pago tras aprobar una finca.
/// </summary>
[Authorize(Roles = "Ingeniero")]
[Route("Ingeniero")]
public class EngineerController : Controller
{
    private readonly NativaDbContext       _db;
    private readonly PlanActivationService _planService;
    private readonly EmailService          _email;

    // Id del Ingeniero autenticado
    private int UserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public EngineerController(NativaDbContext db, PlanActivationService planService, EmailService email)
    {
        _db          = db;
        _planService = planService;
        _email       = email;
    }

    // ── Vistas ─────────────────────────────────────────────────────────────────

    // GET /Ingeniero — cola FIFO
    [HttpGet("")]
    public IActionResult Cola() => View();

    // GET /Ingeniero/MisAsignadas — fincas en revisión asignadas al ingeniero
    [HttpGet("MisAsignadas")]
    public IActionResult MisAsignadas() => View();

    // GET /Ingeniero/Evaluar/{id} — formulario de dictamen
    [HttpGet("Evaluar/{id:int}")]
    public async Task<IActionResult> Evaluar(int id)
    {
        var activo = await _db.Activos
            .Include(a => a.Dueno)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activo == null)
            return NotFound();

        // Solo el ingeniero asignado puede evaluar
        if (activo.IdIngeniero != UserId)
            return Forbid();

        // Solo fincas en revisión son evaluables
        if (activo.Estado != EstadoActivoEnum.EnRevision)
            return Forbid();

        return View(activo);
    }

    // ── Endpoints AJAX (JSON) ──────────────────────────────────────────────────

    // GET /Ingeniero/ColaFifo — fincas pendientes ORDER BY FechaRegistro ASC
    [HttpGet("ColaFifo")]
    public async Task<IActionResult> ColaFifo()
    {
        var cola = await _db.Activos
            .Include(a => a.Dueno)
            .Where(a => a.Estado == EstadoActivoEnum.Pendiente)
            .OrderBy(a => a.FechaRegistro)          // FIFO: primero en entrar, primero en salir
            .Select(a => new
            {
                a.Id,
                a.Hectareas,
                nombreDueno   = a.Dueno!.Nombre,
                fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                lat           = a.Lat,
                lng           = a.Lng,
                // RowVersion en Base64 para concurrencia optimista en TomarFinca
                rowVersion    = Convert.ToBase64String(a.RowVersion!)
            })
            .ToListAsync();

        return Json(cola);
    }

    // GET /Ingeniero/MisAsignadasData — fincas EnRevision asignadas al ingeniero
    [HttpGet("MisAsignadasData")]
    public async Task<IActionResult> MisAsignadasData()
    {
        var asignadas = await _db.Activos
            .Include(a => a.Dueno)
            .Where(a => a.IdIngeniero == UserId &&
                        a.Estado == EstadoActivoEnum.EnRevision)
            .OrderBy(a => a.FechaRegistro)
            .Select(a => new
            {
                a.Id,
                a.Hectareas,
                nombreDueno   = a.Dueno!.Nombre,
                fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();

        return Json(asignadas);
    }

    // GET /Ingeniero/DatosEvaluar/{id} — JSON con detalle + adjuntos para la vista
    [HttpGet("DatosEvaluar/{id:int}")]
    public async Task<IActionResult> DatosEvaluar(int id)
    {
        var activo = await _db.Activos
            .Include(a => a.Dueno)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activo == null || activo.IdIngeniero != UserId)
            return NotFound();

        var adjuntos = await _db.AdjuntosActivos
            .Where(a => a.IdActivo == id)
            .Select(a => new { a.Id, a.NombreArchivo, a.BlobUrl,
                fechaSubida = a.FechaSubida.ToString("dd/MM/yyyy") })
            .ToListAsync();

        return Json(new
        {
            activo.Id,
            activo.Hectareas,
            activo.Vegetacion,
            activo.Hidrologia,
            activo.Topografia,
            activo.EsNacional,
            activo.Lat,
            activo.Lng,
            fechaRegistro = activo.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
            nombreDueno   = activo.Dueno!.Nombre,
            correo        = activo.Dueno.Correo,
            adjuntos
        });
    }

    // POST /Ingeniero/TomarFinca — asignar finca de la cola FIFO con RowVersion (CU18)
    [HttpPost("TomarFinca")]
    public async Task<IActionResult> TomarFinca([FromBody] TomarFincaRequest req)
    {
        var activo = await _db.Activos.FindAsync(req.Id);

        if (activo == null)
            return Json(new { success = false, message = "Finca no encontrada." });

        if (activo.Estado != EstadoActivoEnum.Pendiente)
            return Json(new { success = false, conflict = true,
                message = "Esta finca ya fue tomada por otro ingeniero. Actualiza la cola." });

        // Concurrencia optimista: usar el RowVersion que el cliente leyó
        // Si otro usuario modificó el registro entre que el cliente lo leyó y ahora,
        // EF detecta la diferencia y lanza DbUpdateConcurrencyException
        try
        {
            var rowVersion = Convert.FromBase64String(req.RowVersion);
            _db.Entry(activo).OriginalValues["RowVersion"] = rowVersion;
        }
        catch
        {
            return Json(new { success = false, message = "RowVersion inválido." });
        }

        activo.Estado      = EstadoActivoEnum.EnRevision;
        activo.IdIngeniero = UserId;

        try
        {
            await _db.SaveChangesAsync();
            // N05 — notificar al dueño que la finca entró a revisión
            var dueno = await _db.Sujetos.FindAsync(activo.IdDueno);
            if (dueno != null)
                try { await _email.EnviarFincaEnRevisionAsync(dueno.Correo!, dueno.Nombre!, activo.Id); } catch { }

            return Json(new { success = true, id = activo.Id,
                message = "Finca tomada. Puedes evaluarla en 'Mis Asignadas'." });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra transacción ganó la carrera — informar al cliente
            return Json(new { success = false, conflict = true,
                message = "Esta finca acaba de ser tomada por otro ingeniero. La cola se actualizará." });
        }
    }

    // POST /Ingeniero/Evaluar — Aprobar | Rechazar | Devolver (dictamen)
    [HttpPost("Evaluar")]
    public async Task<IActionResult> Evaluar([FromBody] EvaluarRequest req)
    {
        var activo = await _db.Activos.FindAsync(req.Id);

        if (activo == null || activo.IdIngeniero != UserId)
            return Json(new { success = false, message = "Finca no encontrada." });

        if (activo.Estado != EstadoActivoEnum.EnRevision)
            return Json(new { success = false, message = "Solo se pueden evaluar fincas en revisión." });

        // Validar dictamen
        if (req.Dictamen != "Aprobar" && req.Dictamen != "Rechazar" && req.Dictamen != "Devolver")
            return Json(new { success = false, message = "Dictamen inválido." });

        // Rechazar o Devolver requieren observaciones
        if (req.Dictamen != "Aprobar" && string.IsNullOrWhiteSpace(req.Observaciones))
            return Json(new { success = false,
                message = "Las observaciones son obligatorias para Rechazar o Devolver." });

        activo.Estado = req.Dictamen switch
        {
            "Aprobar"  => EstadoActivoEnum.Aprobada,
            "Rechazar" => EstadoActivoEnum.Rechazada,
            "Devolver" => EstadoActivoEnum.Devuelta,
            _          => activo.Estado
        };

        // Guardar observaciones para Rechazar/Devolver; limpiar para Aprobar
        activo.Observaciones = req.Dictamen == "Aprobar"
            ? null
            : req.Observaciones?.Trim();

        await _db.SaveChangesAsync();

        // N07/N08/N09 — notificar al dueño el resultado del dictamen
        var duenoDictamen = await _db.Sujetos.FindAsync(activo.IdDueno);
        if (duenoDictamen != null)
        {
            try
            {
                _ = req.Dictamen switch
                {
                    "Aprobar"  => _email.EnviarDictamenAprobadaAsync(duenoDictamen.Correo!, duenoDictamen.Nombre!, activo.Id),
                    "Rechazar" => _email.EnviarDictamenRechazadaAsync(duenoDictamen.Correo!, duenoDictamen.Nombre!, activo.Id, activo.Observaciones!),
                    "Devolver" => _email.EnviarDictamenDevueltaAsync(duenoDictamen.Correo!, duenoDictamen.Nombre!, activo.Id, activo.Observaciones!),
                    _          => Task.CompletedTask
                };
            }
            catch { }
        }

        var mensaje = req.Dictamen switch
        {
            "Aprobar"  => "Finca aprobada correctamente.",
            "Rechazar" => "Finca rechazada. Se notificará al dueño.",
            "Devolver" => "Finca devuelta al dueño con observaciones.",
            _          => "Dictamen registrado."
        };

        return Json(new { success = true, message = mensaje });
    }

    // ── CU24: Activar Plan de Pago ────────────────────────────────────────────

    // GET /Ingeniero/MisAprobadas — vista fincas aprobadas del ingeniero sin plan
    [HttpGet("MisAprobadas")]
    public IActionResult MisAprobadas() => View();

    // GET /Ingeniero/MisAprobadasData — JSON
    [HttpGet("MisAprobadasData")]
    public async Task<IActionResult> MisAprobadasData()
    {
        var planActivoIds = await _db.PlanesPago.Select(p => p.IdActivo).ToListAsync();

        var fincas = await _db.Activos
            .Include(a => a.Dueno)
            .Where(a => a.IdIngeniero == UserId &&
                        a.Estado == EstadoActivoEnum.Aprobada &&
                        !planActivoIds.Contains(a.Id))
            .OrderBy(a => a.FechaRegistro)
            .Select(a => new
            {
                a.Id,
                a.Hectareas,
                nombreDueno   = a.Dueno!.Nombre,
                fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Json(fincas);
    }

    // POST /Ingeniero/ActivarPlan — CU24
    [HttpPost("ActivarPlan")]
    public async Task<IActionResult> ActivarPlan([FromBody] ActivarPlanIngenieroRequest req)
    {
        var (ok, mensaje, planId) = await _planService.ActivarAsync(req.ActivoId, UserId);
        return Json(new { success = ok, message = mensaje, planId });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record TomarFincaRequest(int Id, string RowVersion);

public record EvaluarRequest(int Id, string Dictamen, string? Observaciones);

public record ActivarPlanIngenieroRequest(int ActivoId);
