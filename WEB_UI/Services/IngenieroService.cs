using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WEB_UI.Data;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Services;

public class IngenieroService
{
    private readonly NativaDbContext  _db;
    private readonly EmailService     _email;
    private readonly CalculadoraService _calc;

    public IngenieroService(NativaDbContext db, EmailService email, CalculadoraService calc)
    {
        _db    = db;
        _email = email;
        _calc  = calc;
    }

    // ── CU17 Cola FIFO ───────────────────────────────────────────────────────
    public async Task<List<object>> GetColaFifoAsync()
    {
        return await _db.Activos
            .Where(a => a.Estado == EstadoActivoEnum.Pendiente)
            .Include(a => a.Dueno)
            .OrderBy(a => a.FechaRegistro)
            .Select(a => (object)new
            {
                a.Id,
                a.Hectareas,
                a.Vegetacion,
                a.Hidrologia,
                a.Topografia,
                a.EsNacional,
                a.Lat,
                a.Lng,
                NombreDueno   = a.Dueno.Nombre,
                FechaRegistro = a.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                RowVersion    = Convert.ToBase64String(a.RowVersion)
            })
            .ToListAsync();
    }

    // ── CU18 Tomar Finca (RowVersion / concurrencia optimista) ───────────────
    public async Task<(bool ok, int statusCode, string mensaje)> TomarFincaAsync(
        int id, string rowVersionBase64, int ingenieroId)
    {
        var finca = await _db.Activos
            .Include(a => a.Dueno)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (finca is null)
            return (false, 404, "Finca no encontrada.");

        if (finca.Estado != EstadoActivoEnum.Pendiente)
            return (false, 409, "Esta finca ya fue tomada por otro ingeniero.");

        byte[] rv;
        try   { rv = Convert.FromBase64String(rowVersionBase64); }
        catch { return (false, 400, "RowVersion inválido."); }

        // Establecer RowVersion original para concurrencia optimista
        _db.Entry(finca).Property(f => f.RowVersion).OriginalValue = rv;
        finca.Estado      = EstadoActivoEnum.EnRevision;
        finca.IdIngeniero = ingenieroId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, 409, "Conflicto: otro ingeniero tomó esta finca antes. Actualiza la cola.");
        }

        // N05 — notificar al Dueño
        _ = _email.EnviarGenericoAsync(finca.Dueno.Correo,
            "Tu finca está en revisión — Sistema Nativa",
            $"<p>Hola <strong>{finca.Dueno.Nombre}</strong>,</p>" +
            $"<p>Tu finca ID #{finca.Id} ha pasado a estado <strong>En Revisión</strong>. " +
            $"Un ingeniero evaluará tu solicitud a la brevedad.</p>");

        return (true, 200, "Finca tomada correctamente.");
    }

    // ── CU19 Get Finca para Evaluar ──────────────────────────────────────────
    public async Task<Activo?> GetParaEvaluarAsync(int id, int ingenieroId)
        => await _db.Activos
            .Include(a => a.Dueno)
            .Include(a => a.Adjuntos)
            .FirstOrDefaultAsync(a => a.Id == id && a.IdIngeniero == ingenieroId
                                 && a.Estado == EstadoActivoEnum.EnRevision);

    // ── CU20-22 Dictamen ─────────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> DictamenAsync(
        int id, string tipo, string? observaciones, int ingenieroId)
    {
        var finca = await _db.Activos
            .Include(a => a.Dueno)
            .FirstOrDefaultAsync(a => a.Id == id && a.IdIngeniero == ingenieroId
                                 && a.Estado == EstadoActivoEnum.EnRevision);

        if (finca is null)
            return (false, "Finca no encontrada o no está en tu revisión.");

        switch (tipo)
        {
            case "Aprobar":
                finca.Estado = EstadoActivoEnum.Aprobada;
                _ = _email.EnviarGenericoAsync(finca.Dueno.Correo,
                    "¡Tu finca fue aprobada! — Sistema Nativa",
                    $"<p>Hola <strong>{finca.Dueno.Nombre}</strong>,</p>" +
                    $"<p>¡Felicidades! Tu finca ID #{finca.Id} fue <strong>aprobada</strong>. " +
                    $"Para activar tu plan de pagos, registra tu cuenta bancaria (IBAN) en el sistema.</p>");
                break;

            case "Rechazar":
                if (string.IsNullOrWhiteSpace(observaciones))
                    return (false, "Las observaciones son obligatorias para rechazar.");
                finca.Estado        = EstadoActivoEnum.Rechazada;
                finca.Observaciones = observaciones;
                _ = _email.EnviarGenericoAsync(finca.Dueno.Correo,
                    "Tu finca fue rechazada — Sistema Nativa",
                    $"<p>Hola <strong>{finca.Dueno.Nombre}</strong>,</p>" +
                    $"<p>Tu finca ID #{finca.Id} fue <strong>rechazada</strong>.</p>" +
                    $"<p><strong>Observaciones:</strong> {observaciones}</p>" +
                    $"<p>Esta resolución es definitiva.</p>");
                break;

            case "Devolver":
                if (string.IsNullOrWhiteSpace(observaciones))
                    return (false, "Las observaciones son obligatorias para devolver.");
                finca.Estado        = EstadoActivoEnum.Devuelta;
                finca.IdIngeniero   = null;
                finca.Observaciones = observaciones;
                _ = _email.EnviarGenericoAsync(finca.Dueno.Correo,
                    "Tu finca fue devuelta para corrección — Sistema Nativa",
                    $"<p>Hola <strong>{finca.Dueno.Nombre}</strong>,</p>" +
                    $"<p>Tu finca ID #{finca.Id} fue <strong>devuelta</strong> para que hagas correcciones.</p>" +
                    $"<p><strong>Observaciones:</strong> {observaciones}</p>" +
                    $"<p>Corrige los datos y reenvíala para evaluación.</p>");
                break;

            default:
                return (false, "Tipo de dictamen inválido.");
        }

        await _db.SaveChangesAsync();
        return (true, $"Dictamen '{tipo}' aplicado correctamente.");
    }

    // ── CU24 Activar Plan de Pagos ───────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> ActivarPlanAsync(int activoId, int ingenieroId)
    {
        var finca = await _db.Activos
            .Include(a => a.Dueno)
            .ThenInclude(d => d.CuentasBancarias)
            .FirstOrDefaultAsync(a => a.Id == activoId
                                 && a.IdIngeniero == ingenieroId
                                 && a.Estado == EstadoActivoEnum.Aprobada);

        if (finca is null)
            return (false, "Finca no encontrada, no aprobada o no en tu asignación.");

        // Verificar IBAN activo del Dueño
        var cuentaActiva = finca.Dueno.CuentasBancarias.FirstOrDefault(c => c.Activo);
        if (cuentaActiva is null)
            return (false, "El dueño no tiene cuenta bancaria registrada. No se puede activar el plan.");

        // Parámetros vigentes
        var parametros = await _db.ParametrosPago
            .Where(p => p.Vigente)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (parametros is null)
            return (false, "No hay parámetros de pago vigentes configurados.");

        var monto = _calc.Calcular(finca, parametros);
        var snapshot = JsonSerializer.Serialize(new
        {
            parametros.Id,
            parametros.PrecioBase,
            parametros.PctVegetacion,
            parametros.PctHidrologia,
            parametros.PctNacional,
            parametros.PctTopografia,
            parametros.Tope,
            parametros.Vigente,
            parametros.FechaCreacion
        });

        var plan = new PlanPago
        {
            IdActivo               = finca.Id,
            IdIngeniero            = ingenieroId,
            FechaActivacion        = DateTime.UtcNow,
            SnapshotParametrosJson = snapshot,
            MontoMensual           = monto,
            FechaCreacion          = DateTime.UtcNow
        };
        _db.PlanesPago.Add(plan);
        await _db.SaveChangesAsync();

        // Generar 12 PagoMensual
        for (int i = 1; i <= 12; i++)
        {
            _db.PagosMensuales.Add(new PagoMensual
            {
                IdPlan       = plan.Id,
                NumeroPago   = i,
                Monto        = monto,
                FechaPago    = plan.FechaActivacion.AddDays(i * 30),
                Estado       = EstadoPagoEnum.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        return (true, $"Plan activado. Monto mensual: ₡{monto:N2}. 12 pagos programados.");
    }
}
