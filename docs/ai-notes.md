# Notas de la IA — Sistema Nativa

> Este archivo registra decisiones, dudas resueltas y alertas técnicas
> que surgen durante la implementación. Se actualiza en cada sesión de trabajo.

---

## Convenciones definidas (2026-03-15)

| Decisión | Resolución |
|---|---|
| Naming de código | Inglés para services, controllers, viewmodels; español en comentarios y UI |
| camelCase scope | Solo variables locales y parámetros (estándar C#) |
| Namespace base | `Nativa.*` |
| **Entidades DOMAIN** | **Quedan en español** — Sujeto, Activo, PlanPago, etc. ya construidas con migration ejecutada |
| **Enums DOMAIN** | **Quedan en español** — RolEnum, EstadoActivoEnum, etc. ya construidos |
| Services, Controllers, ViewModels, DTOs | En inglés |
| Idioma documentación | Español (es-CR) |
| Controladores con URLs en español | Usar `[Route("Dueno")]` en `OwnerController`, etc. |

---

## Estado real del proyecto (verificado 2026-03-15 — análisis completo)

### Estructura de proyectos real (3 proyectos, no 1)
```
UiNativaSolution/
├── DOMAIN/        → Nativa.Domain.csproj      (entidades + enums, sin NuGet)
├── INFRASTRUCTURE/→ Nativa.Infrastructure.csproj (DbContext + migrations)
│                       depende de → DOMAIN + EF Core 10.0.5 + SqlServer 10.0.5
└── WEB_UI/        → WEB_UI.csproj             (MVC)
                        depende de → INFRASTRUCTURE + EF.Tools 10.0.5
```

### Lo que está hecho
- DOMAIN: 8 entidades en español ✅ | 4 enums en español ✅
- INFRASTRUCTURE: NativaDbContext básico ✅ | Migration InitialCreate ejecutada ✅ | BD PSA_Dev con 8 tablas ✅
- WEB_UI: _Layout con navbar condicional ✅ | Landing views ✅ | Auth UI stubs ✅ | ViewModels ✅
- Auth usa Session temporal (no Cookie Auth) ⚠️
- Vistas Auth bajo Views/Login/ y Views/Register/ — se moverán a Views/Auth/ en P2

### Bugs críticos encontrados en NativaDbContext / migration
Ver sección "Alertas técnicas" abajo — las FKs están mal mapeadas en 6 tablas.

---

## Dudas pendientes

*(Se documentan aquí las dudas que surjan durante la implementación para revisión del equipo)*

### [ABIERTA] Cifrado del IBAN
- El spec dice "IbanCompleto cifrado en BD" pero no especifica el algoritmo.
- Opciones: AES-256 con clave en appsettings, o ASP.NET Core Data Protection API.
- **Pendiente de decisión del equipo antes de implementar P5 (BankAccountService).**

### [ABIERTA] Vistas en español o inglés
- Las carpetas de Views (`Views/Dueno/`, `Views/Ingeniero/`) van en español para que
  el routing MVC por convención funcione sin atributos extra en las Views.
- Los controladores usan `[Route("Dueno")]` para mantener clase en inglés.
- **Confirmado por arquitectura: carpetas de Views en español, clases en inglés.**

### [ABIERTA] Tabla de auditoría / logs
- El spec no menciona tabla de auditoría explícita.
- `PaymentHostedService` debe loguear errores — ¿solo ILogger o tabla BD?
- **Por defecto: ILogger. Si el equipo requiere tabla, agregar en P6.**

---

## Alertas técnicas

### 🔴 Bug crítico — FK duplicadas en NativaDbContext (afecta 6 tablas)
**Causa:** Las entidades tienen FK explícita (ej. `IdDueno int`) + navigation property (`Sujeto? Dueno`)
sin que el Fluent API indique `HasForeignKey(x => x.IdDueno)`. EF Core genera una columna sombra adicional.

**Resultado en BD:** La FK real está en la columna sombra, no en `IdDueno`:

| Tabla | Propiedad en entidad | FK real en BD | Problema |
|---|---|---|---|
| CuentasBancarias | `IdDueno` | `DuenoId` (shadow) | IdDueno es columna huérfana |
| OtpSesiones | `IdSujeto` | `SujetoId` (shadow) | IdSujeto es columna huérfana |
| AdjuntosActivos | `IdActivo` | `ActivoId` (shadow) | IdActivo es columna huérfana |
| PlanesPago | `IdActivo`, `IdIngeniero` | `ActivoId`, `IngenieroId` | Ambas huérfanas |
| PagosMensuales | `IdPlan` | `PlanId` (shadow) | IdPlan es columna huérfana |
| ParametrosPagos | `CreadoPor` | `CreadorId` (shadow) | CreadoPor es columna huérfana |

**Fix requerido en P1:** Agregar `HasForeignKey` explícito en Fluent API para cada relación,
borrar la migration y recrear desde cero.

### 🔴 Bug — FechaCreacion sin DEFAULT GETUTCDATE()
La migration crea `FechaCreacion` como `datetime2` sin valor por defecto.
Si no se asigna en C# al guardar, quedará `0001-01-01`. Fix: agregar `HasDefaultValueSql("GETUTCDATE()")`.

### ⚠️ Falta índice `Activo(Estado, FechaRegistro)`
Requerido para rendimiento de la cola FIFO. Agregar en Fluent API.

### ⚠️ RowVersion y concurrencia (CU18 — Tomar Finca)
Al implementar `POST /Ingeniero/Cola/Tomar/{id}`:
- El RowVersion del Asset **debe venir del formulario** (hidden field en la vista de cola).
- Si `DbUpdateConcurrencyException` → mostrar error 409 con SweetAlert2, NO throw no manejado.
- Dos Engineers que toman la misma finca simultáneamente: el segundo debe ver el error 409.

### ⚠️ PaymentParameters — nunca modificar, solo INSERT
- `NUNCA` hacer UPDATE sobre un registro de PaymentParameters.
- Siempre INSERT una nueva fila con `IsActive=true` y poner `IsActive=false` en la anterior.
- Los PaymentPlans activos referencian su snapshot JSON (`ParametersSnapshot`), no la tabla directa.

### ⚠️ OTP — bloqueo en 3 intentos fallidos
- 3 fallos consecutivos → `Subject.Status = Blocked`.
- Solo un Admin puede reactivar un Subject bloqueado (CU07).
- Enviar notificación N03 al Owner al bloquear.
- El cooldown de reenvío (30s) y el límite (3 en 5 min) son independientes del bloqueo.

### ⚠️ Recuperación de contraseña — respuesta genérica (OWASP)
- El endpoint `POST /Auth/Forgot` **siempre** responde igual, independiente de si el correo existe.
- Nunca revelar "ese correo no existe" — es un vector de enumeración de usuarios.

### ⚠️ Soft delete obligatorio
- **Nunca** DELETE físico en ninguna tabla.
- Subjects: `Status = Inactive`.
- Assets: `Status = Expired` o `Status = Rejected`.
- BankAccounts: `IsActive = false`.
- AssetAttachments: nunca borrar (ni físico ni lógico).

---

## Notas de implementación por parte

### P1 — Lo que queda por hacer
- **Reescribir NativaDbContext** con Fluent API completa: todas las FKs explícitas, `HasDefaultValueSql("GETUTCDATE()")`, índice compuesto `Activo(Estado, FechaRegistro)`, tipos decimales confirmados, `IsRowVersion()`.
- **Borrar migration actual** (`InitialCreate`) + recrear BD + nueva migration.
- Instalar **BCrypt.Net-Next** en WEB_UI (o donde se usen los services).
- **Configurar Cookie Auth** en Program.cs (reemplazar Session por Cookie Auth real).
- **Completar appsettings.json** — agregar secciones Auth, AzureBlob, ExternalApis, Email.

### P2 — Lo que queda por hacer
- Las vistas existentes en `Views/Login/` y `Views/Register/` se **mueven** a `Views/Auth/`
  con los nombres del spec. No recrear desde cero.
- Los controllers `LoginController.cs` y `RegisterController.cs` se **reemplazan** por `AuthController.cs`.
- Cookie name: `nativa_auth`.
- `_Layout.cshtml` cambia de leer Session a leer Cookie Claims.

---

## Historial de sesiones

| Fecha | Sesión | Resultado |
|---|---|---|
| 2026-03-15 | Inicio | docs/ creada, convenciones definidas |
| 2026-03-15 | Análisis | 3 proyectos confirmados, bugs FK documentados, entidades quedan en español |
