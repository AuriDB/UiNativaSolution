using Microsoft.EntityFrameworkCore;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;

namespace WEB_UI.Services;


/// <summary>
/// Servicio en background que ejecuta los PagosMensuales cuya FechaPago ya venció.
/// Corre cada hora. Usa un scope propio para obtener el DbContext (Scoped desde Singleton).
/// </summary>
public class PaymentHostedService : BackgroundService
{
    private readonly IServiceProvider              _services;
    private readonly ILogger<PaymentHostedService> _logger;

    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    public PaymentHostedService(IServiceProvider services, ILogger<PaymentHostedService> logger)
    {
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentHostedService iniciado — intervalo: {Intervalo}h.", Intervalo.TotalHours);

        // Ejecutar una vez al arrancar, luego cada hora
        while (!stoppingToken.IsCancellationRequested)
        {
            await EjecutarPagosPendientesAsync();
            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task EjecutarPagosPendientesAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var db    = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
            var email = scope.ServiceProvider.GetRequiredService<EmailService>();

            var ahora = DateTime.UtcNow;

            var pendientes = await db.PagosMensuales
                .Include(p => p.Plan)
                    .ThenInclude(pl => pl!.Activo)
                        .ThenInclude(a => a!.Dueno)
                .Where(p => p.Estado == EstadoPagoEnum.Pendiente && p.FechaPago <= ahora)
                .ToListAsync();

            if (!pendientes.Any())
            {
                _logger.LogDebug("No hay pagos pendientes a ejecutar.");
                return;
            }

            var activosVencidos = new List<int>();

            foreach (var pago in pendientes)
            {
                pago.Estado         = EstadoPagoEnum.Ejecutado;
                pago.FechaEjecucion = ahora;

                // Pago #12 (último) → el contrato venció; el activo regresa a Vencida
                if (pago.NumeroPago == 12)
                    activosVencidos.Add(pago.Plan!.IdActivo);
            }

            // Marcar activos como Vencida (idempotente: solo si aún están Aprobada)
            if (activosVencidos.Any())
            {
                var activos = await db.Activos
                    .Where(a => activosVencidos.Contains(a.Id) &&
                                a.Estado == EstadoActivoEnum.Aprobada)
                    .ToListAsync();

                foreach (var activo in activos)
                    activo.Estado = EstadoActivoEnum.Vencida;
            }

            await db.SaveChangesAsync();

            // N10/N12 — notificaciones por email (no bloquear si falla)
            foreach (var pago in pendientes)
            {
                var dueno = pago.Plan?.Activo?.Dueno;
                if (dueno?.Correo == null) continue;

                try
                {
                    await email.EnviarPagoEjecutadoAsync(
                        dueno.Correo, dueno.Nombre!, pago.NumeroPago, pago.Monto, ahora);

                    // N12 — contrato vencido al ejecutar pago #12
                    if (pago.NumeroPago == 12)
                        await email.EnviarContratoVencidoAsync(
                            dueno.Correo, dueno.Nombre!, pago.Plan!.IdActivo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo enviar email de pago #{Num}.", pago.NumeroPago);
                }
            }

            _logger.LogInformation(
                "Pagos ejecutados: {Count} pagos procesados a las {Hora} UTC. Contratos vencidos: {Vencidos}.",
                pendientes.Count, ahora.ToString("HH:mm:ss"), activosVencidos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar pagos pendientes.");
        }
    }
}
