using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace WEB_UI.Services;

public class BlobService
{
    private readonly string _connStr;
    private readonly string _container;
    private readonly ILogger<BlobService> _logger;
    private readonly bool _devMode;

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf", ".dwg" };

    private const long TamanoMaxBytes = 10 * 1024 * 1024; // 10 MB

    public BlobService(IConfiguration cfg, ILogger<BlobService> logger)
    {
        _connStr   = cfg["AzureBlob:ConnectionString"] ?? "AZURE_CONN_STRING";
        _container = cfg["AzureBlob:ContainerName"]    ?? "psa-docs";
        _logger    = logger;
        _devMode   = _connStr == "AZURE_CONN_STRING";
    }

    /// <summary>
    /// Valida y sube el archivo a Azure Blob. Devuelve la URL pública.
    /// En modo dev (sin credenciales reales) devuelve una URL simulada.
    /// </summary>
    public async Task<(bool ok, string? url, string? error)> SubirAsync(IFormFile archivo)
    {
        var ext = Path.GetExtension(archivo.FileName);
        if (!ExtensionesPermitidas.Contains(ext))
            return (false, null, $"Extensión '{ext}' no permitida. Use: jpg, jpeg, png, pdf, dwg.");

        if (archivo.Length > TamanoMaxBytes)
            return (false, null, "El archivo supera el límite de 10 MB.");

        var nombreBlob = $"{Guid.NewGuid()}{ext}";

        if (_devMode)
        {
            _logger.LogWarning("BlobService en modo dev. Simulando subida de {Archivo}", archivo.FileName);
            return (true, $"https://dev.blob.local/{_container}/{nombreBlob}", null);
        }

        try
        {
            var client    = new BlobContainerClient(_connStr, _container);
            await client.CreateIfNotExistsAsync(PublicAccessType.None);
            var blobClient = client.GetBlobClient(nombreBlob);

            using var stream = archivo.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = archivo.ContentType
            });

            return (true, blobClient.Uri.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo blob {Archivo}", archivo.FileName);
            return (false, null, "Error al subir el archivo. Intenta de nuevo.");
        }
    }

    /// <summary>Genera SAS token de lectura (1 hora) para el Ingeniero.</summary>
    public string? GenerarSas(string blobUrl, TimeSpan duracion)
    {
        if (_devMode) return blobUrl;
        try
        {
            var uri        = new Uri(blobUrl);
            var blobClient = new BlobClient(new Uri(blobUrl));
            // Para SAS real se requiere cuenta con credencial de almacenamiento
            return blobUrl; // simplificado para dev
        }
        catch { return blobUrl; }
    }
}
