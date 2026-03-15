using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Infrastructure;
using WEB_UI.Services;

namespace WEB_UI.Controllers;

/// <summary>
/// Gestión de cuentas bancarias del Dueño.
/// IBAN cifrado con AES-256-GCM; solo se muestra la versión ofuscada.
/// </summary>
[Authorize(Roles = "Dueno")]
[Route("Dueno/Cuenta")]
public class BankAccountController : Controller
{
    private readonly NativaDbContext   _db;
    private readonly BankAccountService _bankService;

    private int UserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public BankAccountController(NativaDbContext db, BankAccountService bankService)
    {
        _db          = db;
        _bankService = bankService;
    }

    // ── Vista ────────────────────────────────────────────────────────────────

    // GET /Dueno/Cuenta
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Dueno/Cuentas.cshtml");

    // ── Endpoints AJAX ───────────────────────────────────────────────────────

    // GET /Dueno/Cuenta/MisCuentas — JSON lista ofuscada
    [HttpGet("MisCuentas")]
    public async Task<IActionResult> MisCuentas()
    {
        var cuentas = await _db.CuentasBancarias
            .Where(c => c.IdDueno == UserId)
            .OrderByDescending(c => c.Activo)
            .ThenByDescending(c => c.FechaCreacion)
            .Select(c => new
            {
                c.Id,
                c.Banco,
                c.TipoCuenta,
                c.Titular,
                c.IbanOfuscado,
                c.Activo,
                fechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return Json(cuentas);
    }

    // POST /Dueno/Cuenta/Agregar — registrar nueva cuenta
    [HttpPost("Agregar")]
    public async Task<IActionResult> Agregar([FromBody] AgregarCuentaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Banco) ||
            string.IsNullOrWhiteSpace(req.TipoCuenta) ||
            string.IsNullOrWhiteSpace(req.Titular) ||
            string.IsNullOrWhiteSpace(req.Iban))
            return Json(new { success = false, message = "Todos los campos son obligatorios." });

        var iban = req.Iban.Trim().ToUpper();

        if (!_bankService.ValidarIban(iban))
            return Json(new { success = false,
                message = "IBAN inválido. Debe iniciar con CR seguido de exactamente 20 dígitos." });

        var cuenta = new CuentaBancaria
        {
            IdDueno      = UserId,
            Banco        = req.Banco.Trim(),
            TipoCuenta   = req.TipoCuenta.Trim(),
            Titular      = req.Titular.Trim(),
            IbanCompleto = _bankService.Cifrar(iban),
            IbanOfuscado = _bankService.Ofuscar(iban),
            Activo       = true
        };

        _db.CuentasBancarias.Add(cuenta);
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Cuenta bancaria registrada correctamente." });
    }

    // POST /Dueno/Cuenta/Desactivar — soft-delete
    [HttpPost("Desactivar")]
    public async Task<IActionResult> Desactivar([FromBody] DesactivarCuentaRequest req)
    {
        var cuenta = await _db.CuentasBancarias.FindAsync(req.Id);

        if (cuenta == null || cuenta.IdDueno != UserId)
            return Json(new { success = false, message = "Cuenta no encontrada." });

        if (!cuenta.Activo)
            return Json(new { success = false, message = "La cuenta ya está inactiva." });

        cuenta.Activo = false;
        await _db.SaveChangesAsync();

        return Json(new { success = true, message = "Cuenta desactivada correctamente." });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record AgregarCuentaRequest(string Banco, string TipoCuenta, string Titular, string Iban);
public record DesactivarCuentaRequest(int Id);
