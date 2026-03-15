using System.Text.RegularExpressions;
using Xunit;

namespace Nativa.Tests;

public class IbanValidationTests
{
    // Misma regex usada en ActivoService y cuenta.js
    private static readonly Regex _ibanRegex = new(@"^CR\d{20}$", RegexOptions.Compiled);

    private static bool EsValido(string iban) => _ibanRegex.IsMatch(iban);

    [Theory]
    [InlineData("CR21015200009123456789")]  // 22 chars: CR + 20 dígitos
    [InlineData("CR00000000000000000000")]  // todos ceros
    [InlineData("CR99999999999999999999")]  // todos nueves
    public void Iban_FormatoCorrecto_EsValido(string iban)
        => Assert.True(EsValido(iban));

    [Theory]
    [InlineData("CR2101520000912345678")]   // solo 19 dígitos
    [InlineData("CR210152000091234567890")] // 21 dígitos (muy largo)
    [InlineData("US21015200009123456789")]  // prefijo incorrecto
    [InlineData("cr21015200009123456789")]  // prefijo minúsculas
    [InlineData("CR2101520000A123456789")]  // tiene letra en dígitos
    [InlineData("CR")]                      // solo prefijo
    [InlineData("")]                        // vacío
    [InlineData("21015200009123456789")]    // sin prefijo CR
    public void Iban_FormatoIncorrecto_EsInvalido(string iban)
        => Assert.False(EsValido(iban));

    [Fact]
    public void Iban_LongitudExacta_22Caracteres()
    {
        var iban = "CR21015200009123456789";
        Assert.Equal(22, iban.Length);
        Assert.True(EsValido(iban));
    }
}
