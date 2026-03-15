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

public class DictamenTests
{
    private static NativaDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<NativaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NativaDbContext(opts);
    }

    private static IngenieroService CreateService(NativaDbContext db)
    {
        var mockEmail = new Mock<EmailService>(
            Mock.Of<IConfiguration>(), Mock.Of<ILogger<EmailService>>());
        mockEmail.Setup(e => e.EnviarGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);

        var calc = new CalculadoraService();
        return new IngenieroService(db, mockEmail.Object, calc);
    }

    private static async Task<(NativaDbContext db, Activo finca, int ingId)> SeedFincaEnRevisionAsync()
    {
        var db = CreateDb();

        var dueno = new Sujeto
        {
            Cedula = "1-0001-0001", Nombre = "Dueño Test", Correo = "dueno@test.com",
            PasswordHash = "hash", Rol = RolEnum.Dueno, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var ing = new Sujeto
        {
            Cedula = "1-0002-0002", Nombre = "Ing Test", Correo = "ing@test.com",
            PasswordHash = "hash", Rol = RolEnum.Ingeniero, Estado = EstadoSujetoEnum.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Sujetos.AddRange(dueno, ing);
        await db.SaveChangesAsync();

        var finca = new Activo
        {
            IdDueno = dueno.Id, IdIngeniero = ing.Id,
            Hectareas = 10m, Vegetacion = 50m, Hidrologia = 30m, Topografia = 20m,
            EsNacional = false, Lat = 9.7m, Lng = -83.7m,
            Estado = EstadoActivoEnum.EnRevision,
            FechaRegistro = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow
        };
        db.Activos.Add(finca);
        await db.SaveChangesAsync();

        return (db, finca, ing.Id);
    }

    [Fact]
    public async Task Dictamen_Rechazar_SinObservaciones_RetornaError()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, mensaje) = await svc.DictamenAsync(finca.Id, "Rechazar", "", ingId);

        Assert.False(ok);
        Assert.Contains("observaciones", mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dictamen_Devolver_SinObservaciones_RetornaError()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, _) = await svc.DictamenAsync(finca.Id, "Devolver", null, ingId);

        Assert.False(ok);
    }

    [Fact]
    public async Task Dictamen_Aprobar_SinObservaciones_OK()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, _) = await svc.DictamenAsync(finca.Id, "Aprobar", null, ingId);

        Assert.True(ok);
        var fincaActualizada = await db.Activos.FindAsync(finca.Id);
        Assert.Equal(EstadoActivoEnum.Aprobada, fincaActualizada!.Estado);
    }

    [Fact]
    public async Task Dictamen_Devolver_CambiaEstadoDevueltaYLimpiaIngeniero()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, _) = await svc.DictamenAsync(finca.Id, "Devolver", "Faltan planos", ingId);

        Assert.True(ok);
        var f = await db.Activos.FindAsync(finca.Id);
        Assert.Equal(EstadoActivoEnum.Devuelta, f!.Estado);
        Assert.Null(f.IdIngeniero);
        Assert.Equal("Faltan planos", f.Observaciones);
    }

    [Fact]
    public async Task Dictamen_Rechazar_ConObservaciones_MarcaRechazada()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, _) = await svc.DictamenAsync(finca.Id, "Rechazar", "No cumple requisitos", ingId);

        Assert.True(ok);
        var f = await db.Activos.FindAsync(finca.Id);
        Assert.Equal(EstadoActivoEnum.Rechazada, f!.Estado);
    }

    [Fact]
    public async Task Dictamen_TipoInvalido_RetornaError()
    {
        var (db, finca, ingId) = await SeedFincaEnRevisionAsync();
        var svc = CreateService(db);

        var (ok, mensaje) = await svc.DictamenAsync(finca.Id, "Cancelar", "obs", ingId);

        Assert.False(ok);
        Assert.Contains("inválido", mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
