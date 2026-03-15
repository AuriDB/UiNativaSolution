<!--
  CLAUDE.md — Fuente de verdad absoluta para Sistema Nativa
  Lee este archivo COMPLETO antes de escribir cualquier código.
  Si algo no está aquí, aplicá las convenciones del stack definido.
  Si el usuario dice "Parte N", implementá exactamente lo de esa sección.
  No cambies nombres, no inventes alternativas, no hagas preguntas sobre
  cosas que ya están documentadas aquí.
-->

# Sistema Nativa — Instrucciones para Claude

## PROYECTO

| Campo | Valor |
|---|---|
| nombre_sistema | Sistema Nativa |
| descripcion | Plataforma web de Pago por Servicios Ambientales (PSA) para Costa Rica |
| dev_principal | Erick Masis |
| origen | Proyecto universitario Universidad Cenfotec con criterios de producción |
| repo_local | `C:\Users\yorze\OneDrive\Documentos\GitHub\UiNativaSolution` |
| idioma_ui | Español (es-CR) |

---

## STACK — NO NEGOCIABLE

| Tecnología | Valor |
|---|---|
| runtime | .NET 10.0 |
| framework | ASP.NET Core MVC ← MVC, **no** Web API separada |
| ide | Visual Studio 2026 |
| db | SQL Server local, BD: PSA_Dev |
| orm | Entity Framework Core 10 (Code First, Migrations) |
| frontend | Razor Views + Bootstrap 5 (Bootswatch Minty, CDN) |
| js | jQuery AJAX + SweetAlert2 + Ag-Grid Community (CDN) |
| mapas | Leaflet.js (pin lat/lng en registro de finca) |
| blob | Azure Blob Storage, contenedor: `psa-docs` |
| email_dev | Mailtrap SMTP (`appsettings.Development.json`) |
| auth | ASP.NET Core Cookie Authentication, HttpOnly, SameSite=Strict |
| hashing | BCrypt.Net-Next factor 12 |
| pdf | QuestPDF (comprobantes de pago) |
| testing | xUnit + Moq |

---

## ESTRUCTURA DEL REPO — ÚNICA CORRECTA

```
UiNativaSolution/
├── CLAUDE.md                        ← este archivo
├── docs/
│   └── progress.md
├── WEB_UI/
│   ├── Controllers/
│   ├── Data/
│   │   └── DataSeeder.cs
│   ├── Models/                      ← ViewModels y DTOs
│   ├── Services/
│   ├── Views/
│   └── wwwroot/
└── TESTS/
    └── Nativa.Tests.csproj
```

> **CRÍTICO**: Proyecto **único** WEB_UI. Sin proyectos separados DOMAIN, INFRASTRUCTURE, ni Application.
> Las entidades van en `WEB_UI/Models/Entities/`, los enums en `WEB_UI/Models/Enums/`.

---

## LO QUE YA ESTÁ CONSTRUIDO (no tocar sin razón)

### Páginas públicas
- `Landing/Project` — COMPLETA (Hero, ¿Qué es PSA?, proceso 4 pasos, 7 tarjetas beneficios, estadísticas animadas, CTA)
- `Landing/Team` — COMPLETA (plantilla del equipo)

### Autenticación (UI lista, sin integración backend real)
- Login — formulario funcional simulado
- Registro 3 pasos — COMPLETO (datos personales → contraseña con medidor → revisión)
- Verificar OTP — UI lista, sin lógica de backend
- Olvidé contraseña — UI lista
- Restablecer contraseña — UI lista

### Dashboard autenticado
- Bienvenida con nombre y fecha
- 4 KPI cards (fincas, activas, próximo pago, en revisión) — placeholder data
- Tabla de fincas recientes — placeholder data
- Acciones rápidas sidebar
- Menú condicional por rol (Dueño / Ingeniero / Admin)

### Componentes UI compartidos
- Evaluador de fortaleza de contraseña en tiempo real
- Toggle ver/ocultar contraseña
- Indicadores de pasos con dots
- Animaciones (badges flotantes, contadores, hover cards)
- SweetAlert2 + Bootstrap alerts
- Loading spinners en botones
- Validación de Cédula formato 1-2345-6789

---

## LO QUE FALTA CONSTRUIR

