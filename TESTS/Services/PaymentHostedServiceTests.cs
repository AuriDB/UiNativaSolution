using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests.Services;

/// <summary>
/// Tests del PaymentHostedService.
/// Cubre: pago vencido → Ejecutado, pago futuro → sin cambio,
///        pago #12 → Activo.Estado = Vencida, idempotencia (ya Ejecutado).
/// </summary>
public class PaymentHostedServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (IServiceProvider provider, Mock<EmailService> emailMock) CrearProvider(string dbName)
    {
        var sc        = new ServiceCollection();
        var emailMock = new Mock<EmailService>(MockBehavior.Loose, null!);

        sc.AddDbContext<NativaDbContext>(o => o.UseInMemoryDatabase(dbName));
        sc.AddScoped<EmailService>(_ => emailMock.Object);

        return (sc.BuildServiceProvider(), emailMock);
    }

    /// <summary>Invoca el método privado EjecutarPagosPendientesAsync vía reflexión.</summary>
    private static async Task EjecutarAsync(IServiceProvider provider)
    {
        var svc    = new PaymentHostedService(provider, NullLogger<PaymentHostedService>.Instance);
        var method = typeof(PaymentHostedService)
            .GetMethod("EjecutarPagosPendientesAsync",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(svc, null)!;
    }

    private static Sujeto Dueno() => new()
    {
        Id           = 1,
        Cedula       = "1-0001-0001",
        Nombre       = "Dueño PSA",
        Correo       = "dueno@nativa.cr",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!", 4),
        Rol          = RolEnum.Dueno,
        Estado       = EstadoSujetoEnum.Activo
    };

    private static Activo ActivoAprobado(int id) => new()
    {
        Id            = id,
        IdDueno       = 1,
        Hectareas     = 10m,
        Estado        = EstadoActivoEnum.Aprobada,
        FechaRegistro = DateTime.UtcNow.AddMonths(-13)
    };

    private static PlanPago Plan(int id, int activoId) => new()
    {
        Id                     = id,
        IdActivo               = activoId,
        IdIngeniero            = 1,
        FechaActivacion        = DateTime.UtcNow.AddMonths(-12),
        SnapshotParametrosJson = "{}",
        MontoMensual           = 150m
    };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ejecutar_PagoVencido_QuedaEjecutado()
    {
        var (provider, _) = CrearProvider("phs_vencido");

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
            db.Sujetos.Add(Dueno());
            db.Activos.Add(ActivoAprobado(1));
            db.PlanesPago.Add(Plan(1, 1));
            db.PagosMensuales.Add(new PagoMensual
            {
                Id = 1, IdPlan = 1, NumeroPago = 3, Monto = 150m,
                FechaPago = DateTime.UtcNow.AddSeconds(-30),   // vencido
                Estado    = EstadoPagoEnum.Pendiente
            });
            await db.SaveChangesAsync();
        }

        await EjecutarAsync(provider);

        using var scope2 = provider.CreateScope();
        var db2  = scope2.ServiceProvider.GetRequiredService<NativaDbContext>();
        var pago = await db2.PagosMensuales.FindAsync(1);

        Assert.Equal(EstadoPagoEnum.Ejecutado, pago!.Estado);
        Assert.NotNull(pago.FechaEjecucion);
    }

    [Fact]
    public async Task Ejecutar_PagoFuturo_SiguePendiente()
    {
        var (provider, _) = CrearProvider("phs_futuro");

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
            db.Sujetos.Add(Dueno());
            db.Activos.Add(ActivoAprobado(1));
            db.PlanesPago.Add(Plan(1, 1));
            db.PagosMensuales.Add(new PagoMensual
            {
                Id = 1, IdPlan = 1, NumeroPago = 2, Monto = 150m,
                FechaPago = DateTime.UtcNow.AddDays(30),   // futuro
                Estado    = EstadoPagoEnum.Pendiente
            });
            await db.SaveChangesAsync();
        }

        await EjecutarAsync(provider);

        using var scope2 = provider.CreateScope();
        var db2  = scope2.ServiceProvider.GetRequiredService<NativaDbContext>();
        var pago = await db2.PagosMensuales.FindAsync(1);

        Assert.Equal(EstadoPagoEnum.Pendiente, pago!.Estado);   // sin cambio
    }

    [Fact]
    public async Task Ejecutar_Pago12Vencido_ActivoQuedaVencida()
    {
        var (provider, _) = CrearProvider("phs_pago12");

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
            db.Sujetos.Add(Dueno());
            db.Activos.Add(ActivoAprobado(1));
            db.PlanesPago.Add(Plan(1, 1));
            db.PagosMensuales.Add(new PagoMensual
            {
                Id = 1, IdPlan = 1, NumeroPago = 12, Monto = 150m,
                FechaPago = DateTime.UtcNow.AddSeconds(-1),    // recién vencido
                Estado    = EstadoPagoEnum.Pendiente
            });
            await db.SaveChangesAsync();
        }

        await EjecutarAsync(provider);

        using var scope2 = provider.CreateScope();
        var db2   = scope2.ServiceProvider.GetRequiredService<NativaDbContext>();
        var activo = await db2.Activos.FindAsync(1);

        Assert.Equal(EstadoActivoEnum.Vencida, activo!.Estado);
    }

    [Fact]
    public async Task Ejecutar_PagoYaEjecutado_NoSeReprocesa()
    {
        var (provider, _) = CrearProvider("phs_idempotente");
        var fechaOriginal = DateTime.UtcNow.AddDays(-2);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NativaDbContext>();
            db.Sujetos.Add(Dueno());
            db.Activos.Add(ActivoAprobado(1));
            db.PlanesPago.Add(Plan(1, 1));
            db.PagosMensuales.Add(new PagoMensual
            {
                Id             = 1, IdPlan = 1, NumeroPago = 5, Monto = 150m,
                FechaPago      = DateTime.UtcNow.AddSeconds(-60),
                Estado         = EstadoPagoEnum.Ejecutado,     // ya ejecutado
                FechaEjecucion = fechaOriginal
            });
            await db.SaveChangesAsync();
        }

        await EjecutarAsync(provider);

        using var scope2 = provider.CreateScope();
        var db2  = scope2.ServiceProvider.GetRequiredService<NativaDbContext>();
        var pago = await db2.PagosMensuales.FindAsync(1);

        // El pago ya ejecutado no debe cambiar su FechaEjecucion
        Assert.Equal(EstadoPagoEnum.Ejecutado, pago!.Estado);
        Assert.Equal(fechaOriginal.Date, pago.FechaEjecucion!.Value.Date);
    }
}
