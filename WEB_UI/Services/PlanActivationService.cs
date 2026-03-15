using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;

namespace WEB_UI.Services;

/// <summary>
/// Activa un plan de pago PSA para una finca Aprobada:
/// 1. Verifica prerequisitos (finca Aprobada, parámetros vigentes, sin plan previo).
/// 2. Calcula el MontoMensual con CalculatorService.
/// 3. Crea el PlanPago con snapshot de parámetros.
/// 4. Genera 12 PagosMensuales (uno por mes a partir de hoy + 1 mes).
/// </summary>
public class PlanActivationService
{
    private readonly NativaDbContext    _db;
    private readonly CalculatorService  _calculator;

    public PlanActivationService(NativaDbContext db, CalculatorService calculator)
    {
        _db         = db;
        _calculator = calculator;
    }

    public async Task<(bool ok, string mensaje, int? planId)> ActivarAsync(int activoId, int ingenieroId)
    {
        var activo = await _db.Activos.FindAsync(activoId);

        if (activo == null)
            return (false, "Finca no encontrada.", null);

        if (activo.Estado != EstadoActivoEnum.Aprobada)
            return (false, "Solo se puede activar un plan para fincas en estado Aprobada.", null);

        // Un activo solo puede tener un plan de pago
        var planExistente = await _db.PlanesPago.AnyAsync(p => p.IdActivo == activoId);
        if (planExistente)
            return (false, "Esta finca ya tiene un plan de pago activo.", null);

        // Calcular monto con parámetros vigentes
        var resultado = await _calculator.CalcularAsync(activo);
        if (resultado == null)
            return (false, "No hay parámetros de pago vigentes. Configúralos en el panel de Administración.", null);

        var (monto, parametros) = resultado.Value;

        // Snapshot inmutable de los parámetros usados al activar
        var snapshot = JsonSerializer.Serialize(new
        {
            parametros.Id,
            parametros.PrecioBase,
            parametros.PctVegetacion,
            parametros.PctHidrologia,
            parametros.PctNacional,
            parametros.PctTopografia,
            parametros.Tope,
            FechaSnapshot = DateTime.UtcNow
        });

        var ahora = DateTime.UtcNow;

        var plan = new PlanPago
        {
            IdActivo               = activoId,
            IdIngeniero            = ingenieroId,
            FechaActivacion        = ahora,
            SnapshotParametrosJson = snapshot,
            MontoMensual           = monto
        };

        _db.PlanesPago.Add(plan);
        await _db.SaveChangesAsync(); // Necesario para obtener plan.Id

        // 12 pagos: el primero vence en 1 mes, el último en 12 meses
        var pagos = Enumerable.Range(1, 12).Select(n => new PagoMensual
        {
            IdPlan     = plan.Id,
            NumeroPago = n,
            Monto      = monto,
            FechaPago  = ahora.AddMonths(n),
            Estado     = EstadoPagoEnum.Pendiente
        });

        _db.PagosMensuales.AddRange(pagos);
        await _db.SaveChangesAsync();

        return (true,
            $"Plan activado correctamente. Monto mensual: ₡{monto:N2}. Se programaron 12 pagos.",
            plan.Id);
    }

    /// <summary>
    /// Retorna una previsualización del monto sin persistir nada.
    /// Útil para mostrar al Admin antes de activar.
    /// </summary>
    public async Task<decimal?> PreviewMontoAsync(int activoId)
    {
        var activo = await _db.Activos.FindAsync(activoId);
        if (activo == null) return null;

        var resultado = await _calculator.CalcularAsync(activo);
        return resultado?.monto;
    }
}