- API backend (controladores MVC con lógica real + EF Core)
- Capa de datos: DbContext, entidades, migrations
- Integración de los formularios existentes con backend real
- Registro de fincas + upload Azure Blob
- Cola FIFO + evaluación técnica Ingeniero
- Dictamen (aprobar / rechazar / devolver)
- Módulo de pagos (activar plan, PagoHostedService)
- Gestión admin (usuarios, parámetros de pago)
- Notificaciones por correo (N03–N14)

---

## ENTIDADES — DOMINIO

### Sujeto
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK identity |
| Cedula | string | UNIQUE, max 20, requerido |
| Nombre | string | max 200, requerido |
| Correo | string | UNIQUE, max 200, requerido |
| PasswordHash | string | BCrypt. NUNCA retornar en responses |
| Rol | RolEnum | Dueno \| Ingeniero \| Admin |
| Estado | EstadoSujetoEnum | |
| RowVersion | byte[] | [Timestamp] — concurrencia optimista |
| FechaCreacion | DateTime | UTC, auto en INSERT |

### Activo (la "Finca")
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | PK identity |
| IdDueno | int | FK → Sujeto |
| IdIngeniero | int? | FK → Sujeto, null hasta asignación FIFO |
| Hectareas | decimal(10,4) | requerido, > 0 |
| Vegetacion | decimal(5,2) | % cobertura vegetal |
| Hidrologia | decimal(5,2) | % cobertura hídrica |
| Topografia | decimal(5,2) | índice topográfico |
| EsNacional | bool | aplica bono nacional en fórmula |
| Lat | decimal(9,6) | latitud |
| Lng | decimal(9,6) | longitud |
| Estado | EstadoActivoEnum | |
| FechaRegistro | DateTime | UTC — define orden FIFO (ORDER BY ASC) |
| Observaciones | string? | max 2000. OBLIGATORIO en Rechazo y Devolución |
| RowVersion | byte[] | [Timestamp] — CU17 concurrencia optimista |

### AdjuntoActivo
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| IdActivo | int | FK → Activo |
| BlobUrl | string | URL completa Azure Blob |
| NombreArchivo | string | |
| FechaSubida | DateTime | UTC |

### CuentaBancaria
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| IdDueno | int | FK → Sujeto |
| Banco | string | catálogo SUGEF |
| TipoCuenta | string | Corriente \| Ahorros |
| Titular | string | |
| IbanCompleto | string | CR + 20 dígitos, **cifrado en BD** |
| IbanOfuscado | string | `CR********************` para Admin |
| Activo | bool | máximo uno activo por Dueño |

### ParametrosPago — NUNCA modificar filas existentes. Siempre INSERT nueva versión.
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| PrecioBase | decimal(10,2) | ₡ por hectárea/mes |
| PctVegetacion | decimal(5,4) | |
| PctHidrologia | decimal(5,4) | |
| PctNacional | decimal(5,4) | solo si EsNacional=true |
| PctTopografia | decimal(5,4) | |
| Tope | decimal(5,4) | máximo de SumaPct aplicable |
| Vigente | bool | solo UNO true a la vez |
| FechaCreacion | DateTime | UTC |
| CreadoPor | int | FK → Sujeto (Admin) |

### PlanPago
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| IdActivo | int | FK → Activo |
| IdIngeniero | int | FK → Sujeto |
| FechaActivacion | DateTime | UTC |
| SnapshotParametrosJson | string | JSON de ParametrosPago al activar — INMUTABLE |
| MontoMensual | decimal(12,2) | calculado y persistido al activar |

### PagoMensual
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| IdPlan | int | FK → PlanPago |
| NumeroPago | int | 1 a 12 |
| Monto | decimal(12,2) | |
| FechaPago | DateTime | FechaActivacion + (NumeroPago × 30 días) |
| Estado | EstadoPagoEnum | |
| FechaEjecucion | DateTime? | null hasta ejecución |

### OtpSesion
| Campo | Tipo | Reglas |
|---|---|---|
| Id | int | |
| IdSujeto | int | FK → Sujeto |
| HashOtp | string | BCrypt del OTP 6 dígitos |
| Expiracion | DateTime | UTC = creación + 90s |
| Usada | bool | |
| Intentos | int | incrementa por fallo |
| UltimoReenvio | DateTime? | cooldown 30s |
| ConteoReenvios | int | máx 3 en 5 minutos |

