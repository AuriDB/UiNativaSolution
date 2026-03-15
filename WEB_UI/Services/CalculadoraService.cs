using WEB_UI.Models.Entities;

namespace WEB_UI.Services;

public class CalculadoraService
{
    /// <summary>
    /// Calcula el monto mensual PSA según la fórmula del spec.
    /// SumaPct = PctVeg + PctHid + (EsNac ? PctNac : 0) + PctTop
    /// Pago = Hectareas × PrecioBase × (1 + Min(SumaPct, Tope))
    /// Redondeo: 2 decimales, MidpointRounding.AwayFromZero
    /// </summary>
    public decimal Calcular(Activo finca, ParametrosPago p)
    {
        var sumaPct = p.PctVegetacion
                    + p.PctHidrologia
                    + (finca.EsNacional ? p.PctNacional : 0m)
                    + p.PctTopografia;

        var pago = finca.Hectareas * p.PrecioBase * (1m + Math.Min(sumaPct, p.Tope));
        return Math.Round(pago, 2, MidpointRounding.AwayFromZero);
    }
}
