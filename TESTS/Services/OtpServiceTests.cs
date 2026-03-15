using Microsoft.EntityFrameworkCore;
using Moq;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests.Services;

/// <summary>
/// Tests de OtpService con BD InMemory y EmailService mockeado.
/// Cubre: TTL expirado, bloqueo en 3er intento, cooldown 30s, límite reenvíos.
/// </summary>
public class OtpServiceTests
{
    private static NativaDbContext CrearDb(string nombre)
    {
        var opts = new DbContextOptionsBuilder<NativaDbContext>()
            .UseInMemoryDatabase(nombre)
            .Options;
        return new NativaDbContext(opts);
    }

    private static (OtpService svc, Mock<EmailService> emailMock, NativaDbContext db) Crear(string dbName)
    {
        var db    = CrearDb(dbName);
        var email = new Mock<EmailService>(MockBehavior.Loose, null!);
        var svc   = new OtpService(db, email.Object);
        return (svc, email, db);
    }

    private static Sujeto SujetoActivo(int id = 1) => new()
    {
        Id           = id,
        Cedula       = $"1-000{id}-0000",
        Nombre       = "Test User",
        Correo       = $"test{id}@nativa.cr",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!", 4),
        Rol          = RolEnum.Dueno,
        Estado       = EstadoSujetoEnum.Inactivo
    };

    [Fact]
    public async Task Verificar_OtpExpirado_RetornaError()
    {
        var (svc, _, db) = Crear("otp_expirado");
        var sujeto = SujetoActivo(1);
        db.Sujetos.Add(sujeto);

        // Sesión ya expirada (expiración en el pasado)
        db.OtpSesiones.Add(new OtpSesion
        {
            IdSujeto   = 1,
            HashOtp    = BCrypt.Net.BCrypt.HashPassword("123456", 4),
            Expiracion = DateTime.UtcNow.AddSeconds(-10),
            Usada      = false,
            Intentos   = 0
        });
        await db.SaveChangesAsync();

        var (ok, error) = await svc.VerificarAsync("test1@nativa.cr", "123456");

        Assert.False(ok);
        Assert.Contains("expirado", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Verificar_TresIntentosFallidos_BloqueuCuenta()
    {
        var (svc, emailMock, db) = Crear("otp_bloqueo");
        var sujeto = SujetoActivo(2);
        db.Sujetos.Add(sujeto);

        var otp    = "654321";
        var hash   = BCrypt.Net.BCrypt.HashPassword(otp, 4);

        db.OtpSesiones.Add(new OtpSesion
        {
            IdSujeto   = 2,
            HashOtp    = hash,
            Expiracion = DateTime.UtcNow.AddSeconds(90),
            Usada      = false,
            Intentos   = 0
        });
        await db.SaveChangesAsync();

        // Tres intentos con código incorrecto
        await svc.VerificarAsync("test2@nativa.cr", "000000");
        await svc.VerificarAsync("test2@nativa.cr", "000000");
        var (ok, error) = await svc.VerificarAsync("test2@nativa.cr", "000000");

        Assert.False(ok);

        var sujetoActualizado = await db.Sujetos.FindAsync(2);
        Assert.Equal(EstadoSujetoEnum.Bloqueado, sujetoActualizado!.Estado);

        // N03 debe haberse enviado
        emailMock.Verify(
            e => e.EnviarBloqueoOtpAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Verificar_OtpCorrecto_ActivaCuenta()
    {
        var (svc, _, db) = Crear("otp_correcto");
        var sujeto = SujetoActivo(3);
        db.Sujetos.Add(sujeto);

        var otp  = "111222";
        var hash = BCrypt.Net.BCrypt.HashPassword(otp, 4);
        db.OtpSesiones.Add(new OtpSesion
        {
            IdSujeto   = 3,
            HashOtp    = hash,
            Expiracion = DateTime.UtcNow.AddSeconds(90),
            Usada      = false,
            Intentos   = 0
        });
        await db.SaveChangesAsync();

        var (ok, error) = await svc.VerificarAsync("test3@nativa.cr", otp);

        Assert.True(ok);
        Assert.Null(error);

        var sujetoActualizado = await db.Sujetos.FindAsync(3);
        Assert.Equal(EstadoSujetoEnum.Activo, sujetoActualizado!.Estado);
    }

    [Fact]
    public async Task Reenviar_SinEsperarCooldown_RetornaError()
    {
        var (svc, _, db) = Crear("otp_cooldown");
        var sujeto = SujetoActivo(4);
        db.Sujetos.Add(sujeto);

        db.OtpSesiones.Add(new OtpSesion
        {
            IdSujeto      = 4,
            HashOtp       = BCrypt.Net.BCrypt.HashPassword("000000", 4),
            Expiracion    = DateTime.UtcNow.AddSeconds(90),
            Usada         = false,
            Intentos      = 0,
            UltimoReenvio = DateTime.UtcNow.AddSeconds(-10),  // hace 10s (< 30s)
            ConteoReenvios = 0
        });
        await db.SaveChangesAsync();

        var (ok, error) = await svc.ReenviarAsync("test4@nativa.cr");

        Assert.False(ok);
        Assert.Contains("30", error);
    }

    [Fact]
    public async Task Reenviar_LimiteTresReenvios_RetornaError()
    {
        var (svc, _, db) = Crear("otp_limite_reenvios");
        var sujeto = SujetoActivo(5);
        db.Sujetos.Add(sujeto);

        db.OtpSesiones.Add(new OtpSesion
        {
            IdSujeto       = 5,
            HashOtp        = BCrypt.Net.BCrypt.HashPassword("000000", 4),
            Expiracion     = DateTime.UtcNow.AddSeconds(90),
            Usada          = false,
            Intentos       = 0,
            ConteoReenvios = 3   // ya alcanzó el máximo
        });
        await db.SaveChangesAsync();

        var (ok, error) = await svc.ReenviarAsync("test5@nativa.cr");

        Assert.False(ok);
        Assert.Contains("Límite", error, StringComparison.OrdinalIgnoreCase);
    }
}