---

## ENUMS

```csharp
public enum RolEnum          { Dueno = 1, Ingeniero = 2, Admin = 3 }
public enum EstadoSujetoEnum { Activo = 1, Inactivo = 2, Bloqueado = 3 }
public enum EstadoActivoEnum { Pendiente = 1, EnRevision = 2, Aprobada = 3, Devuelta = 4, Rechazada = 5, Vencida = 6 }
public enum EstadoPagoEnum   { Pendiente = 1, Ejecutado = 2 }
```

---

## FÓRMULA — CalculadoraService

```
SumaPct = PctVegetacion + PctHidrologia + (EsNacional ? PctNacional : 0) + PctTopografia
Pago    = Hectareas × PrecioBase × (1 + Math.Min(SumaPct, Tope))
```

- **Parámetros**: leer de `ParametrosPago WHERE Vigente = true`
- **Redondeo**: 2 decimales, `MidpointRounding.AwayFromZero`
- **Snapshot**: serializar `ParametrosPago` vigente al activar (no cambia después)

---

## REGLAS DE NEGOCIO

### Contraseña
- Mín 6 chars, 1 mayúscula, 1 número, 1 carácter especial
- Validar en **JS frontend Y en backend C#** — doble barrera
- El medidor de fortaleza visual ya está implementado en UI

### OTP
- 6 dígitos numéricos aleatorios
- Hash BCrypt antes de guardar en OtpSesion
- TTL: 90 segundos
- Cooldown reenvío: 30 segundos entre reenvíos
- Límite: máx 3 reenvíos en 5 minutos
- 3 fallos consecutivos → estado=Bloqueado (solo Admin reactiva)
- La pantalla de OTP ya existe en UI, falta la lógica

### Recuperación de contraseña
- Respuesta siempre genérica (no revelar si correo existe — OWASP)
- Token: HMAC-SHA256, TTL 15 min, un solo uso
- Si expirado o ya usado: HTTP 410 Gone

### FIFO y concurrencia optimista
- Cola: `Activos WHERE Estado=Pendiente ORDER BY FechaRegistro ASC`
- Tomar finca: `UPDATE activos SET Estado=EnRevision, IdIngeniero=@id WHERE Id=@id AND RowVersion=@rv`
- 0 filas afectadas → HTTP 409 Conflict
- Mientras EnRevision: Dueño **NO** puede editar
- N05 al Dueño cuando pasa a EnRevision

### Adjuntos
- Azure Blob contenedor: `psa-docs`
- Extensiones: jpg, jpeg, png, pdf, dwg | tamaño máx: 10 MB
- **NUNCA borrado físico**
- SAS token generado on-demand para el Ingeniero

### Dictamen
| Tipo | Estado resultante | Obs obligatorias | Notif | Siguiente |
|---|---|---|---|---|
| Aprobar | Aprobada | no | N07 + IBAN | esperar IBAN → CU24 |
| Rechazar | Rechazada (FINAL) | SÍ | N08 | ninguna, irreversible |
| Devolver | Devuelta | SÍ | N09 | Dueño corrige → FIFO |

### APIs externas en CU19 Evaluar
- OpenWeather + Open Elevation en **paralelo** (`Task.WhenAll`)
- Si falla: loguear + mostrar "No disponible", NO bloquear flujo

### PagoHostedService
- `IHostedService` + `PeriodicTimer`, corre **1 vez/día**
- `WHERE Estado=Pendiente AND FechaPago <= GETDATE()`
- **NUNCA reprocesar Ejecutados**
- Transacción BD por pago: UPDATE + log atómicos
- Pago #12 → Vencida + copia en FIFO + N12
- Errores individuales logueados sin detener ciclo

### Parámetros — inmutabilidad
- **NUNCA** modificar `ParametrosPago` existente. Siempre INSERT nueva fila.
- **Opción A**: nueva fila Vigente=true. Planes activos intactos.
- **Opción B**: nueva fila + recalcular PagoMensual Pendientes. Ejecutados intocables. N14.

### IBAN
- Regex: `^CR\d{20}$`
- Validar en **JS y C#** (doble barrera)
- Cifrado en BD, ofuscado para Admin: `CR********************`
- Actualizable con plan activo
- Botón "Activar plan" deshabilitado si no hay IBAN

