using WEB_UI.Models.Entities;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests;

public class CalculadoraServiceTests
{
    private readonly CalculadoraService _calc = new();

    private static ParametrosPago Params(
        decimal precioBase  = 10000m,
        decimal pctVeg      = 0.10m,
        decimal pctHid      = 0.08m,
        decimal pctNac      = 0.05m,
        decimal pctTop      = 0.07m,
        decimal tope        = 0.50m) => new()
    {
        PrecioBase    = precioBase,
        PctVegetacion = pctVeg,
        PctHidrologia = pctHid,
        PctNacional   = pctNac,
        PctTopografia = pctTop,
        Tope          = tope,
        Vigente       = true,
        FechaCreacion = DateTime.UtcNow,
        CreadoPor     = 1
    };

    private static Activo Finca(decimal ha = 10m, bool esNacional = false) => new()
    {
        Hectareas  = ha,
        EsNacional = esNacional
    };

    [Fact]
    public void Formula_SinBonoNacional_RetornaMontoEsperado()
    {
        // SumaPct = 0.10 + 0.08 + 0 + 0.07 = 0.25  (sin nacional)
        // Pago    = 10 × 10000 × (1 + 0.25) = 125,000
        var resultado = _calc.Calcular(Finca(10m, false), Params());
        Assert.Equal(125_000m, resultado);
    }

    [Fact]
    public void Formula_ConBonoNacional_IncluyyePctNacional()
    {
        // SumaPct = 0.10 + 0.08 + 0.05 + 0.07 = 0.30
        // Pago    = 10 × 10000 × (1 + 0.30) = 130,000
        var resultado = _calc.Calcular(Finca(10m, true), Params());
        Assert.Equal(130_000m, resultado);
    }

    [Fact]
    public void Formula_CuandoSumaPctSuperaTope_UsaTope()
    {
        // SumaPct = 0.10 + 0.08 + 0.05 + 0.07 = 0.30  → menor que tope 0.50 → NO aplica
        // Para forzar tope: usar tope=0.20
        // SumaPct = 0.30 > Tope 0.20 → usa 0.20
        // Pago = 10 × 10000 × (1 + 0.20) = 120,000
        var resultado = _calc.Calcular(Finca(10m, true), Params(tope: 0.20m));
        Assert.Equal(120_000m, resultado);
    }

    [Fact]
    public void Formula_Redondeo_AwayFromZero()
    {
        // Caso con resultado fraccionario para verificar redondeo
        // 3 × 10000 × (1 + 0.25) = 37,500 → exacto, sin fracción
        // Cambiemos: 3 × 9999 × 1.25 = 37,496.25 → redondeado = 37,496.25
        var p = Params(precioBase: 9999m);
        var resultado = _calc.Calcular(Finca(3m, false), p);
        Assert.Equal(37_496.25m, resultado);
    }

    [Fact]
    public void Formula_ConHectareasDecimales_CalculaCorrectamente()
    {
        // 5.5 × 10000 × 1.25 = 68,750
        var resultado = _calc.Calcular(Finca(5.5m, false), Params());
        Assert.Equal(68_750m, resultado);
    }
}
