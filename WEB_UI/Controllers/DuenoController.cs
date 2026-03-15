using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WEB_UI.Models.Dtos;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

[Authorize(Roles = "Dueno")]
public class DuenoController : Controller
{
    private readonly ActivoService     _activo;
    private readonly EncryptionService _enc;

    public DuenoController(ActivoService activo, EncryptionService enc)
    {
        _activo = activo;
        _enc    = enc;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── CU12 Ver mis Fincas ──────────────────────────────────────────────────
    [Route("Dueno/Fincas")]
    public IActionResult Fincas() => View("~/Views/Dueno/Fincas/Index.cshtml");

    [HttpGet("Dueno/Fincas/Data")]
    public async Task<IActionResult> FincasData()
    {
        var data = await _activo.ListarFincasDuenoAsync(UserId);
        return Json(data);
    }

    // ── CU11 Registrar Finca ─────────────────────────────────────────────────
    [HttpGet("Dueno/Fincas/Nueva")]
    public IActionResult FincasNueva() => View("~/Views/Dueno/Fincas/Nueva.cshtml");

    [HttpPost("Dueno/Fincas/Nueva")]
    public async Task<IActionResult> FincasNueva(
        decimal hectareas, decimal vegetacion, decimal hidrologia, decimal topografia,
        bool esNacional, decimal lat, decimal lng)
    {
        var archivos = Request.Form.Files;
        var (ok, mensaje, id) = await _activo.RegistrarAsync(
            hectareas, vegetacion, hidrologia, topografia,
            esNacional, lat, lng, UserId, archivos);

        if (!ok) { TempData["Error"] = mensaje; return View("~/Views/Dueno/Fincas/Nueva.cshtml"); }

        TempData["Success"] = mensaje;
        return RedirectToAction(nameof(FincaDetalle), new { id });
    }

    // ── CU13 Detalle Finca ───────────────────────────────────────────────────
    [HttpGet("Dueno/Fincas/{id:int}")]
    public async Task<IActionResult> FincaDetalle(int id)
    {
        var finca = await _activo.GetDetalleAsync(id, UserId);
        if (finca is null) return NotFound();
        return View("~/Views/Dueno/Fincas/Detalle.cshtml", finca);
    }

    // ── CU14 Editar Finca ────────────────────────────────────────────────────
    [HttpPost("Dueno/Fincas/Editar/{id:int}")]
    public async Task<IActionResult> FincaEditar(int id,
        decimal hectareas, decimal vegetacion, decimal hidrologia, decimal topografia,
        bool esNacional, decimal lat, decimal lng, string? observaciones)
    {
        var archivos = Request.Form.Files;
        var (ok, mensaje) = await _activo.EditarAsync(
            id, UserId, hectareas, vegetacion, hidrologia, topografia,
            esNacional, lat, lng, observaciones, archivos);

        TempData[ok ? "Success" : "Error"] = mensaje;
        return RedirectToAction(nameof(FincaDetalle), new { id });
    }

    // ── CU15 Reenviar a Evaluación ───────────────────────────────────────────
    [HttpPost("Dueno/Fincas/Reenviar/{id:int}")]
    public async Task<IActionResult> FincaReenviar(int id)
    {
        var (ok, mensaje) = await _activo.ReenviarAsync(id, UserId);
        TempData[ok ? "Success" : "Error"] = mensaje;
        return RedirectToAction(nameof(FincaDetalle), new { id });
    }

    // ── CU16 Ver Adjuntos ────────────────────────────────────────────────────
    [HttpGet("Dueno/Fincas/{id:int}/Adjuntos")]
    public async Task<IActionResult> FincaAdjuntos(int id)
    {
        // Verificar que la finca pertenece al dueño
        var finca = await _activo.GetDetalleAsync(id, UserId);
        if (finca is null) return NotFound();
        var adjuntos = await _activo.GetAdjuntosAsync(id);
        return Json(adjuntos.Select(a => new { a.NombreArchivo, a.BlobUrl, Fecha = a.FechaSubida.ToString("dd/MM/yyyy") }));
    }

    // ── CU23 IBAN ────────────────────────────────────────────────────────────
    [HttpGet("Dueno/CuentaBancaria")]
    public async Task<IActionResult> CuentaBancaria()
    {
        var cuenta = await _activo.GetCuentaActivaAsync(UserId);
        ViewBag.Cuenta = cuenta;
        return View("~/Views/Dueno/Cuenta.cshtml");
    }

    [HttpPost("Dueno/CuentaBancaria")]
    public async Task<IActionResult> CuentaBancaria([FromBody] RegistrarIbanDto dto)
    {
        if (dto is null) return Json(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _activo.RegistrarIbanAsync(
            UserId, dto.Banco, dto.TipoCuenta, dto.Titular, dto.Iban, _enc);

        return Json(new { success = ok, message = mensaje });
    }

    // ── CU26 Historial Pagos ─────────────────────────────────────────────────
    [Route("Dueno/Pagos")]
    public IActionResult Pagos() => View("~/Views/Dueno/Pagos.cshtml");

    [HttpGet("Dueno/Pagos/Data")]
    public async Task<IActionResult> PagosData()
    {
        var pagos = await _activo.GetPagosHistorialAsync(UserId);
        return Json(pagos);
    }
}