---

## CASOS DE USO

| CU | Nombre | Actor | Endpoint |
|---|---|---|---|
| CU01 | Registrar Dueño + OTP | Dueño | `POST /Auth/Register` + `POST /Auth/VerifyOtp` |
| CU02 | Iniciar Sesión | Todos | `POST /Auth/Login` |
| CU03 | Cerrar Sesión | Todos | `POST /Auth/Logout` |
| CU04 | Recuperar Contraseña | Todos | `POST /Auth/Forgot` + `POST /Auth/Reset` |
| CU05 | Crear Ingeniero | Admin | `POST /Admin/Usuarios/Crear` |
| CU06 | Editar Usuario | Admin | `POST /Admin/Usuarios/Editar/{id}` |
| CU07 | Inactivar Usuario | Admin | `POST /Admin/Usuarios/Inactivar/{id}` |
| CU08 | Ver Usuarios | Admin | `GET /Admin/Usuarios` |
| CU09 | Ver Perfil | Dueño | `GET /Dueno/Perfil` |
| CU10 | Editar Perfil | Dueño | `POST /Dueno/Perfil` |
| CU11 | Registrar Finca | Dueño | `POST /Dueno/Fincas/Nueva` (multipart) |
| CU12 | Ver mis Fincas | Dueño | `GET /Dueno/Fincas` |
| CU13 | Ver Detalle Finca | Dueño | `GET /Dueno/Fincas/{id}` |
| CU14 | Actualizar Finca | Dueño | `POST /Dueno/Fincas/Editar/{id}` (solo Devuelta) |
| CU15 | Reenviar a Evaluación | Dueño | `POST /Dueno/Fincas/Reenviar/{id}` |
| CU16 | Ver Adjuntos | Dueño/Ing | `GET /Fincas/{id}/Adjuntos` |
| CU17 | Ver Cola FIFO | Ingeniero | `GET /Ingeniero/Cola` |
| CU18 | Tomar Finca | Ingeniero | `POST /Ingeniero/Cola/Tomar/{id}` |
| CU19 | Evaluar Finca | Ingeniero | `GET /Ingeniero/Fincas/Evaluar/{id}` |
| CU20 | Aprobar Dictamen | Ingeniero | `POST /Ingeniero/Fincas/Dictamen/{id}` `{tipo:"Aprobar"}` |
| CU21 | Rechazar Dictamen | Ingeniero | `POST /Ingeniero/Fincas/Dictamen/{id}` `{tipo:"Rechazar"}` |
| CU22 | Devolver Dictamen | Ingeniero | `POST /Ingeniero/Fincas/Dictamen/{id}` `{tipo:"Devolver"}` |
| CU23 | Registrar/Actualizar IBAN | Dueño | `POST /Dueno/CuentaBancaria` |
| CU24 | Activar Plan de Pagos | Ingeniero | `POST /Ingeniero/Fincas/ActivarPlan/{id}` |
| CU25 | Pago Automático | Sistema | BackgroundService (PeriodicTimer diario) |
| CU26 | Ver Historial Pagos | Dueño | `GET /Dueno/Pagos` |
| CU27 | Reconfigurar Parámetros | Admin | `POST /Admin/Parametros` |
| CU28 | Ver Parámetros | Admin | `GET /Admin/Parametros` |

---

## NOTIFICACIONES

| Código | Evento | Destinatario | Extra |
|---|---|---|---|
| N03 | Bloqueo OTP/intentos | Dueño | — |
| N05 | Finca pasa a EnRevision | Dueño | — |
| N06 | Ingeniero corrige datos evaluación | Dueño | — |
| N07 | Dictamen Aprobada | Dueño | instrucciones IBAN |
| N08 | Dictamen Rechazada | Dueño | observaciones |
| N09 | Dictamen Devuelta | Dueño | observaciones |
| N10 | Pago mensual ejecutado | Dueño | PDF (QuestPDF) |
| N12 | Contrato vencido pago #12 | Dueño | — |
| N13 | Cuenta inactivada por Admin | Usuario | — |
| N14 | Recálculo pagos Opción B | Dueño | nuevo desglose |

---

## BASE DE DATOS

