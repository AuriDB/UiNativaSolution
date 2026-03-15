using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Nativa.Domain.Entities;
using Nativa.Domain.Enums;
using Nativa.Infrastructure;
using WEB_UI.Controllers;
using WEB_UI.Services;
using Xunit;

namespace Nativa.Tests.Controllers;

/// <summary>
/// Tests del dictamen del Ingeniero (CU19).
/// Cubre: Aprobar → Aprobada, Rechazar sin observaciones → error,
///        Rechazar con observaciones → Rechazada, Devolver → Devuelta,
///        dictamen inválido → error, ingeniero no asignado → error.
/// </summary>
public class DictamenTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static NativaDbContext CrearDb(string nombre)
    {
        var opts = new DbContextOptionsBuilder<NativaDbContext>()
            .UseInMemoryDatabase(nombre)
            .Options;
        return new NativaDbContext(opts);
    }

    private static EngineerController CrearController(NativaDbContext db, int ingenieroId = 1)
    {
        var emailMock = new Mock<EmailService>(MockBehavior.Loose, null!);
        var calc      = new CalculatorService(db);
        var planSvc   = new PlanActivationService(db, calc);
        var ctrl      = new EngineerController(db, planSvc, emailMock.Object);

        var claims   = new[] { new Claim(ClaimTypes.NameIdentifier, ingenieroId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return ctrl;
    }

    /// <summary>Extrae el campo "success" del valor anónimo del JsonResult.</summary>
    private static bool ObtenerSuccess(JsonResult result) =>
        (bool)result.Value!.GetType().GetProperty("success")!.GetValue(result.Value)!;

    private static Sujeto Dueno() => new()
    {
        Id           = 10,
        Cedula       = "1-0010-0010",
        Nombre       = "Dueño",
        Correo       = "dueno@nativa.cr",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!", 4),
        Rol          = RolEnum.Dueno,
        Estado       = EstadoSujetoEnum.Activo
    };

    private static Activo ActivoEnRevision(int id, int ingenieroId = 1) => new()
    {
        Id            = id,
        IdDueno       = 10,
        IdIngeniero   = ingenieroId,
        Hectareas     = 5m,
        Estado        = EstadoActivoEnum.EnRevision,
        FechaRegistro = DateTime.UtcNow.AddDays(-3)
    };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluar_Aprobar_EstadoCambiaAAprobada()
    {
        var db = CrearDb("dict_aprobar");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1));
        await db.SaveChangesAsync();

        var ctrl   = CrearController(db);
        var result = await ctrl.Evaluar(new EvaluarRequest(1, "Aprobar", null));
        var json   = Assert.IsType<JsonResult>(result);

        Assert.True(ObtenerSuccess(json));

        var activo = await db.Activos.FindAsync(1);
        Assert.Equal(EstadoActivoEnum.Aprobada, activo!.Estado);
        Assert.Null(activo.Observaciones);
    }

    [Fact]
    public async Task Evaluar_RechazarSinObservaciones_RetornaError()
    {
        var db = CrearDb("dict_rechazar_sin_obs");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1));
        await db.SaveChangesAsync();

        var ctrl   = CrearController(db);
        var result = await ctrl.Evaluar(new EvaluarRequest(1, "Rechazar", null));
        var json   = Assert.IsType<JsonResult>(result);

        Assert.False(ObtenerSuccess(json));
        // Estado no cambia
        var activo = await db.Activos.FindAsync(1);
        Assert.Equal(EstadoActivoEnum.EnRevision, activo!.Estado);
    }

    [Fact]
    public async Task Evaluar_RechazarConObservaciones_EstadoCambiaARechazada()
    {
        var db = CrearDb("dict_rechazar_obs");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1));
        await db.SaveChangesAsync();

        var ctrl = CrearController(db);
        await ctrl.Evaluar(new EvaluarRequest(1, "Rechazar", "Zona inundable"));

        var activo = await db.Activos.FindAsync(1);
        Assert.Equal(EstadoActivoEnum.Rechazada, activo!.Estado);
        Assert.Equal("Zona inundable", activo.Observaciones);
    }

    [Fact]
    public async Task Evaluar_Devolver_EstadoCambiaADevuelta()
    {
        var db = CrearDb("dict_devolver");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1));
        await db.SaveChangesAsync();

        var ctrl = CrearController(db);
        await ctrl.Evaluar(new EvaluarRequest(1, "Devolver", "Faltan documentos de catastro"));

        var activo = await db.Activos.FindAsync(1);
        Assert.Equal(EstadoActivoEnum.Devuelta, activo!.Estado);
        Assert.Equal("Faltan documentos de catastro", activo.Observaciones);
    }

    [Fact]
    public async Task Evaluar_DictamenInvalido_RetornaError()
    {
        var db = CrearDb("dict_invalido");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1));
        await db.SaveChangesAsync();

        var ctrl   = CrearController(db);
        var result = await ctrl.Evaluar(new EvaluarRequest(1, "Suspender", null));
        var json   = Assert.IsType<JsonResult>(result);

        Assert.False(ObtenerSuccess(json));
        var activo = await db.Activos.FindAsync(1);
        Assert.Equal(EstadoActivoEnum.EnRevision, activo!.Estado);
    }

    [Fact]
    public async Task Evaluar_IngenieroNoAsignado_RetornaError()
    {
        var db = CrearDb("dict_otro_ing");
        db.Sujetos.Add(Dueno());
        db.Activos.Add(ActivoEnRevision(1, ingenieroId: 99));   // asignado al ingeniero 99
        await db.SaveChangesAsync();

        var ctrl   = CrearController(db, ingenieroId: 1);       // autenticado como ingeniero 1
        var result = await ctrl.Evaluar(new EvaluarRequest(1, "Aprobar", null));
        var json   = Assert.IsType<JsonResult>(result);

        Assert.False(ObtenerSuccess(json));
    }
}
