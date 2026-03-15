using System.Security.Cryptography;
using System.Text;

namespace WEB_UI.Services;

public class EncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration cfg)
    {
        var keyBase64 = cfg["Encryption:Key"] ?? "";
        try   { _key = Convert.FromBase64String(keyBase64); }
        catch { _key = Encoding.UTF8.GetBytes("dev_fallback_key_32bytes_here!!!"); }
        if (_key.Length < 16) _key = Encoding.UTF8.GetBytes("dev_fallback_key_32bytes_here!!!");
    }

    public string Cifrar(string texto)
    {
        using var aes = Aes.Create();
        aes.Key = _key[..32 <= _key.Length ? 32 : _key.Length];
        aes.GenerateIV();
        using var enc  = aes.CreateEncryptor();
        var bytes  = Encoding.UTF8.GetBytes(texto);
        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        var result = new byte[aes.IV.Length + cipher.Length];
        aes.IV.CopyTo(result, 0);
        cipher.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    public string Descifrar(string cifrado)
    {
        var data = Convert.FromBase64String(cifrado);
        using var aes = Aes.Create();
        aes.Key = _key[..32 <= _key.Length ? 32 : _key.Length];
        var iv     = data[..16];
        var cipher = data[16..];
        aes.IV = iv;
        using var dec  = aes.CreateDecryptor();
        var bytes = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>CR******************** para Admin</summary>
    public static string Ofuscar(string iban) =>
        iban.Length >= 2 ? iban[..2] + new string('*', iban.Length - 2) : iban;
}