### Convenciones
- Esquema: `dbo`
- Nombres: PascalCase (Activos, PagosMensuales, OtpSesiones)
- Todas las tablas: `Id` (int PK identity) + `FechaCreacion` (datetime2 DEFAULT GETUTCDATE())
- RowVersion: `[Timestamp] byte[]`, `.IsRowVersion()` en EF Core
- Soft delete: campo `Estado` o `bool Activo`. **NUNCA DELETE físico**
- Índices: `Sujeto(Cedula)`, `Sujeto(Correo)`, `Activo(Estado, FechaRegistro)`

### Connection String

```
Server=localhost;Database=PSA_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```

---

## CONVENCIONES DE CÓDIGO

### Patrón arquitectónico
MVC clásico. Los Controllers llaman directamente a Services inyectados.
**No hay CQRS. No hay MediatR. Sin capas de Application/Domain separadas.**
Estructura simple: `Controller → Service → DbContext`

### Naming
- Controllers → `[Entidad]Controller` ej: `DuenoController`, `IngenieroController`
- Services → `[Entidad]Service` ej: `ActivoService`, `OtpService`
- ViewModels → `[Pantalla]ViewModel` ej: `RegistroFincaViewModel`
- DTOs → `[Entidad][Create|Response]Dto`

### HTTP (acciones de controllers MVC)
- `200/View()` → render de página normal
- `RedirectToAction` → después de POST exitoso (PRG pattern)
- `BadRequest` → validación falla
- `Forbid()` → rol incorrecto
- `NotFound()` → recurso no existe o no pertenece al usuario
- `Json(new { success, message })` → respuestas AJAX

### TempData para mensajes al usuario
```csharp
TempData["Success"] = "Finca registrada correctamente.";
TempData["Error"]   = "No se pudo procesar la solicitud.";
```
(la _Layout ya debe renderizarlos)

### Cookie y Claims
- cookie name: `nativa_auth`
- Claims: `ClaimTypes.NameIdentifier` (userId), `ClaimTypes.Role`, `ClaimTypes.Email`
- Expiración: 8 horas, sliding: false
- Rutas públicas: `/Landing/`, `/Auth/`, `/Home/`, `/Error/`

---

## INTEGRACIONES EXTERNAS

### OpenWeather
- URL: `https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lng}&appid={key}`
- Uso: temperatura y presión en evaluación de finca
- Config: `ExternalApis:OpenWeatherApiKey`

### Open Elevation
- URL: `https://api.open-elevation.com/api/v1/lookup?locations={lat},{lng}`
- Uso: m.s.n.m. del predio
- Config: sin auth

### Azure Blob
- Uso: adjuntos fincas
- Config: `AzureBlob:ConnectionString`, `AzureBlob:ContainerName = "psa-docs"`

### Mailtrap SMTP (dev)
- Host: `smtp.mailtrap.io`, Puerto: 587
- Config: `Email:Host/Port/User/Pass` en `appsettings.Development.json`

---

## VIEWS — PÁGINAS RAZOR

### Ya existe (no recrear)
- `Views/Landing/Project.cshtml`
- `Views/Landing/Team.cshtml`
- `Views/Auth/Login.cshtml`
- `Views/Auth/Registro.cshtml` ← 3 pasos ya implementados
- `Views/Auth/VerificarOtp.cshtml`
- `Views/Auth/OlvidoContrasena.cshtml`
- `Views/Auth/RestablecerContrasena.cshtml`
- `Views/Dashboard/Index.cshtml` ← KPIs placeholder, menú por rol

### Falta construir
- `Views/Dueno/Fincas/Index.cshtml` — lista mis fincas (Ag-Grid)
- `Views/Dueno/Fincas/Nueva.cshtml` — form + Leaflet map + file upload
- `Views/Dueno/Fincas/Detalle.cshtml` — estado + historial
- `Views/Dueno/Cuenta.cshtml` — IBAN
- `Views/Dueno/Pagos.cshtml` — historial pagos (Ag-Grid)
- `Views/Ingeniero/Cola.cshtml` — Ag-Grid FIFO + filtros + Tomar
- `Views/Ingeniero/Fincas/Evaluar.cshtml` — panel APIs + dictamen
- `Views/Admin/Usuarios.cshtml` — Ag-Grid CRUD
- `Views/Admin/Parametros.cshtml` — form Opción A/B

