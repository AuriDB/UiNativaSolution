using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

/// <summary>
/// Gestiona las fincas (Activos) del Dueño: registrar, editar, cancelar y adjuntos.
/// Todas las acciones requieren rol Dueno.
/// </summary>
[Authorize(Roles = "Dueno")]
[Route("Dueno")]
public class OwnerController : Controller
{
    private readonly NativaDbContext _db;
    private readonly BlobService     _blob;

    // Id del Dueño autenticado (extraído de Claims)
    private int UserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public OwnerController(NativaDbContext db, BlobService blob)
    {
        _db   = db;
        _blob = blob;
    }

    // ── Vistas ────────────────────────────────────────────────────────────────

    // GET /Dueno — lista de fincas
    [HttpGet("")]
    public IActionResult Index() => View();

    // GET /Dueno/Registrar — formulario nueva finca
    [HttpGet("Registrar")]
    public IActionResult Registrar() => View();

    // GET /Dueno/Detalle/{id} — detalle + adjuntos
    [HttpGet("Detalle/{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var activo = await _db.Activos.FindAsync(id);
        if (activo == null || activo.IdDueno != UserId)
            return NotFound();

        return View(activo);
    }

    // GET /Dueno/Editar/{id} — formulario edición
    [HttpGet("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var activo = await _db.Activos.FindAsync(id);
        if (activo == null || activo.IdDueno != UserId)
            return NotFound();

        // Solo editable en estado Pendiente o Devuelta
        if (activo.Estado != EstadoActivoEnum.Pendiente &&
            activo.Estado != EstadoActivoEnum.Devuelta)
            return Forbid();

        return View(activo);
    }

    // ── Endpoints AJAX (JSON) ─────────────────────────────────────────────────

    // GET /Dueno/MisFincas — lista JSON para Ag-Grid
    [HttpGet("MisFincas")]
    public async Task<IActionResult> MisFincas()
    {
        var fincas = await _db.Activos
            .Where(a => a.IdDueno == UserId)
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => new
            {
                a.Id,
                a.Hectareas,
                estado      = EstadoTexto(a.Estado),
                estadoColor = EstadoColor(a.Estado),
                estadoId    = (int)a.Estado,
                fechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy"),
                lat         = a.Lat,
                lng         = a.Lng,
                puedeEditar   = a.Estado == EstadoActivoEnum.Pendiente ||
                                a.Estado == EstadoActivoEnum.Devuelta,
                puedeCancelar = a.Estado == EstadoActivoEnum.Pendiente
            })
            .ToListAsync();

        return Json(fincas);
    }

    // GET /Dueno/DatosDetalle/{id} — JSON detalle para vista
    [HttpGet("DatosDetalle/{id:int}")]
    public async Task<IActionResult> DatosDetalle(int id)
    {
        var activo = await _db.Activos.FindAsync(id);
        if (activo == null || activo.IdDueno != UserId)
            return NotFound();

        var adjuntos = await _db.AdjuntosActivos
            .Where(a => a.IdActivo == id)
            .Select(a => new { a.Id, a.NombreArchivo, a.BlobUrl, fechaSubida = a.FechaSubida.ToString("dd/MM/yyyy") })
            .ToListAsync();

        var puedeModificar = activo.Estado == EstadoActivoEnum.Pendiente ||
                             activo.Estado == EstadoActivoEnum.Devuelta;

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
            estado        = EstadoTexto(activo.Estado),
            estadoColor   = EstadoColor(activo.Estado),
            estadoId      = (int)activo.Estado,
            activo.Observaciones,
            fechaRegistro = activo.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
            adjuntos,
            puedeModificar
        });
    }

    // POST /Dueno/Registrar — crear nueva finca
    [HttpPost("Registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarFincaRequest req)
    {
        // Validaciones básicas de negocio
        if (req.Hectareas <= 0)
            return Json(new { success = false, message = "Las hectáreas deben ser mayores a 0." });

        if (req.Lat < -90 || req.Lat > 90 || req.Lng < -180 || req.Lng > 180)
            return Json(new { success = false, message = "Coordenadas geográficas inválidas." });

        var activo = new Activo
        {
            IdDueno       = UserId,
            Hectareas     = req.Hectareas,
            Vegetacion    = req.Vegetacion,
            Hidrologia    = req.Hidrologia,
            Topografia    = req.Topografia,
            EsNacional    = req.EsNacional,
            Lat           = req.Lat,
            Lng           = req.Lng,
            Estado        = EstadoActivoEnum.Pendiente,
            FechaRegistro = DateTime.UtcNow   // define orden FIFO para ingeniero
        };

        _db.Activos.Add(activo);
        await _db.SaveChangesAsync();

        return Json(new { success = true, id = activo.Id,
            message = "Finca registrada correctamente y en cola de revisión." });
    }

    // POST /Dueno/Editar — actualizar finca (solo Pendiente o Devuelta)
    [HttpPost("Editar")]
    public async Task<IActionResult> Editar([FromBody] EditarFincaRequest req)
    {
        var activo = await _db.Activos.FindAsync(req.Id);
        if (activo == null || activo.IdDueno != UserId)
            return Json(new { success = false, message = "Finca no encontrada." });

        if (activo.Estado != EstadoActivoEnum.Pendiente &&
            activo.Estado != EstadoActivoEnum.Devuelta)
            return Json(new { success = false, message = "No se puede editar una finca en este estado." });

        if (req.Hectareas <= 0)
            return Json(new { success = false, message = "Las hectáreas deben ser mayores a 0." });

        activo.Hectareas  = req.Hectareas;
        activo.Vegetacion = req.Vegetacion;
        activo.Hidrologia = req.Hidrologia;
        activo.Topografia = req.Topografia;
        activo.EsNacional = req.EsNacional;
        activo.Lat        = req.Lat;
        activo.Lng        = req.Lng;

        // Si estaba Devuelta, regresa a Pendiente y limpia observaciones del ingeniero
        if (activo.Estado == EstadoActivoEnum.Devuelta)
        {
            activo.Estado        = EstadoActivoEnum.Pendiente;
            activo.Observaciones = null;
            activo.FechaRegistro = DateTime.UtcNow; // nueva posición en cola FIFO
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = "Finca actualizada y reingresada a la cola de revisión." });
    }

    // POST /Dueno/Cancelar — soft delete (Pendiente → Vencida)
    [HttpPost("Cancelar")]
    public async Task<IActionResult> Cancelar([FromBody] CancelarFincaRequest req)
    {
        var activo = await _db.Activos.FindAsync(req.Id);
        if (activo == null || activo.IdDueno != UserId)
            return Json(new { success = false, message = "Finca no encontrada." });

        if (activo.Estado != EstadoActivoEnum.Pendiente)
            return Json(new { success = false, message = "Solo se pueden cancelar fincas en estado Pendiente." });

        activo.Estado = EstadoActivoEnum.Vencida;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Finca cancelada." });
    }

    // POST /Dueno/SubirAdjunto — sube archivo (multipart/form-data)
    [HttpPost("SubirAdjunto")]
    public async Task<IActionResult> SubirAdjunto([FromForm] int idActivo, IFormFile archivo)
    {
        var activo = await _db.Activos.FindAsync(idActivo);
        if (activo == null || activo.IdDueno != UserId)
            return Json(new { success = false, message = "Finca no encontrada." });

        if (activo.Estado != EstadoActivoEnum.Pendiente &&
            activo.Estado != EstadoActivoEnum.Devuelta)
            return Json(new { success = false, message = "No se pueden adjuntar archivos en este estado." });

        if (archivo == null || archivo.Length == 0)
            return Json(new { success = false, message = "Selecciona un archivo válido." });

        var (ok, resultado) = await _blob.SubirAsync(archivo);
        if (!ok)
            return Json(new { success = false, message = resultado });

        var adjunto = new AdjuntoActivo
        {
            IdActivo      = idActivo,
            BlobUrl       = resultado,
            NombreArchivo = archivo.FileName,
            FechaSubida   = DateTime.UtcNow
        };

        _db.AdjuntosActivos.Add(adjunto);
        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            adjunto = new { adjunto.Id, adjunto.NombreArchivo, adjunto.BlobUrl,
                fechaSubida = adjunto.FechaSubida.ToString("dd/MM/yyyy") }
        });
    }

    // POST /Dueno/EliminarAdjunto — elimina adjunto
    [HttpPost("EliminarAdjunto")]
    public async Task<IActionResult> EliminarAdjunto([FromBody] EliminarAdjuntoRequest req)
    {
        var adjunto = await _db.AdjuntosActivos
            .Include(a => a.Activo)
            .FirstOrDefaultAsync(a => a.Id == req.IdAdjunto);

        if (adjunto == null || adjunto.Activo!.IdDueno != UserId)
            return Json(new { success = false, message = "Adjunto no encontrado." });

        if (adjunto.Activo.Estado != EstadoActivoEnum.Pendiente &&
            adjunto.Activo.Estado != EstadoActivoEnum.Devuelta)
            return Json(new { success = false, message = "No se pueden eliminar adjuntos en este estado." });

        await _blob.EliminarAsync(adjunto.BlobUrl);
        _db.AdjuntosActivos.Remove(adjunto);
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    // ── Historial de pagos (P6) ───────────────────────────────────────────────

    // GET /Dueno/HistorialPagos — vista
    [HttpGet("HistorialPagos")]
    public IActionResult HistorialPagos() => View();

    // GET /Dueno/HistorialPagosData — JSON planes + pagos del dueño
    [HttpGet("HistorialPagosData")]
    public async Task<IActionResult> HistorialPagosData()
    {
        var planes = await _db.PlanesPago
            .Include(p => p.Activo)
            .Where(p => p.Activo!.IdDueno == UserId)
            .OrderByDescending(p => p.FechaActivacion)
            .Select(p => new
            {
                planId          = p.Id,
                activoId        = p.IdActivo,
                hectareas       = p.Activo!.Hectareas,
                montoMensual    = p.MontoMensual,
                fechaActivacion = p.FechaActivacion.ToString("dd/MM/yyyy"),
                pagos = _db.PagosMensuales
                    .Where(m => m.IdPlan == p.Id)
                    .OrderBy(m => m.NumeroPago)
                    .Select(m => new
                    {
                        m.Id,
                        m.NumeroPago,
                        m.Monto,
                        estado         = m.Estado == Nativa.Domain.Enums.EstadoPagoEnum.Ejecutado ? "Ejecutado" : "Pendiente",
                        estadoColor    = m.Estado == Nativa.Domain.Enums.EstadoPagoEnum.Ejecutado ? "success" : "warning",
                        fechaPago      = m.FechaPago.ToString("dd/MM/yyyy"),
                        fechaEjecucion = m.FechaEjecucion != null
                            ? m.FechaEjecucion.Value.ToString("dd/MM/yyyy HH:mm")
                            : null
                    })
                    .ToList()
            })
            .ToListAsync();

        return Json(planes);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string EstadoTexto(EstadoActivoEnum estado) => estado switch
    {
        EstadoActivoEnum.Pendiente  => "Pendiente",
        EstadoActivoEnum.EnRevision => "En Revisión",
        EstadoActivoEnum.Aprobada   => "Aprobada",
        EstadoActivoEnum.Devuelta   => "Devuelta",
        EstadoActivoEnum.Rechazada  => "Rechazada",
        EstadoActivoEnum.Vencida    => "Vencida",
        _                           => estado.ToString()
    };

    private static string EstadoColor(EstadoActivoEnum estado) => estado switch
    {
        EstadoActivoEnum.Pendiente  => "warning",
        EstadoActivoEnum.EnRevision => "info",
        EstadoActivoEnum.Aprobada   => "success",
        EstadoActivoEnum.Devuelta   => "secondary",
        EstadoActivoEnum.Rechazada  => "danger",
        EstadoActivoEnum.Vencida    => "dark",
        _                           => "light"
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record RegistrarFincaRequest(
    decimal Hectareas, decimal Vegetacion, decimal Hidrologia,
    decimal Topografia, bool EsNacional, decimal Lat, decimal Lng);

public record EditarFincaRequest(
    int Id,
    decimal Hectareas, decimal Vegetacion, decimal Hidrologia,
    decimal Topografia, bool EsNacional, decimal Lat, decimal Lng);

public record CancelarFincaRequest(int Id);
public record EliminarAdjuntoRequest(int IdAdjunto);
