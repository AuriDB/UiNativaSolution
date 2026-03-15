# Mapeo Spec → Código — Sistema Nativa

> Las entidades del DOMAIN y los enums **permanecen en español** (ya construidos con migration ejecutada).
> Services, Controllers, ViewModels y DTOs van en inglés.
> Este archivo es la referencia para saber qué nombre usar en cada capa.

---

## Entidades DOMAIN (clases en español — no cambiar)

| Clase | Namespace | Tabla BD | Descripción |
|---|---|---|---|
| `Sujeto` | `Nativa.Domain.Entities` | `Sujetos` | Usuario del sistema |
| `Activo` | `Nativa.Domain.Entities` | `Activos` | Finca/predio registrado |
| `AdjuntoActivo` | `Nativa.Domain.Entities` | `AdjuntosActivos` | Archivo adjunto de finca |
| `CuentaBancaria` | `Nativa.Domain.Entities` | `CuentasBancarias` | Cuenta IBAN del Dueño |
| `ParametrosPago` | `Nativa.Domain.Entities` | `ParametrosPagos` | Config de cálculo de pagos |
| `PlanPago` | `Nativa.Domain.Entities` | `PlanesPago` | Plan de 12 pagos |
| `PagoMensual` | `Nativa.Domain.Entities` | `PagosMensuales` | Pago individual |
| `OtpSesion` | `Nativa.Domain.Entities` | `OtpSesiones` | Sesión de verificación OTP |

---

## Enums DOMAIN (en español — no cambiar)

| Enum | Valores |
|---|---|
| `RolEnum` | `Dueno=1`, `Ingeniero=2`, `Admin=3` |
| `EstadoSujetoEnum` | `Activo=1`, `Inactivo=2`, `Bloqueado=3` |
| `EstadoActivoEnum` | `Pendiente=1`, `EnRevision=2`, `Aprobada=3`, `Devuelta=4`, `Rechazada=5`, `Vencida=6` |
| `EstadoPagoEnum` | `Pendiente=1`, `Ejecutado=2` |

---

## Propiedades de entidades (español — no cambiar)

### Sujeto
| Propiedad | Tipo | Notas |
|---|---|---|
| `Id` | `int` | PK Identity |
| `Cedula` | `string` | UNIQUE, max 20, formato 1-2345-6789 |
| `Nombre` | `string` | max 200 |
| `Correo` | `string` | UNIQUE, max 200 |
| `PasswordHash` | `string` | BCrypt factor 12. NUNCA exponer |
| `Rol` | `RolEnum` | |
| `Estado` | `EstadoSujetoEnum` | |
| `RowVersion` | `byte[]` | [Timestamp] |
| `FechaCreacion` | `DateTime` | UTC, DEFAULT GETUTCDATE() |

### Activo (Finca)
| Propiedad | Tipo | Notas |
|---|---|---|
| `Id` | `int` | PK Identity |
| `IdDueno` | `int` | FK → Sujeto |
| `IdIngeniero` | `int?` | FK → Sujeto, null hasta FIFO |
| `Hectareas` | `decimal(10,4)` | > 0 |
| `Vegetacion` | `decimal(5,2)` | % cobertura vegetal |
| `Hidrologia` | `decimal(5,2)` | % cobertura hídrica |
| `Topografia` | `decimal(5,2)` | índice topográfico |
| `EsNacional` | `bool` | bono nacional |
| `Lat` | `decimal(9,6)` | latitud |
| `Lng` | `decimal(9,6)` | longitud |
| `Estado` | `EstadoActivoEnum` | |
| `FechaRegistro` | `DateTime` | UTC — define orden FIFO |
| `Observaciones` | `string?` | max 2000, obligatorio en Rechazo y Devolución |
| `RowVersion` | `byte[]` | [Timestamp] — concurrencia optimista |
| `FechaCreacion` | `DateTime` | UTC, DEFAULT GETUTCDATE() |

### CuentaBancaria
| Propiedad | Tipo | Notas |
|---|---|---|
| `Id` | `int` | |
| `IdDueno` | `int` | FK → Sujeto |
| `Banco` | `string` | catálogo SUGEF |
| `TipoCuenta` | `string` | Corriente \| Ahorros |
| `Titular` | `string` | |
| `IbanCompleto` | `string` | cifrado en BD |
| `IbanOfuscado` | `string` | CR******************** para Admin |
| `Activo` | `bool` | máximo uno activo por Dueño |

