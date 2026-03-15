using Microsoft.EntityFrameworkCore;
using WEB_UI.Data;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Services;

public class ActivoService
{
    private readonly NativaDbContext _db;
    private readonly BlobService     _blob;
    private readonly EmailService    _email;

    public ActivoService(NativaDbContext db, BlobService blob, EmailService email)
    {
        _db    = db;
        _blob  = blob;
        _email = email;
    }

    // ── CU11 Registrar Finca ─────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje, int id)> RegistrarAsync(
        decimal hectareas, decimal vegetacion, decimal hidrologia, decimal topografia,
        bool esNacional, decimal lat, decimal lng,
        int duenoId, IFormFileCollection archivos)
    {
        if (hectareas <= 0)
            return (false, "Las hectáreas deben ser mayores a 0.", 0);

        var finca = new Activo
        {
            IdDueno      = duenoId,
            Hectareas    = hectareas,
            Vegetacion   = vegetacion,
            Hidrologia   = hidrologia,
            Topografia   = topografia,
            EsNacional   = esNacional,
            Lat          = lat,
            Lng          = lng,
            Estado       = EstadoActivoEnum.Pendiente,
            FechaRegistro = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        };
        _db.Activos.Add(finca);
        await _db.SaveChangesAsync();

        // Subir adjuntos
        foreach (var archivo in archivos)
        {
            var (ok, url, _) = await _blob.SubirAsync(archivo);
            if (ok && url is not null)
            {
                _db.AdjuntosActivos.Add(new AdjuntoActivo
                {
                    IdActivo      = finca.Id,
                    BlobUrl       = url,
                    NombreArchivo = archivo.FileName,
                    FechaSubida   = DateTime.UtcNow,
                    FechaCreacion = DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync();
        return (true, "Finca registrada correctamente.", finca.Id);
    }

    // ── CU12 Listar mis Fincas (JSON para Ag-Grid) ───────────────────────────
    public async Task<List<object>> ListarFincasDuenoAsync(int duenoId)
    {
        return await _db.Activos
            .Where(a => a.IdDueno == duenoId)
            .OrderByDescending(a => a.FechaRegistro)
            .Select(a => (object)new
            {
                a.Id,
                a.Hectareas,
                a.Vegetacion,
                a.Hidrologia,
                a.Topografia,
                a.EsNacional,
                a.Lat,
                a.Lng,
                Estado          = a.Estado.ToString(),
                EstadoNum       = (int)a.Estado,
                FechaRegistro   = a.FechaRegistro.ToString("dd/MM/yyyy HH:mm"),
                RowVersion      = Convert.ToBase64String(a.RowVersion)
            })
            .ToListAsync();
    }

    // ── CU13 Detalle Finca ───────────────────────────────────────────────────
    public async Task<Activo?> GetDetalleAsync(int id, int duenoId)
        => await _db.Activos
            .Include(a => a.Adjuntos)
            .FirstOrDefaultAsync(a => a.Id == id && a.IdDueno == duenoId);

    // ── CU14 Editar Finca (solo Devuelta) ────────────────────────────────────
    public async Task<(bool ok, string mensaje)> EditarAsync(
        int id, int duenoId,
        decimal hectareas, decimal vegetacion, decimal hidrologia, decimal topografia,
        bool esNacional, decimal lat, decimal lng, string? observaciones,
        IFormFileCollection nuevosArchivos)
    {
        var finca = await _db.Activos
            .Include(a => a.Adjuntos)
            .FirstOrDefaultAsync(a => a.Id == id && a.IdDueno == duenoId);

        if (finca is null)       return (false, "Finca no encontrada.");
        if (finca.Estado != EstadoActivoEnum.Devuelta)
            return (false, "Solo se pueden editar fincas en estado Devuelta.");

        finca.Hectareas    = hectareas;
        finca.Vegetacion   = vegetacion;
        finca.Hidrologia   = hidrologia;
        finca.Topografia   = topografia;
        finca.EsNacional   = esNacional;
        finca.Lat          = lat;
        finca.Lng          = lng;
        finca.Observaciones = observaciones;

        foreach (var archivo in nuevosArchivos)
        {
            var (ok, url, _) = await _blob.SubirAsync(archivo);
            if (ok && url is not null)
                _db.AdjuntosActivos.Add(new AdjuntoActivo
                {
                    IdActivo      = finca.Id,
                    BlobUrl       = url,
                    NombreArchivo = archivo.FileName,
                    FechaSubida   = DateTime.UtcNow,
                    FechaCreacion = DateTime.UtcNow
                });
        }

        await _db.SaveChangesAsync();
        return (true, "Finca actualizada correctamente.");
    }

    // ── CU15 Reenviar a FIFO ─────────────────────────────────────────────────
    public async Task<(bool ok, string mensaje)> ReenviarAsync(int id, int duenoId)
    {
        var finca = await _db.Activos
            .FirstOrDefaultAsync(a => a.Id == id && a.IdDueno == duenoId);

        if (finca is null)       return (false, "Finca no encontrada.");
        if (finca.Estado != EstadoActivoEnum.Devuelta)
            return (false, "Solo se puede reenviar una finca en estado Devuelta.");

        finca.Estado       = EstadoActivoEnum.Pendiente;
        finca.IdIngeniero  = null;
        finca.FechaRegistro = DateTime.UtcNow; // nuevo timestamp FIFO
        await _db.SaveChangesAsync();
        return (true, "Finca reenviada a evaluación.");
    }

    // ── CU16 Adjuntos ────────────────────────────────────────────────────────
    public async Task<List<AdjuntoActivo>> GetAdjuntosAsync(int activoId)
        => await _db.AdjuntosActivos
            .Where(a => a.IdActivo == activoId)
            .OrderBy(a => a.FechaSubida)
            .ToListAsync();

    // ── CU23 Registrar/Actualizar IBAN ───────────────────────────────────────
    public async Task<(bool ok, string mensaje)> RegistrarIbanAsync(
        int duenoId, string banco, string tipoCuenta, string titular,
        string iban, EncryptionService enc)
    {
        // Validar formato IBAN CR + 20 dígitos
        if (!System.Text.RegularExpressions.Regex.IsMatch(iban, @"^CR\d{20}$"))
            return (false, "El IBAN debe tener el formato CR seguido de 20 dígitos.");

        // Desactivar el anterior
        var cuentasActivas = await _db.CuentasBancarias
            .Where(c => c.IdDueno == duenoId && c.Activo)
            .ToListAsync();
        foreach (var c in cuentasActivas) c.Activo = false;

        _db.CuentasBancarias.Add(new CuentaBancaria
        {
            IdDueno       = duenoId,
            Banco         = banco,
            TipoCuenta    = tipoCuenta,
            Titular       = titular,
            IbanCompleto  = enc.Cifrar(iban),
            IbanOfuscado  = EncryptionService.Ofuscar(iban),
            Activo        = true,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return (true, "Cuenta bancaria registrada correctamente.");
    }

    public async Task<CuentaBancaria?> GetCuentaActivaAsync(int duenoId)
        => await _db.CuentasBancarias
            .FirstOrDefaultAsync(c => c.IdDueno == duenoId && c.Activo);
}
