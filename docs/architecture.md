# Arquitectura — Sistema Nativa

## Patrón arquitectónico
**MVC clásico en un solo proyecto.** Sin capas separadas, sin CQRS, sin MediatR.

```
Request HTTP
    │
    ▼
Controller          ← valida modelo, llama al service, retorna View/Redirect
    │
    ▼
Service             ← lógica de negocio, reglas, cálculos
    │
    ▼
NativaDbContext     ← EF Core, acceso directo a BD
    │
    ▼
SQL Server (PSA_Dev)
```

Los Controllers **no acceden** directamente al DbContext. Siempre van por un Service.

---

## Estructura de carpetas objetivo

```
WEB_UI/
├── Controllers/
│   ├── AuthController.cs
│   ├── DashboardController.cs
│   ├── OwnerController.cs          ← [Route("Dueno")]
│   ├── EngineerController.cs       ← [Route("Ingeniero")]
│   ├── AdminController.cs
│   ├── LandingController.cs        ← ya existe
│   └── HomeController.cs           ← ya existe (redirect/error)
│
├── Data/
│   ├── Entities/
│   │   ├── Subject.cs
│   │   ├── Asset.cs
│   │   ├── AssetAttachment.cs
│   │   ├── BankAccount.cs
│   │   ├── PaymentParameters.cs
│   │   ├── PaymentPlan.cs
│   │   ├── MonthlyPayment.cs
│   │   └── OtpSession.cs
│   ├── Enums/
│   │   ├── Role.cs
│   │   ├── SubjectStatus.cs
│   │   ├── AssetStatus.cs
│   │   └── PaymentStatus.cs
│   └── NativaDbContext.cs
│
├── Services/
│   ├── AuthService.cs
│   ├── OtpService.cs
│   ├── AssetService.cs
│   ├── CalculatorService.cs
│   ├── BlobService.cs
│   ├── BankAccountService.cs
│   ├── EmailService.cs
│   ├── ExternalApiService.cs       ← OpenWeather + Open Elevation
│   └── PaymentHostedService.cs     ← IHostedService
│
├── Models/                         ← ViewModels (ya existen algunos)
│   ├── Auth/
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   ├── OtpViewModel.cs
│   │   ├── ForgotPasswordViewModel.cs
│   │   └── ResetPasswordViewModel.cs
│   ├── Owner/
│   │   ├── CreateAssetViewModel.cs
│   │   ├── EditAssetViewModel.cs
│   │   └── BankAccountViewModel.cs
│   ├── Engineer/
│   │   ├── AssetQueueViewModel.cs
│   │   └── EvaluateAssetViewModel.cs
│   └── Admin/
│       ├── UserViewModel.cs
│       └── PaymentParametersViewModel.cs
│
├── Views/
│   ├── Auth/
│   │   ├── Login.cshtml
│   │   ├── Registro.cshtml
│   │   ├── VerificarOtp.cshtml
│   │   ├── OlvidoContrasena.cshtml
│   │   └── RestablecerContrasena.cshtml
│   ├── Dashboard/
│   │   └── Index.cshtml
│   ├── Dueno/                      ← nombre carpeta en español (URL /Dueno)
│   │   ├── Fincas/
│   │   │   ├── Index.cshtml
│   │   │   ├── Nueva.cshtml
│   │   │   └── Detalle.cshtml
│   │   ├── Cuenta.cshtml
│   │   ├── Pagos.cshtml
│   │   └── Perfil.cshtml
│   ├── Ingeniero/                  ← nombre carpeta en español (URL /Ingeniero)
│   │   ├── Cola.cshtml
│   │   └── Fincas/
│   │       └── Evaluar.cshtml
│   ├── Admin/
│   │   ├── Usuarios.cshtml
│   │   └── Parametros.cshtml
│   ├── Landing/                    ← ya existe
│   │   ├── Project.cshtml
│   │   └── Team.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _Layout_Auth.cshtml
│       └── _Layout_Public.cshtml
│
├── Migrations/                     ← generadas por EF Core CLI
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   │   └── Pages/
│   │       ├── Auth/
│   │       ├── Owner/
│   │       ├── Engineer/
│   │       └── Admin/
│   └── lib/
│
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

---

## Flujo de autenticación

```
POST /Auth/Login
    │
    ├─ Validar ModelState
    ├─ AuthService.LoginAsync(email, password)
    │       ├─ Buscar Subject por Email
    │       ├─ BCrypt.Verify(password, subject.PasswordHash)
    │       ├─ Verificar Status != Blocked
    │       └─ Retornar Subject
    ├─ Crear ClaimsPrincipal
    │       ├─ ClaimTypes.NameIdentifier = subject.Id
    │       ├─ ClaimTypes.Role = subject.Role.ToString()
    │       └─ ClaimTypes.Email = subject.Email
    ├─ HttpContext.SignInAsync("nativa_auth", principal)
    └─ RedirectToAction("Index", "Dashboard")
