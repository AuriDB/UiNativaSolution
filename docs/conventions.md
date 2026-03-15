# Convenciones de código — Sistema Nativa

## 1. Naming general

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases | PascalCase, inglés | `Subject`, `AssetService`, `AuthController` |
| Interfaces | PascalCase + prefijo `I`, inglés | `IAssetRepository`, `IEmailService` |
| Métodos | PascalCase, inglés | `GetByIdAsync`, `CalculatePayment` |
| Propiedades públicas | PascalCase, inglés | `PasswordHash`, `CreatedAt` |
| Variables locales | camelCase, inglés | `assetId`, `monthlyAmount` |
| Parámetros | camelCase, inglés | `int subjectId`, `string ibanNumber` |
| Constantes | PascalCase o UPPER_SNAKE_CASE | `MaxOtpAttempts`, `MAX_IBAN_LENGTH` |
| Enums | PascalCase, inglés | `Role`, `AssetStatus`, `PaymentStatus` |
| Enum valores | PascalCase, inglés | `Owner`, `Engineer`, `UnderReview` |

### Excepción: español por funcionalidad
Cuando un nombre afecta URLs o contratos externos definidos en el spec, se puede usar español:
- Rutas de controllers: `DuenoController` si es necesario para la URL `/Dueno/...`
  (alternativa preferida: `OwnerController` con `[Route("Dueno")]`)
- TempData keys definidas en el spec: `TempData["Success"]`, `TempData["Error"]`
- Cookie name del spec: `nativa_auth`
- Claim names estándar: `ClaimTypes.NameIdentifier`, `ClaimTypes.Role`

---

## 2. Comentarios

### Regla principal
**Todo comentario va en español (es-CR).**
Los comentarios explican el *por qué* del código, no el *qué*.

### Obligatorio comentar
- Toda validación de negocio
- Reglas de seguridad (OTP, IBAN, contraseña)
- Consultas con lógica FIFO o RowVersion
- Fórmulas de cálculo
- Cualquier código que no sea evidente

```csharp
// Verificar que no hayan pasado más de 90 segundos desde que se generó el OTP
if (session.ExpiresAt < DateTime.UtcNow)
    throw new InvalidOperationException("El OTP ha expirado.");

// Validar IBAN: debe comenzar con CR seguido de exactamente 20 dígitos
if (!Regex.IsMatch(iban, @"^CR\d{20}$"))
    throw new ArgumentException("El IBAN debe tener el formato CR + 20 dígitos.");
```

### No comentar
- Código auto-explicativo (`var list = new List<Asset>();`)
- Re-decir lo que hace el método (`// Obtiene por id`)

---

## 3. Estructura de Controllers

```csharp
// Patrón PRG (Post-Redirect-Get) para todas las acciones POST
[HttpPost]
public async Task<IActionResult> Create(CreateAssetViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    // Llamar al service correspondiente
    await _assetService.CreateAsync(model, userId);

    TempData["Success"] = "Finca registrada correctamente.";
    return RedirectToAction(nameof(Index));
}
```

### Respuestas HTTP esperadas
| Situación | Respuesta |
|---|---|
| Página normal | `View()` |
| POST exitoso | `RedirectToAction(...)` |
| Validación falla | `View(model)` con ModelState |
| Recurso no encontrado | `NotFound()` |
| Rol incorrecto | `Forbid()` |
| Concurrencia (RowVersion) | HTTP 409 vía TempData["Error"] |
| Token expirado/usado | HTTP 410 vía redirect |

---

## 4. Estructura de Services

```csharp
public class AssetService
{
    private readonly NativaDbContext _db;
    private readonly ILogger<AssetService> _logger;

    public AssetService(NativaDbContext db, ILogger<AssetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Asset> GetByIdAsync(int id, int ownerId)
    {
        // Verificar que la finca pertenece al dueño autenticado
        var asset = await _db.Assets
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerId == ownerId);

        if (asset is null)
            throw new InvalidOperationException("Finca no encontrada.");

        return asset;
    }
}
```

---

## 5. ViewModels y DTOs

```csharp
// ViewModel: datos para la vista (puede tener DataAnnotations)
public class CreateAssetViewModel
{
    [Required(ErrorMessage = "Las hectáreas son requeridas.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Debe ser mayor a 0.")]
    public decimal Hectares { get; set; }
    // ...
}

// DTO: transferencia de datos entre capas
public class AssetResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    // ...
}
```

---

## 6. Validaciones — doble barrera

Todas las validaciones críticas se implementan **dos veces**:

| Validación | JS (frontend) | C# (backend) |
|---|---|---|
| Contraseña (mín 6, 1 mayúscula, 1 número, 1 especial) | ✅ medidor ya implementado | ✅ en AuthController |
| IBAN (`^CR\d{20}$`) | ✅ Regex en input event | ✅ en BankAccountService |
| Cédula (formato 1-2345-6789) | ✅ ya implementado | ✅ en AuthController |
| Tamaño de adjuntos (máx 10 MB) | ✅ en file input | ✅ en AssetService |

---

## 7. Async/Await

- Todos los métodos que acceden a BD o servicios externos son `async`.
- Sufijo `Async` en todos los métodos asíncronos.
- Usar `ConfigureAwait(false)` solo en librerías, no en ASP.NET Core.

```csharp
// Correcto
public async Task<List<Asset>> GetAllByOwnerAsync(int ownerId) { ... }

// Incorrecto
public List<Asset> GetAllByOwner(int ownerId) { ... }
```

---

## 8. Nullable

- El proyecto tiene `<Nullable>enable</Nullable>` activado.
- Propiedades nullable se marcan con `?`.
- Strings no nulos se inicializan con `= string.Empty`.

```csharp
public string Name { get; set; } = string.Empty;      // no nullable
public string? Observations { get; set; }              // nullable
public int? EngineerId { get; set; }                   // nullable FK
```

---

## 9. Seguridad

- **Nunca** retornar `PasswordHash` en responses o vistas.
- **Nunca** delete físico — usar campo `Status` o `bool Active`.
- **Nunca** modificar `PaymentParameters` existente — siempre INSERT nueva fila.
- Respuestas genéricas en recuperación de contraseña (OWASP — no revelar si el correo existe).
- SAS tokens para adjuntos Azure Blob: generados on-demand, no almacenar.

---

## 10. Convenciones EF Core

```csharp
// FechaCreacion en todas las tablas
entity.Property(e => e.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");

// RowVersion para concurrencia optimista
entity.Property(e => e.RowVersion)
    .IsRowVersion();

// Decimales precisos
entity.Property(e => e.Hectares)
    .HasColumnType("decimal(10,4)");

// Soft delete: nunca DeleteBehavior.Cascade en entidades principales
entity.HasOne(e => e.Owner)
    .WithMany(s => s.Assets)
    .HasForeignKey(e => e.OwnerId)
    .OnDelete(DeleteBehavior.Restrict);
```
