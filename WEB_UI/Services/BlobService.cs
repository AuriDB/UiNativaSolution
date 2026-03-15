namespace WEB_UI.Services;

/// <summary>
/// Almacenamiento de adjuntos.
/// Desarrollo: guarda en wwwroot/uploads/.
/// Producción: reemplazar implementación por Azure Blob Storage.
/// </summary>
public class BlobService
{
    private readonly IWebHostEnvironment _env;

    // Extensiones permitidas para adjuntos de fincas
    private static readonly HashSet<string> _extensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

    public BlobService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // Sube el archivo y retorna la URL pública
    public async Task<(bool ok, string urlOError)> SubirAsync(IFormFile archivo, string carpeta = "adjuntos")
    {
        var ext = Path.GetExtension(archivo.FileName);

        if (!_extensionesPermitidas.Contains(ext))
            return (false, $"Extensión no permitida: {ext}");

        if (archivo.Length > 10 * 1024 * 1024) // 10 MB
            return (false, "El archivo supera el tamaño máximo de 10 MB.");

        var directorio = Path.Combine(_env.WebRootPath, "uploads", carpeta);
        Directory.CreateDirectory(directorio);

        // Nombre único para evitar colisiones
        var nombreArchivo = $"{Guid.NewGuid():N}{ext}";
        var rutaFisica    = Path.Combine(directorio, nombreArchivo);

        using var stream = File.Create(rutaFisica);
        await archivo.CopyToAsync(stream);

        return (true, $"/uploads/{carpeta}/{nombreArchivo}");
    }

    // Elimina el archivo físico (solo aplica en dev con almacenamiento local)
    public Task EliminarAsync(string? blobUrl)
    {
        if (!string.IsNullOrEmpty(blobUrl) && blobUrl.StartsWith("/uploads/"))
        {
            var ruta = Path.Combine(_env.WebRootPath, blobUrl.TrimStart('/'));
            if (File.Exists(ruta)) File.Delete(ruta);
        }
        return Task.CompletedTask;
    }
}
