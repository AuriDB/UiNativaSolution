using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WEB_UI.Services;

/// <summary>
/// Servicio de cuentas bancarias:
/// - Validación IBAN CR (^CR\d{20}$)
/// - Cifrado AES-256-GCM para IbanCompleto
/// - Ofuscación para mostrar al usuario
/// </summary>
public class BankAccountService
{
    private readonly byte[] _key;

    private static readonly Regex IbanRegex =
        new(@"^CR\d{20}$", RegexOptions.Compiled);

    public BankAccountService(IConfiguration configuration)
    {
        var b64 = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key no configurada en appsettings.");

        _key = Convert.FromBase64String(b64);

        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key debe ser de 32 bytes (256 bits).");
    }

    // ── Validación ──────────────────────────────────────────────────────────

    public bool ValidarIban(string iban) =>
        !string.IsNullOrWhiteSpace(iban) && IbanRegex.IsMatch(iban.Trim().ToUpper());

    // ── Cifrado AES-256-GCM ─────────────────────────────────────────────────

    /// <summary>Cifra un texto plano. Resultado: "base64Nonce.base64Cipher.base64Tag"</summary>
    public string Cifrar(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce      = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        var cipherText = new byte[plainBytes.Length];
        var tag        = new byte[AesGcm.TagByteSizes.MaxSize];   // 16 bytes

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(cipherText)}.{Convert.ToBase64String(tag)}";
    }

    /// <summary>Descifra un texto cifrado con formato "nonce.cipher.tag".</summary>
    public string Descifrar(string encrypted)
    {
        var parts = encrypted.Split('.');
        if (parts.Length != 3)
            throw new InvalidOperationException("Formato de IBAN cifrado inválido.");

        var nonce      = Convert.FromBase64String(parts[0]);
        var cipherText = Convert.FromBase64String(parts[1]);
        var tag        = Convert.FromBase64String(parts[2]);
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // ── Ofuscación ──────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra primeros 4 y últimos 4 caracteres, enmascara el centro.
    /// Ej: CR21 **** **** **** 5678
    /// </summary>
    public string Ofuscar(string iban)
    {
        if (string.IsNullOrEmpty(iban) || iban.Length < 8) return "****";
        return $"{iban[..4]}{"****"}{iban[^4..]}";
    }
}
