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

    // ── Dashboard Dueño ──────────────────────────────────────────────────────
    public async Task<object> GetDashboardDuenoAsync(int duenoId)
    {
        var total      = await _db.Activos.CountAsync(a => a.IdDueno == duenoId);
        var activas    = await _db.Activos.CountAsync(a => a.IdDueno == duenoId && a.Estado == EstadoActivoEnum.Aprobada);
        var enRevision = await _db.Activos.CountAsync(a => a.IdDueno == duenoId && a.Estado == EstadoActivoEnum.EnRevision);

        var proximo = await _db.PagosMensuales
            .Where(p => p.Plan.Activo.IdDueno == duenoId && p.Estado == EstadoPagoEnum.Pendiente)
            .OrderBy(p => p.FechaPago)
            .Select(p => new { p.Monto, p.FechaPago })
            .FirstOrDefaultAsync();

        return new
        {
            totalFincas      = total,
            fincasActivas    = activas,
            fincasEnRevision = enRevision,
            proximoPago      = proximo == null ? null : (object)new
            {
                monto = proximo.Monto,
                fecha = proximo.FechaPago.ToString("dd/MM/yyyy")
            }
        };
    }

    public async Task<List<object>> GetFincasRecientesAsync(int duenoId, int limit = 5)
    {
        var raw = await _db.Activos
            .Where(a => a.IdDueno == duenoId)
            .OrderByDescending(a => a.FechaRegistro)
            .Take(limit)
            .Select(a => new { a.Id, a.Lat, a.Lng, a.Hectareas, a.Estado })
            .ToListAsync();

        return raw.Select(a => (object)new
        {
            a.Id,
            Nombre      = $"Finca #{a.Id}",
            Coordenadas = $"Lat {a.Lat:F4}, Lng {a.Lng:F4}",
            a.Hectareas,
            Estado      = a.Estado.ToString()
        }).ToList();
    }

    // ── CU26 Historial Pagos ─────────────────────────────────────────────────
    public async Task<List<object>> GetPagosHistorialAsync(int duenoId)
    {
        return await _db.PagosMensuales
            .Include(p => p.Plan)
                .ThenInclude(pl => pl.Activo)
            .Where(p => p.Plan.Activo.IdDueno == duenoId)
            .OrderByDescending(p => p.Plan.FechaActivacion)
            .ThenBy(p => p.NumeroPago)
            .Select(p => (object)new
            {
                p.Id,
                p.NumeroPago,
                FincaId       = p.Plan.IdActivo,
                p.Monto,
                FechaPago     = p.FechaPago.ToString("dd/MM/yyyy"),
                FechaEjecucion= p.FechaEjecucion.HasValue
                                    ? p.FechaEjecucion.Value.ToString("dd/MM/yyyy")
                                    : "",
                Estado        = p.Estado.ToString()
            })
            .ToListAsync();
    }
}
