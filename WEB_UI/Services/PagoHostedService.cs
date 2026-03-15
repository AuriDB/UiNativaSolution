using Microsoft.EntityFrameworkCore;
using WEB_UI.Data;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;

namespace WEB_UI.Services;

public class PagoHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PagoHostedService> _logger;

    public PagoHostedService(IServiceScopeFactory scopeFactory, ILogger<PagoHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PagoHostedService iniciado.");

        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        // Ejecutar inmediatamente al iniciar (para no esperar 24h en dev/test)
        await ProcesarPagosAsync();

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcesarPagosAsync();
        }
    }

    private async Task ProcesarPagosAsync()
    {
        _logger.LogInformation("PagoHostedService: iniciando ciclo {Fecha}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<EmailService>();

        var pagosPendientes = await db.PagosMensuales
            .Include(p => p.Plan)
                .ThenInclude(pl => pl.Activo)
                    .ThenInclude(a => a.Dueno)
            .Where(p => p.Estado == EstadoPagoEnum.Pendiente
                     && p.FechaPago <= DateTime.UtcNow)
            .ToListAsync();

        _logger.LogInformation("PagoHostedService: {Count} pagos a procesar.", pagosPendientes.Count);

        foreach (var pago in pagosPendientes)
        {
            try
            {
                await ProcesarPagoIndividualAsync(db, email, pago);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando PagoMensual ID={Id}", pago.Id);
                // Continúa con el siguiente pago
            }
        }

        _logger.LogInformation("PagoHostedService: ciclo completado.");
    }

    private async Task ProcesarPagoIndividualAsync(NativaDbContext db, EmailService email, PagoMensual pago)
    {
        var finca = pago.Plan.Activo;
        var dueno = finca.Dueno;

        // Marcar pago como ejecutado (transacción atómica)
        pago.Estado         = EstadoPagoEnum.Ejecutado;
        pago.FechaEjecucion = DateTime.UtcNow;
        await db.SaveChangesAsync();

        _logger.LogInformation("Pago #{Num} ejecutado — Finca {FincaId}, Monto ₡{Monto:N2}",
            pago.NumeroPago, finca.Id, pago.Monto);

        // N10 — notificar pago al Dueño
        _ = email.EnviarGenericoAsync(dueno.Correo,
            $"Tu pago mensual #{pago.NumeroPago} fue procesado — Sistema Nativa",
            $"<p>Hola <strong>{dueno.Nombre}</strong>,</p>" +
            $"<p>Tu pago mensual <strong>#{pago.NumeroPago}</strong> de " +
            $"<strong>₡{pago.Monto:N2}</strong> para la finca ID #{finca.Id} " +
            $"fue procesado el <strong>{pago.FechaEjecucion.Value:dd/MM/yyyy}</strong>.</p>" +
            $"<p>Gracias por ser parte del programa Nativa.</p>");

        // Pago #12 → Vencida + re-ingreso FIFO + N12
        if (pago.NumeroPago == 12)
        {
            await ProcesarVencimientoAsync(db, email, finca, dueno);
        }
    }

    private async Task ProcesarVencimientoAsync(NativaDbContext db, EmailService email, Activo finca, Sujeto dueno)
    {
        // Marcar finca como Vencida
        finca.Estado = EstadoActivoEnum.Vencida;

        // Re-ingresar copia a FIFO (nuevo período)
        var nuevaFinca = new Activo
        {
            IdDueno       = finca.IdDueno,
            Hectareas     = finca.Hectareas,
            Vegetacion    = finca.Vegetacion,
            Hidrologia    = finca.Hidrologia,
            Topografia    = finca.Topografia,
            EsNacional    = finca.EsNacional,
            Lat           = finca.Lat,
            Lng           = finca.Lng,
            Estado        = EstadoActivoEnum.Pendiente,
            FechaRegistro = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        };
        db.Activos.Add(nuevaFinca);
        await db.SaveChangesAsync();

        _logger.LogInformation("Finca {Id} vencida. Nueva finca {NuevaId} ingresada a FIFO.", finca.Id, nuevaFinca.Id);

        // N12 — notificar vencimiento
        _ = email.EnviarGenericoAsync(dueno.Correo,
            "Tu contrato PSA ha concluido — Sistema Nativa",
            $"<p>Hola <strong>{dueno.Nombre}</strong>,</p>" +
            $"<p>Tu contrato de Pago por Servicios Ambientales para la finca ID #{finca.Id} " +
            $"ha concluido exitosamente (pago #12 procesado).</p>" +
            $"<p>Tu propiedad ha sido ingresada nuevamente al programa para un nuevo período de evaluación. " +
            $"Pronto recibirás noticias de un ingeniero evaluador.</p>" +
            $"<p>¡Gracias por tu compromiso con el medio ambiente!</p>");
    }
}
