# Sistema Nativa — Arquitectura

---

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Runtime | .NET 10.0 |
| Framework | ASP.NET Core MVC (único proyecto WEB_UI) |
| ORM | Entity Framework Core 10 (Code First) |
| BD | SQL Server local — `PSA_Dev` |
| Auth | Cookie Authentication, `nativa_auth`, 8h, HttpOnly, SameSite=Strict |
| Hashing | BCrypt.Net-Next factor 12 |
| Cifrado | AES-256 (IBAN) |
| PDF | QuestPDF 2025.4.0 |
| Email | MailKit/MimeKit (Mailtrap en dev) |
| Blob | Azure Blob Storage (`psa-docs`) |
| Frontend | Razor Views + Bootstrap 5 Bootswatch Minty (CDN) |
| JS | jQuery AJAX + SweetAlert2 + Ag-Grid Community (CDN) |
| Mapas | Leaflet.js |
| Testing | xUnit + Moq |

---

## Patrón Arquitectónico

```
HTTP Request
    ↓
Controller  (autorización, binding, validación superficial)
    ↓
Service     (lógica de negocio, transacciones)
    ↓
DbContext   (EF Core, consultas, cambios)
    ↓
SQL Server
```

**NO HAY:** CQRS, MediatR, capas separadas Domain/Infrastructure/Application.

---

## Estructura de Directorios

```
UiNativaSolution/
├── CLAUDE.md                          ← spec absoluta
├── docs/                              ← documentación
│   ├── progress.md
│   ├── use-cases.md
│   ├── architecture.md  (este archivo)
│   ├── auth.md
│   ├── fincas.md
│   ├── ingeniero.md
│   ├── pagos.md
│   ├── admin.md
│   ├── notifications.md
│   ├── testing.md
│   ├── backlog.md
│   └── api-integrations.md
├── WEB_UI/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── DuenoController.cs
│   │   ├── IngenieroController.cs
│   │   ├── AdminController.cs
│   │   ├── HomeController.cs
│   │   └── LandingController.cs
│   ├── Data/
│   │   ├── NativaDbContext.cs
│   │   └── DataSeeder.cs
│   ├── Migrations/
│   ├── Models/
│   │   ├── Entities/          ← 8 entidades
│   │   ├── Enums/             ← 4 enums
│   │   └── Dtos/
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── OtpService.cs
│   │   ├── EmailService.cs
│   │   ├── ActivoService.cs
│   │   ├── IngenieroService.cs
│   │   ├── AdminService.cs
│   │   ├── PagoHostedService.cs
│   │   ├── CalculadoraService.cs
│   │   ├── EncryptionService.cs
│   │   ├── BlobService.cs
│   │   └── ExternalApiService.cs
│   ├── Views/
│   │   ├── Auth/
│   │   ├── Dueno/
│   │   │   └── Fincas/
│   │   ├── Ingeniero/
│   │   │   └── Fincas/
│   │   ├── Admin/
│   │   ├── Dashboard/
│   │   ├── Landing/
│   │   └── Shared/
│   └── wwwroot/
│       ├── css/
│       └── js/
│           ├── site.js
│           └── Pages/
│               ├── Auth/
│               ├── Dueno/
│               ├── Ingeniero/
│               └── Admin/
└── TESTS/
    └── Nativa.Tests.csproj
```

---

## Entidades y Relaciones

```
Sujeto (1) ──── (N) Activo        (Dueno → Fincas)
Sujeto (1) ──── (N) Activo        (Ingeniero → Fincas asignadas)
Activo (1) ──── (N) AdjuntoActivo
Sujeto (1) ──── (N) CuentaBancaria
Activo (1) ──── (1) PlanPago
PlanPago (1) ── (N) PagoMensual   (siempre 12)
Sujeto (1) ──── (N) OtpSesion
ParametrosPago  (tabla de solo-INSERT)
```

---

## Seguridad

| Aspecto | Implementación |
|---|---|
| Contraseña | BCrypt factor 12, validación doble (JS + C#) |
| Cookie | HttpOnly, SameSite=Strict, SecurePolicy=Always, 8h no-sliding |
| OTP | BCrypt hashed, TTL 90s, max 3 intentos → bloqueo |
| Reset token | HMAC-SHA256, TTL 15min, un uso |
| IBAN | AES-256 en BD, ofuscado para Admin |
| FIFO concurrencia | RowVersion [Timestamp] + `DbUpdateConcurrencyException` → 409 |
| Soft delete | NUNCA DELETE físico; campo `Estado` o `bool Activo` |

---

## Connection String (dev)

```
Server=localhost;Database=PSA_Dev;Trusted_Connection=True;TrustServerCertificate=True;
```