---

## PLAN DE IMPLEMENTACIÓN

| Parte | Qué implementar | Verificación |
|---|---|---|
| P1 | DbContext + entidades EF Core + enums + migration Init | `dotnet ef database update OK` |
| P2 | AuthController real: Register, VerifyOtp, Login, Logout, Forgot, Reset — conectar a UI existente | test login en browser |
| P3 | DuenoController: Fincas Index, Nueva (Leaflet+Blob), Detalle, Editar, Reenviar | finca creada con adjuntos |
| P4 | IngenieroController: Cola FIFO (Ag-Grid + RowVersion 409), Evaluar (APIs paralelas), Dictamen A/R/D | 409 visible en UI |
| P5 | DuenoController: IBAN (CU23) + IngenieroController: ActivarPlan (CalculadoraService) | 12 pagos generados |
| P6 | PagoHostedService (idempotente, pago #12 → FIFO) + DuenoController: Pagos historial (Ag-Grid) | test pago #12 |
| P7 | AdminController: Usuarios CRUD + inactivar cascada + Parámetros Opción A/B | Admin CRUD completo |
| P8 | Notificaciones email N03–N14 (EmailService + Mailtrap) | correo en Mailtrap inbox |
| P9 | xUnit: fórmula, OTP, RowVersion, IBAN, pago #12 | CI verde |

---

## TESTS REQUERIDOS

### CalculadoraServiceTests
- Fórmula con inputs conocidos
- SumaPct > Tope → usar Tope

### OtpServiceTests
- TTL expirado → error
- Cooldown 30s
- Bloqueo al 3er intento

### ConcurrencyTests
- Dos Tomar simultáneos → primero OK, segundo HTTP 409

### IbanValidationTests
- CR + 20 dígitos → válido
- Otro prefijo o longitud → inválido

### PagoHostedServiceTests
- Ejecutar 2 veces → sin duplicar
- Pago #12 → Vencida + FIFO + N12

### DictamenTests
- Rechazar sin obs → ValidationException
- Devolver → re-inserta en FIFO

### ParametrosPagoTests
- Opción B → no toca Ejecutados

---

## APPSETTINGS.JSON ESQUELETO

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PSA_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Auth": {
    "CookieName": "nativa_auth",
    "ExpireHours": 8,
    "HmacSecret": "REEMPLAZAR_256BIT"
  },
  "Encryption": {
    "Key": "REEMPLAZAR_32_BYTES_BASE64"
  },
  "AzureBlob": {
    "ConnectionString": "AZURE_CONN_STRING",
    "ContainerName": "psa-docs"
  },
  "ExternalApis": {
    "OpenWeatherApiKey": "TU_KEY",
    "OpenWeatherBase": "https://api.openweathermap.org/data/2.5",
    "OpenElevationBase": "https://api.open-elevation.com/api/v1"
  },
  "Email": {
    "From": "nativa@noreply.cr",
    "DisplayName": "Sistema Nativa"
  }
}
```

```json
// appsettings.Development.json
{
  "Email": {
    "Host": "smtp.mailtrap.io",
    "Port": 587,
    "User": "TU_USER",
    "Pass": "TU_PASS"
  }
}
```

---

## GLOSARIO

| Término | Significado |
|---|---|
| Finca / Activo | Predio del Dueño registrado para recibir pagos |
| FIFO | Cola de fincas Pendientes, ORDER BY FechaRegistro ASC |
| Dictamen | Resolución Ingeniero: Aprobar \| Rechazar \| Devolver |
| Snapshot JSON | Copia inmutable de ParametrosPago al activar plan |
| RowVersion | [Timestamp] byte[], EF Core, concurrencia optimista |
| N0X | Notificación por correo (N03, N05, N07-N10, N12-N14) |
| Tope | Límite máximo SumaPct en fórmula |
| IBAN ofuscado | `CR********************` para vistas Admin |
| Opción A | Parámetros nuevos afectan solo fincas nuevas |
| Opción B | Parámetros nuevos + recalcular Pendientes activos |
| EnRevision | Finca tomada por Ingeniero, Dueño no puede editar |
| nativa_auth | Nombre de la cookie de sesión |
