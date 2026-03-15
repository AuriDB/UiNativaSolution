using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Entities;
using Nativa.Infrastructure;

namespace WEB_UI.Services;

/// <summary>
/// Calcula el monto mensual del PSA según la fórmula oficial.
/// Fórmula: MontoMensual = Hectareas × PrecioBase × (1 + Min(SumaPct, Tope))
/// SumaPct  = PctVegetacion + PctHidrologia + (EsNacional ? PctNacional : 0) + PctTopografia
/// </summary>
public class CalculatorService
{
    private readonly NativaDbContext _db;

    public CalculatorService(NativaDbContext db)
    {
        _db = db;
    }

    /// <summary>Obtiene los parámetros vigentes y calcula el monto mensual para el activo.</summary>
    public async Task<(decimal monto, ParametrosPago parametros)?> CalcularAsync(Activo activo)
    {
        var parametros = await _db.ParametrosPagos
            .Where(p => p.Vigente)
            .OrderByDescending(p => p.FechaCreacion)
            .FirstOrDefaultAsync();

        if (parametros == null) return null;

        return (Calcular(activo, parametros), parametros);
    }

    /// <summary>Cálculo puro — sin acceso a BD (útil para tests y previsualizaciones).</summary>
    public static decimal Calcular(Activo activo, ParametrosPago p)
    {
        var sumaPct = p.PctVegetacion
                    + p.PctHidrologia
                    + (activo.EsNacional ? p.PctNacional : 0)
                    + p.PctTopografia;

        var factor = 1 + Math.Min(sumaPct, p.Tope);
        return Math.Round(activo.Hectareas * p.PrecioBase * factor, 2);
    }
}
