using Nativa.Domain.Entities;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests.Services;

/// <summary>
/// Tests unitarios puros de CalculatorService.Calcular (sin BD).
/// Cubre: fórmula base, SumaPct > Tope (capped), bono nacional, sin bono.
/// </summary>
public class CalculatorServiceTests
{
    // Parámetros de referencia para todos los tests
    private static ParametrosPago Params(
        decimal precioBase    = 50_000m,
        decimal pctVeg        = 0.10m,
        decimal pctHid        = 0.10m,
        decimal pctNac        = 0.05m,
        decimal pctTop        = 0.05m,
        decimal tope          = 0.50m) => new()
    {
        PrecioBase    = precioBase,
        PctVegetacion = pctVeg,
        PctHidrologia = pctHid,
        PctNacional   = pctNac,
        PctTopografia = pctTop,
        Tope          = tope,
        Vigente       = true,
        CreadoPor     = 1
    };

    private static Activo Finca(decimal hectareas, bool esNacional = false) => new()
    {
        Id         = 1,
        IdDueno    = 1,
        Hectareas  = hectareas,
        EsNacional = esNacional,
        Estado     = Nativa.Domain.Enums.EstadoActivoEnum.Aprobada
    };

    [Fact]
    public void Calcular_SinBono_RetornaMontoBase()
    {
        // SumaPct = 0.10 + 0.10 + 0 + 0.05 = 0.25
        // Monto = 10 × 50000 × (1 + 0.25) = 625 000
        var monto = CalculatorService.Calcular(Finca(10m, esNacional: false), Params());

        Assert.Equal(625_000m, monto);
    }

    [Fact]
    public void Calcular_ConBonoNacional_IncluteBonoEnSumaPct()
    {
        // SumaPct = 0.10 + 0.10 + 0.05 + 0.05 = 0.30
        // Monto = 10 × 50000 × (1 + 0.30) = 650 000
        var monto = CalculatorService.Calcular(Finca(10m, esNacional: true), Params());

        Assert.Equal(650_000m, monto);
    }

    [Fact]
    public void Calcular_SumaPctSuperaTope_AplicaTope()
    {
        // SumaPct = 0.30 + 0.30 + 0.05 + 0.20 = 0.85 > Tope 0.50
        // Monto = 5 × 50000 × (1 + 0.50) = 375 000
        var p = Params(pctVeg: 0.30m, pctHid: 0.30m, pctTop: 0.20m, tope: 0.50m);
        var monto = CalculatorService.Calcular(Finca(5m, esNacional: true), p);

        Assert.Equal(375_000m, monto);
    }

    [Fact]
    public void Calcular_SinFactores_SoloMultiplica()
    {
        // SumaPct = 0 → Monto = hectareas × precioBase × 1
        var p = Params(pctVeg: 0, pctHid: 0, pctNac: 0, pctTop: 0);
        var monto = CalculatorService.Calcular(Finca(2m), p);

        Assert.Equal(100_000m, monto);
    }

    [Fact]
    public void Calcular_RedondeoADosDecimales()
    {
        // 3 hectáreas × 33333.33 × 1 = 99999.99
        var p = Params(precioBase: 33_333.33m, pctVeg: 0, pctHid: 0, pctNac: 0, pctTop: 0);
        var monto = CalculatorService.Calcular(Finca(3m), p);

        Assert.Equal(99_999.99m, monto);
    }
}