### ParametrosPago
| Propiedad | Tipo | Notas |
|---|---|---|
| `PrecioBase` | `decimal(10,2)` | ₡ por hectárea/mes |
| `PctVegetacion` | `decimal(5,4)` | |
| `PctHidrologia` | `decimal(5,4)` | |
| `PctNacional` | `decimal(5,4)` | solo si EsNacional=true |
| `PctTopografia` | `decimal(5,4)` | |
| `Tope` | `decimal(5,4)` | límite máximo SumaPct |
| `Vigente` | `bool` | solo UNO true a la vez |
| `CreadoPor` | `int` | FK → Sujeto (Admin) |
| `FechaCreacion` | `DateTime` | |

### PlanPago
| Propiedad | Tipo | Notas |
|---|---|---|
| `IdActivo` | `int` | FK → Activo |
| `IdIngeniero` | `int` | FK → Sujeto |
| `FechaActivacion` | `DateTime` | UTC |
| `SnapshotParametrosJson` | `string` | JSON inmutable de ParametrosPago al activar |
| `MontoMensual` | `decimal(12,2)` | calculado al activar |

### PagoMensual
| Propiedad | Tipo | Notas |
|---|---|---|
| `IdPlan` | `int` | FK → PlanPago |
| `NumeroPago` | `int` | 1 a 12 |
| `Monto` | `decimal(12,2)` | |
| `FechaPago` | `DateTime` | FechaActivacion + (NumeroPago × 30 días) |
| `Estado` | `EstadoPagoEnum` | |
| `FechaEjecucion` | `DateTime?` | null hasta ejecución |

### OtpSesion
| Propiedad | Tipo | Notas |
|---|---|---|
| `IdSujeto` | `int` | FK → Sujeto |
| `HashOtp` | `string` | BCrypt del OTP 6 dígitos |
| `Expiracion` | `DateTime` | UTC = creación + 90s |
| `Usada` | `bool` | |
| `Intentos` | `int` | incrementa por fallo |
| `UltimoReenvio` | `DateTime?` | cooldown 30s |
| `ConteoReenvios` | `int` | máx 3 en 5 minutos |

---

## Services (en inglés)

| Propósito | Clase | Archivo en WEB_UI/Services/ |
|---|---|---|
| Auth / login / registro | `AuthService` | `AuthService.cs` |
| OTP | `OtpService` | `OtpService.cs` |
| Fórmula de pago | `CalculatorService` | `CalculatorService.cs` |
| Azure Blob | `BlobService` | `BlobService.cs` |
| IBAN / cuenta bancaria | `BankAccountService` | `BankAccountService.cs` |
| Email | `EmailService` | `EmailService.cs` |
| OpenWeather + Elevation | `ExternalApiService` | `ExternalApiService.cs` |
| Pago automático diario | `PaymentHostedService` | `PaymentHostedService.cs` |

---

## Controllers y rutas (en inglés, URLs en español por spec)

| Controller | Atributo Route | URL base |
|---|---|---|
| `AuthController` | — (convención) | `/Auth` |
| `OwnerController` | `[Route("Dueno")]` | `/Dueno` |
| `EngineerController` | `[Route("Ingeniero")]` | `/Ingeniero` |
| `AdminController` | — | `/Admin` |
| `DashboardController` | — | `/Dashboard` |

---

## Glosario

| Término UI (español) | Entidad/concepto técnico |
|---|---|
| Finca | `Activo` — predio del Dueño registrado para PSA |
| Cola FIFO | `Activos WHERE Estado=Pendiente ORDER BY FechaRegistro ASC` |
| Dictamen | Resolución del Ingeniero: Aprobar \| Rechazar \| Devolver |
| Snapshot | `SnapshotParametrosJson` — JSON inmutable al activar el plan |
| Tope | `Tope` — límite máximo de SumaPct en fórmula |
| IBAN ofuscado | `IbanOfuscado` = CR******************** |
| EnRevision | `EstadoActivoEnum.EnRevision` — Dueño no puede editar |
| Opción A | Nuevos ParametrosPago afectan solo fincas nuevas |
| Opción B | Nuevos ParametrosPago + recalcular PagosMensuales Pendientes activos |
