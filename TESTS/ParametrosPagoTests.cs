using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WEB_UI.Data;
using WEB_UI.Models.Entities;
using WEB_UI.Models.Enums;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests;

public class ParametrosPagoTests
{
    private static NativaDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<NativaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NativaDbContext(opts);
    }

    private static AdminService CreateAdminService(NativaDbContext db)
    {
        var mockEmail = new Mock<EmailService>(
            Mock.Of<IConfiguration>(), Mock.Of<ILogger<EmailService>>());
        mockEmail.Setup(e => e.EnviarGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);

        return new AdminService(db, mockEmail.Object, new CalculadoraService());
    }

    [Fact]
    public async Task CrearParametros_OpcionA_InactivalVigenteAnterior()
    {
        var db = CreateDb();

        // Seed admin + parámetros vigentes existentes
        var admin = new Sujeto
        {
            Cedula = "1-0001-0001", Nombre = "Admin", Correo = "admin@test.com",
            PasswordHash = "h", Rol = RolEnum.Admin, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Sujetos.Add(admin);
        var viejos = new ParametrosPago
        {
            PrecioBase = 5000m, PctVegetacion = 0.1m, PctHidrologia = 0.1m,
            PctNacional = 0.05m, PctTopografia = 0.07m, Tope = 0.5m,
            Vigente = true, FechaCreacion = DateTime.UtcNow, CreadoPor = 1
        };
        db.ParametrosPago.Add(viejos);
        await db.SaveChangesAsync();

        var svc = CreateAdminService(db);
        var (ok, _) = await svc.CrearParametrosAsync(
            10000m, 0.15m, 0.10m, 0.05m, 0.08m, 0.50m, "A", admin.Id);

        Assert.True(ok);
        var todos = await db.ParametrosPago.ToListAsync();
        Assert.Equal(2, todos.Count);
        Assert.Single(todos.Where(p => p.Vigente));
        Assert.False(todos.First(p => p.Id == viejos.Id).Vigente);
    }

    [Fact]
    public async Task CrearParametros_OpcionB_NoModificaPagosEjecutados()
    {
        var db = CreateDb();

        var dueno = new Sujeto
        {
            Cedula = "1-0002-0002", Nombre = "Dueño", Correo = "d@test.com",
            PasswordHash = "h", Rol = RolEnum.Dueno, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var ing = new Sujeto
        {
            Cedula = "1-0003-0003", Nombre = "Ing", Correo = "i@test.com",
            PasswordHash = "h", Rol = RolEnum.Ingeniero, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var admin = new Sujeto
        {
            Cedula = "1-0001-0001", Nombre = "Admin", Correo = "a@test.com",
            PasswordHash = "h", Rol = RolEnum.Admin, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Sujetos.AddRange(dueno, ing, admin);
        var finca = new Activo
        {
            Hectareas = 10m, Vegetacion = 50m, Hidrologia = 30m, Topografia = 20m,
            EsNacional = false, Lat = 9m, Lng = -84m, Estado = EstadoActivoEnum.Aprobada,
            FechaRegistro = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow
        };
        db.Activos.Add(finca);

        var paramVigente = new ParametrosPago
        {
            PrecioBase = 5000m, PctVegetacion = 0.1m, PctHidrologia = 0.1m,
            PctNacional = 0.05m, PctTopografia = 0.07m, Tope = 0.5m,
            Vigente = true, FechaCreacion = DateTime.UtcNow, CreadoPor = 1
        };
        db.ParametrosPago.Add(paramVigente);
        await db.SaveChangesAsync();

        finca.IdDueno = dueno.Id;
        var plan = new PlanPago
        {
            IdActivo = finca.Id, IdIngeniero = ing.Id,
            FechaActivacion = DateTime.UtcNow.AddMonths(-2),
            SnapshotParametrosJson = "{}",
            MontoMensual = 62500m, FechaCreacion = DateTime.UtcNow
        };
        db.PlanesPago.Add(plan);
        await db.SaveChangesAsync();

        var montoEjecutadoOriginal = 62500m;
        var pagoEjecutado = new PagoMensual
        {
            IdPlan = plan.Id, NumeroPago = 1, Monto = montoEjecutadoOriginal,
            FechaPago = DateTime.UtcNow.AddDays(-30),
            Estado = EstadoPagoEnum.Ejecutado,
            FechaEjecucion = DateTime.UtcNow.AddDays(-1),
            FechaCreacion = DateTime.UtcNow
        };
        var pagoPendiente = new PagoMensual
        {
            IdPlan = plan.Id, NumeroPago = 2, Monto = montoEjecutadoOriginal,
            FechaPago = DateTime.UtcNow.AddDays(30),
            Estado = EstadoPagoEnum.Pendiente,
            FechaCreacion = DateTime.UtcNow
        };
        db.PagosMensuales.AddRange(pagoEjecutado, pagoPendiente);
        await db.SaveChangesAsync();

        var svc = CreateAdminService(db);
        await svc.CrearParametrosAsync(10000m, 0.15m, 0.10m, 0.05m, 0.08m, 0.50m, "B", admin.Id);

        // El pago ejecutado NO debe cambiar
        var ejec = await db.PagosMensuales.FindAsync(pagoEjecutado.Id);
        Assert.Equal(montoEjecutadoOriginal, ejec!.Monto);

        // El pago pendiente SÍ debe recalcularse
        var pend = await db.PagosMensuales.FindAsync(pagoPendiente.Id);
        Assert.NotEqual(montoEjecutadoOriginal, pend!.Monto);
    }
}
