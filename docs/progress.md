# Progreso de implementación — Sistema Nativa

> Actualizar este archivo al completar cada parte.
> Estado: ⬜ Pendiente | 🔄 En progreso | ✅ Completo

---

## P1 — Infraestructura base de datos
**Estado:** ✅ Completo

### Lo que YA existe (no recrear)
- ✅ `DOMAIN/` — 8 entidades en español + 4 enums en español
- ✅ `INFRASTRUCTURE/Nativa.Infrastructure.csproj` — EF Core 10.0.5 + SqlServer instalados
- ✅ `INFRASTRUCTURE/NativaDbContext.cs` — existe pero Fluent API incompleta
- ✅ Migration `InitialCreate` — ejecutada (con bugs de FK duplicadas)
- ✅ BD `PSA_Dev` — 8 tablas creadas (pero con columnas shadow erróneas)

### Lo que falta / correcciones necesarias
- [ ] **Reescribir Fluent API** en `NativaDbContext.cs` — corregir FKs, agregar defaults, índice FIFO
- [ ] **Borrar migration** InitialCreate y BD → recrear: `dotnet ef database drop` + nueva migration
- [ ] Instalar **`BCrypt.Net-Next`** (en WEB_UI o proyecto donde vayan los services)
- [ ] Configurar **Cookie Auth** en `Program.cs` (reemplazar Session por Cookie Auth real)
- [ ] Completar **`appsettings.json`** — agregar secciones Auth, AzureBlob, ExternalApis, Email

### Archivos a modificar (no crear de cero)
```
INFRASTRUCTURE/
└── NativaDbContext.cs        ← reescribir OnModelCreating completo

WEB_UI/
├── Program.cs                ← agregar Cookie Auth, quitar Session temporal
├── appsettings.json          ← agregar secciones faltantes
└── appsettings.Development.json ← agregar Email (Mailtrap)
```

---

## P2 — AuthController (Login, Registro, OTP, Recuperación)
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU01 — Registrar Owner + OTP (`POST /Auth/Register`, `POST /Auth/VerifyOtp`)
- [ ] CU02 — Iniciar sesión (`POST /Auth/Login`)
- [ ] CU03 — Cerrar sesión (`POST /Auth/Logout`)
- [ ] CU04 — Recuperar contraseña (`POST /Auth/Forgot`, `POST /Auth/Reset`)

### Tareas
- [ ] Crear `AuthController.cs`
- [ ] Crear `OtpService.cs`
- [ ] Mover/renombrar vistas existentes a `Views/Auth/`
- [ ] Conectar formularios UI existentes con lógica real
- [ ] Crear `Dashboard/Index.cshtml` con KPIs reales por rol

---

## P3 — DuenoController (Fincas CRUD)
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU09/10 — Ver y editar perfil (`GET/POST /Dueno/Perfil`)
- [ ] CU11 — Registrar finca con Leaflet + Azure Blob (`POST /Dueno/Fincas/Nueva`)
- [ ] CU12 — Ver mis fincas (`GET /Dueno/Fincas`)
- [ ] CU13 — Ver detalle finca (`GET /Dueno/Fincas/{id}`)
- [ ] CU14 — Actualizar finca devuelta (`POST /Dueno/Fincas/Editar/{id}`)
- [ ] CU15 — Reenviar a evaluación (`POST /Dueno/Fincas/Reenviar/{id}`)
- [ ] CU16 — Ver adjuntos (`GET /Fincas/{id}/Adjuntos`)

---

## P4 — IngenieroController (Cola FIFO + Dictamen)
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU17 — Ver cola FIFO (`GET /Ingeniero/Cola`)
- [ ] CU18 — Tomar finca con RowVersion (`POST /Ingeniero/Cola/Tomar/{id}`)
- [ ] CU19 — Evaluar finca (OpenWeather + Open Elevation paralelos)
- [ ] CU20 — Aprobar dictamen
- [ ] CU21 — Rechazar dictamen (observaciones obligatorias)
- [ ] CU22 — Devolver dictamen (observaciones obligatorias → re-FIFO)