```

---

## Flujo FIFO (Cola de fincas)

```
GET /Ingeniero/Cola
    └─ AssetService.GetQueueAsync()
           └─ Assets WHERE Status=Pending ORDER BY RegisteredAt ASC

POST /Ingeniero/Cola/Tomar/{id}
    ├─ Leer RowVersion del asset desde hidden field en la vista
    ├─ AssetService.TakeAssetAsync(id, engineerId, rowVersion)
    │       ├─ UPDATE Assets SET Status=UnderReview, EngineerId=@id
    │       │   WHERE Id=@id AND RowVersion=@rv
    │       └─ 0 filas afectadas → DbUpdateConcurrencyException → HTTP 409
    ├─ Enviar notificación N05 al Owner
    └─ RedirectToAction("Evaluar", new { id })
```

---

## Fórmula de pagos (CalculatorService)

```csharp
decimal sumPct = asset.Vegetation + asset.Hydrology
               + (asset.IsNational ? parameters.NationalPct : 0)
               + asset.Topography;

decimal monthlyAmount = asset.Hectares
                      * parameters.BasePrice
                      * (1 + Math.Min(sumPct, parameters.Cap));

// Redondear a 2 decimales
monthlyAmount = Math.Round(monthlyAmount, 2, MidpointRounding.AwayFromZero);
```

Al activar el plan se persiste el snapshot JSON de PaymentParameters vigente.
Este snapshot es **inmutable** — los planes activos no cambian si se reconfiguran parámetros.

---

## Integraciones externas

### OpenWeather + Open Elevation (en paralelo)
```csharp
// En ExternalApiService.EvaluateLocationAsync(lat, lng)
var weatherTask = GetWeatherAsync(lat, lng);
var elevationTask = GetElevationAsync(lat, lng);
await Task.WhenAll(weatherTask, elevationTask);
// Si falla: loguear + retornar "No disponible", NO bloquear flujo
```

### Azure Blob
- Contenedor: `psa-docs`
- Extensiones permitidas: jpg, jpeg, png, pdf, dwg
- Tamaño máximo: 10 MB
- Nunca borrado físico
- SAS token generado on-demand para el Engineer (no almacenar)

---

## PaymentHostedService

```
IHostedService + PeriodicTimer → ejecuta 1 vez por día
    │
    ├─ Buscar: MonthlyPayments WHERE Status=Pending AND PaymentDate <= NOW()
    ├─ Por cada pago:
    │       ├─ Verificar que NO sea Executed (idempotencia)
    │       ├─ UPDATE MonthlyPayment SET Status=Executed, ExecutedAt=NOW()
    │       ├─ Enviar N10 al Owner (con PDF QuestPDF)
    │       └─ Si PaymentNumber == 12:
    │               ├─ UPDATE Asset SET Status=Expired
    │               ├─ INSERT nuevo Asset en FIFO (Status=Pending, RegisteredAt=NOW())
    │               └─ Enviar N12 al Owner
    └─ Errores individuales: loguear sin detener el ciclo
```

---

## Namespaces

| Carpeta | Namespace |
|---|---|
| `Data/Entities/` | `Nativa.Infrastructure.Entities` |
| `Data/Enums/` | `Nativa.Infrastructure.Enums` |
| `Data/NativaDbContext.cs` | `Nativa.Infrastructure` |
| `Services/` | `Nativa.Services` |
| `Controllers/` | `Nativa.Controllers` |
| `Models/` | `Nativa.Models` |
