# Sistema Nativa — Plan de Testing (P9)

---

## Proyecto de Tests

```
TESTS/Nativa.Tests.csproj
```

**Stack:** xUnit + Moq + Microsoft.EntityFrameworkCore.InMemory

---

## Tests Requeridos por Spec

### CalculadoraServiceTests
| Test | Descripción |
|---|---|
| `Formula_ConInputsConocidos_RetornaMontoEsperado` | Input fijo → resultado predecible |
| `Formula_CuandoSumaPctSuperaTope_UsaTope` | SumaPct > Tope → aplica Tope |
| `Formula_SinBonoNacional_NoSumasPctNacional` | EsNacional=false → PctNacional no suma |
| `Formula_Redondeo_AwayFromZero` | 2 decimales con MidpointRounding.AwayFromZero |

### OtpServiceTests
| Test | Descripción |
|---|---|
| `VerificarOtp_TTLExpirado_RetornaError` | OtpSesion.Expiracion < now → fallo |
| `VerificarOtp_CooldownReenvio_Bloqueado` | UltimoReenvio + 30s > now → 429 |
| `VerificarOtp_TercerIntento_EstadoBloqueado` | Intentos = 2 + nuevo fallo → Bloqueado |
| `ReenviarOtp_MaxReenvios_RetornaError` | ConteoReenvios >= 3 en 5 min → rechaza |

### ConcurrencyTests (RowVersion)
| Test | Descripción |
|---|---|
| `TomarFinca_DosSolicitudesSimultaneas_PrimeroOkSegundo409` | Simula concurrencia con InMemory |
| `TomarFinca_RowVersionInvalido_Retorna400` | Base64 malformado → 400 |
| `TomarFinca_FincaNoEncontrada_Retorna404` | Id inexistente → 404 |

### IbanValidationTests
| Test | Descripción |
|---|---|
| `Iban_FormatoCorrecto_Valido` | `CR21015200009123456789` → válido |
| `Iban_PrefijoCorrecto_LongitudCorta_Invalido` | `CR1234` → inválido |
| `Iban_PrefijoCorrecto_ConLetras_Invalido` | `CRabc12345678901234567` → inválido |
| `Iban_PrefijoCorrecto_LongitudExacta_Valido` | CR + 20 dígitos = 22 chars → válido |
| `Iban_PrefijoDiferente_Invalido` | `US21015200009123456789` → inválido |

### PagoHostedServiceTests
| Test | Descripción |
|---|---|
| `EjecutarPagos_IdempotenciaDobleEjecucion_SinDuplicados` | Correr 2 veces → mismo resultado |
| `EjecutarPagos_Pago12_MarcaVencida` | NumeroPago=12 → Activo.Estado=Vencida |
| `EjecutarPagos_Pago12_ReingresaFifo` | NumeroPago=12 → nuevo Activo Pendiente |
| `EjecutarPagos_Pago12_EnviaN12` | Mock EmailService → verificar llamada N12 |
| `EjecutarPagos_ErrorIndividual_ContinuaCiclo` | Un pago lanza excepción → otros se procesan |

### DictamenTests
| Test | Descripción |
|---|---|
| `Dictamen_Rechazar_SinObservaciones_RetornaError` | observaciones vacías → fallo |
| `Dictamen_Devolver_SinObservaciones_RetornaError` | observaciones vacías → fallo |
| `Dictamen_Devolver_CambiaEstadoDevuelta` | Estado → Devuelta, IdIngeniero → null |
| `Dictamen_Aprobar_SinObservaciones_OK` | Aprobada no requiere obs → OK |

### ParametrosPagoTests
| Test | Descripción |
|---|---|
| `OpcionB_RecalculaPendientes_NoTocaEjecutados` | UPDATE solo Estado=Pendiente |
| `NuevosParametros_SiempreInsert_NoUpdate` | No debe llamar UPDATE en ParametrosPago |

---

## Configuración Base Tests

```csharp
// Usar DbContext InMemory para tests de servicio
var options = new DbContextOptionsBuilder<NativaDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
var db = new NativaDbContext(options);
```

```csharp
// Mock EmailService para tests que envían emails
var mockEmail = new Mock<EmailService>(/* ... */);
mockEmail.Setup(e => e.EnviarGenericoAsync(...)).Returns(Task.CompletedTask);
```

---

## Ejecución

```bash
dotnet test TESTS/Nativa.Tests.csproj
```
