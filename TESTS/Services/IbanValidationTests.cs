using Microsoft.Extensions.Configuration;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests.Services;

/// <summary>
/// Tests unitarios de BankAccountService.ValidarIban y Ofuscar.
/// IBAN Costa Rica: CR + 20 dígitos exactos.
/// </summary>
public class IbanValidationTests
{
    private static BankAccountService CrearServicio()
    {
        // Clave de 32 bytes válida para pruebas
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "T1J5k6K8FaIs+fxIbQyRhaJldV2dygi22Wrrk2E3a64="
            })
            .Build();

        return new BankAccountService(config);
    }

    // ── Válidos ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("CR21000100020003000456789")]   // 22 chars total: CR + 20 dígitos
    [InlineData("CR00000000000000000000")]       // Mínimo: todos ceros
    [InlineData("CR99999999999999999999")]       // Máximo: todos nueves
    public void ValidarIban_IbanCorrecto_RetornaTrue(string iban)
    {
        var svc = CrearServicio();
        Assert.True(svc.ValidarIban(iban));
    }

    // ── Inválidos ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]                             // vacío
    [InlineData("CR2100010002000300045678")]     // 21 dígitos (faltan 1)
    [InlineData("CR210001000200030004567890")]   // 21 dígitos (sobra 1)
    [InlineData("US21000100020003000456789")]    // código de país incorrecto
    [InlineData("CR2100010002000300045678X")]    // letra al final
    [InlineData("cr21000100020003000456789")]    // minúsculas
    [InlineData("21000100020003000456789CR")]    // CR al final
    public void ValidarIban_IbanIncorrecto_RetornaFalse(string iban)
    {
        var svc = CrearServicio();
        Assert.False(svc.ValidarIban(iban));
    }

    // ── Ofuscación ───────────────────────────────────────────────────────────

    [Fact]
    public void Ofuscar_RetornaPrimerosCuatroYUltimosCuatro()
    {
        var svc    = CrearServicio();
        var iban   = "CR21000100020003000456789"; // 25 chars ficticio
        var result = svc.Ofuscar(iban);

        Assert.StartsWith("CR21", result);
        Assert.EndsWith("6789", result);
        Assert.Contains("****", result);
    }

    // ── Cifrado/Descifrado AES-256-GCM ───────────────────────────────────────

    [Fact]
    public void CifrarDescifrar_RoundTrip_Correcto()
    {
        var svc    = CrearServicio();
        var iban   = "CR21000100020003000456789";
        var cifrado = svc.Cifrar(iban);

        Assert.NotEqual(iban, cifrado);
        Assert.Equal(iban, svc.Descifrar(cifrado));
    }

    [Fact]
    public void Cifrar_MismoTexto_ProduceCifradosDiferentes()
    {
        // AES-GCM usa nonce aleatorio → el cifrado debe ser diferente cada vez
        var svc  = CrearServicio();
        var iban = "CR21000100020003000456789";

        Assert.NotEqual(svc.Cifrar(iban), svc.Cifrar(iban));
    }
}
