using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WEB_UI.Models.Dtos;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

[Authorize(Roles = "Ingeniero")]
public class IngenieroController : Controller
{
    private readonly IngenieroService    _ing;
    private readonly ActivoService       _activo;
    private readonly ExternalApiService  _ext;

    public IngenieroController(IngenieroService ing, ActivoService activo, ExternalApiService ext)
    {
        _ing    = ing;
        _activo = activo;
        _ext    = ext;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Dashboard ────────────────────────────────────────────────────────────
    [HttpGet("Ingeniero/Dashboard")]
    public async Task<IActionResult> Dashboard()
        => Json(await _ing.GetDashboardIngenieroAsync(UserId));

    // ── CU17 Cola FIFO ───────────────────────────────────────────────────────
    [Route("Ingeniero/Cola")]
    public IActionResult Cola() => View("~/Views/Ingeniero/Cola.cshtml");

    [HttpGet("Ingeniero/Cola/Data")]
    public async Task<IActionResult> ColaData()
    {
        var data = await _ing.GetColaFifoAsync();
        return Json(data);
    }

    // ── CU18 Tomar Finca ─────────────────────────────────────────────────────
    [HttpPost("Ingeniero/Cola/Tomar/{id:int}")]
    public async Task<IActionResult> TomarFinca(int id, [FromBody] TomarFincaDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, statusCode, mensaje) = await _ing.TomarFincaAsync(id, dto.RowVersion, UserId);

        Response.StatusCode = statusCode;
        return Json(new { success = ok, message = mensaje });
    }

    // ── CU19 Evaluar Finca ───────────────────────────────────────────────────
    [HttpGet("Ingeniero/Fincas/Evaluar/{id:int}")]
    public async Task<IActionResult> Evaluar(int id)
    {
        var finca = await _ing.GetParaEvaluarAsync(id, UserId);
        if (finca is null) return NotFound();
        return View("~/Views/Ingeniero/Fincas/Evaluar.cshtml", finca);
    }

    [HttpGet("Ingeniero/Fincas/Evaluar/{id:int}/ApiData")]
    public async Task<IActionResult> EvaluarApiData(int id)
    {
        var finca = await _ing.GetParaEvaluarAsync(id, UserId);
        if (finca is null) return NotFound();

        var datos = await _ext.ObtenerDatosAsync(finca.Lat, finca.Lng);
        return Json(datos);
    }

    // ── CU16 Adjuntos (Ingeniero también puede ver) ──────────────────────────
    [HttpGet("Ingeniero/Fincas/{id:int}/Adjuntos")]
    public async Task<IActionResult> FincaAdjuntos(int id)
    {
        var finca = await _ing.GetParaEvaluarAsync(id, UserId);
        if (finca is null) return NotFound();
        var adjuntos = await _activo.GetAdjuntosAsync(id);
        return Json(adjuntos.Select(a => new
        {
            a.NombreArchivo,
            a.BlobUrl,
            Fecha = a.FechaSubida.ToString("dd/MM/yyyy")
        }));
    }

    // ── CU20-22 Dictamen ─────────────────────────────────────────────────────
    [HttpPost("Ingeniero/Fincas/Dictamen/{id:int}")]
    public async Task<IActionResult> Dictamen(int id, [FromBody] DictamenDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Datos inválidos." });

        var (ok, mensaje) = await _ing.DictamenAsync(id, dto.Tipo, dto.Observaciones, UserId);
        return Json(new { success = ok, message = mensaje });
    }

    // ── CU24 Activar Plan de Pagos ───────────────────────────────────────────
    [HttpPost("Ingeniero/Fincas/ActivarPlan/{id:int}")]
    public async Task<IActionResult> ActivarPlan(int id)
    {
        var (ok, mensaje) = await _ing.ActivarPlanAsync(id, UserId);
        return Json(new { success = ok, message = mensaje });
    }
}