---

## P5 — IBAN + Activar Plan de Pagos
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU23 — Registrar/actualizar IBAN (`POST /Dueno/CuentaBancaria`)
- [ ] CU24 — Activar plan de pagos (`POST /Ingeniero/Fincas/ActivarPlan/{id}`)

### Tareas
- [ ] Crear `CalculatorService.cs` con fórmula de pago
- [ ] Crear `BankAccountService.cs` (cifrado IBAN)
- [ ] Botón "Activar plan" deshabilitado si no hay IBAN registrado

---

## P6 — PagoHostedService + Historial Pagos
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU25 — Pago automático diario (PeriodicTimer)
- [ ] CU26 — Ver historial pagos (`GET /Dueno/Pagos`)

### Tareas
- [ ] Crear `PaymentHostedService.cs` (IHostedService)
- [ ] Idempotencia: nunca reprocesar Ejecutados
- [ ] Pago #12 → estado Vencida + re-insertar en FIFO + N12
- [ ] Vista `Views/Dueno/Pagos.cshtml` con Ag-Grid

---

## P7 — AdminController (Usuarios + Parámetros)
**Estado:** ✅ Completo

### Casos de uso
- [ ] CU05 — Crear Ingeniero (`POST /Admin/Usuarios/Crear`)
- [ ] CU06 — Editar usuario (`POST /Admin/Usuarios/Editar/{id}`)
- [ ] CU07 — Inactivar usuario (`POST /Admin/Usuarios/Inactivar/{id}`)
- [ ] CU08 — Ver usuarios (`GET /Admin/Usuarios`) con Ag-Grid
- [ ] CU27 — Reconfigurar parámetros Opción A/B (`POST /Admin/Parametros`)
- [ ] CU28 — Ver parámetros (`GET /Admin/Parametros`)

---

## P8 — Notificaciones Email
**Estado:** ✅ Completo

| Código | Evento | Destinatario |
|---|---|---|
| N03 | Bloqueo por intentos OTP fallidos | Owner |
| N05 | Finca pasa a EnRevision | Owner |
| N06 | Ingeniero corrige datos evaluación | Owner |
| N07 | Dictamen Aprobada | Owner |
| N08 | Dictamen Rechazada | Owner |
| N09 | Dictamen Devuelta | Owner |
| N10 | Pago mensual ejecutado (con PDF) | Owner |
| N12 | Contrato vencido (pago #12) | Owner |
| N13 | Cuenta inactivada por Admin | Usuario |
| N14 | Recálculo pagos Opción B | Owner |

### Tareas
- [ ] Crear `EmailService.cs`
- [ ] Configurar Mailtrap en `appsettings.Development.json`
- [ ] Crear plantillas de correo (HTML)
- [ ] Integrar QuestPDF para comprobantes N10

---

## P9 — Tests xUnit
**Estado:** ✅ Completo

| Test | Descripción |
|---|---|
| `CalculatorServiceTests` | Fórmula con inputs conocidos + SumaPct > Tope |
| `OtpServiceTests` | TTL expirado, cooldown 30s, bloqueo en 3er intento |
| `ConcurrencyTests` | Dos Tomar simultáneos → primero OK, segundo HTTP 409 |
| `IbanValidationTests` | CR + 20 dígitos válido, otros → inválido |
| `PaymentHostedServiceTests` | Sin duplicar, pago #12 → Vencida + FIFO + N12 |
| `DictamenTests` | Rechazar sin obs → ValidationException, Devolver → re-FIFO |
| `PaymentParametersTests` | Opción B → no toca Ejecutados |

---

## Historial de cambios
| Fecha | Parte | Descripción |
|---|---|---|
| 2026-03-15 | — | Proyecto iniciado. Carpeta docs/ creada. Convenciones definidas. |
| 2026-03-15 | P1-P9 | Implementación completa P1–P9. Build: 0 errores, 0 warnings. |
