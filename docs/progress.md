# Sistema Nativa — Progreso de Implementación

> Última actualización: 2026-03-15
> Rama activa: `Yorzethmc`

---

## Estado por Parte

| Parte | Descripción | Estado | Notas |
|---|---|---|---|
| P1 | DbContext + Entidades + Enums + Migration Init | ✅ Completo | BD PSA_Dev creada, 8 tablas |
| P2 | AuthController real: Register, OTP, Login, Logout, Forgot, Reset | ✅ Completo | Cookie nativa_auth, BCrypt, HMAC-SHA256 |
| P3 | DuenoController: Fincas Index/Nueva/Detalle/Editar/Reenviar | ✅ Completo | Leaflet map, Azure Blob |
| P4 | IngenieroController: Cola FIFO, Evaluar, Dictamen A/R/D | ✅ Completo | RowVersion 409, APIs paralelas |
| P5 | IBAN (CU23) + ActivarPlan (CU24) + CalculadoraService | ✅ Completo | 12 PagoMensual generados |
| P6 | PagoHostedService + DuenoController.Pagos historial | ✅ Completo | BackgroundService + PeriodicTimer, N10/N12 |
| P7 | AdminController: Usuarios CRUD + Parámetros Opción A/B | ✅ Completo | AdminService, CU05-08, CU27-28, N13/N14 |
| P8 | Notificaciones email N03–N14 (completar cobertura) | ✅ Completo | N10/N12 en P6, N13/N14 en P7 |
| P9 | xUnit: fórmula, IBAN, dictamen, parámetros | ✅ Completo | 25/25 tests — TESTS/Nativa.Tests.csproj |

---

## Migraciones EF Core

| Migración | Descripción | Fecha |
|---|---|---|
| `Init` | Creación inicial de 8 tablas + índices | 2026-03-14 |
| `AddPasswordReset` | Campos PasswordResetHash/Expira en Sujetos | 2026-03-14 |

Comando para aplicar: `dotnet ef database update --project WEB_UI`

---

## Archivos Clave

| Archivo | Rol |
|---|---|
| `WEB_UI/Data/NativaDbContext.cs` | DbContext principal, índices, FK |
| `WEB_UI/Program.cs` | DI, pipeline, cookie auth |
| `WEB_UI/Services/AuthService.cs` | Login, registro, OTP, reset password |
| `WEB_UI/Services/ActivoService.cs` | CU11-16, CU23 |
| `WEB_UI/Services/IngenieroService.cs` | CU17-24 |
| `WEB_UI/Services/CalculadoraService.cs` | Fórmula PSA |
| `WEB_UI/Services/EncryptionService.cs` | AES-256 para IBAN |
| `WEB_UI/Services/BlobService.cs` | Azure Blob, fallback dev |
| `WEB_UI/Services/ExternalApiService.cs` | OpenWeather + OpenElevation en paralelo |
| `WEB_UI/Services/EmailService.cs` | MailKit SMTP (Mailtrap dev) |

---

## Build

```
dotnet build WEB_UI/WEB_UI.csproj
```
Último build: ✅ 0 errores, 4 warnings (versiones NuGet, sin impacto)
